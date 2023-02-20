using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Error;
using Beacon.Sdk.Beacon.Operation;
using Beacon.Sdk.Beacon.Permission;
using Beacon.Sdk.Beacon.Sign;
using Beacon.Sdk.BeaconClients;
using Beacon.Sdk.BeaconClients.Abstract;
using Beacon.Sdk.Core.Domain.Entities;
using Netezos.Encoding;
using Netezos.Forging.Models;
using Netezos.Keys;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Serilog.Extensions.Logging;
using Constants = Beacon.Sdk.Constants;

using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Wallet;
using Hex = Atomex.Common.Hex;
using Atomex.Core;
using Netezos.Forging;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class DappViewModel : ViewModelBase
    {
        public PermissionInfo PermissionInfo { get; set; }
        public string Name => PermissionInfo.AppMetadata.Name;
        public string ConnectedAddress => PermissionInfo.Address;
        public Action<PermissionInfo> OnDisconnect { get; set; }
        public Action<PermissionInfo> OnDappClick { get; set; }

        private ReactiveCommand<Unit, Unit>? _openDappSiteCommand;
        public ReactiveCommand<Unit, Unit> OpenDappSiteCommand =>
            _openDappSiteCommand ??= ReactiveCommand.Create(() =>
            {
                if (Uri.TryCreate(PermissionInfo.AppMetadata.AppUrl, UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
            });

        private ReactiveCommand<Unit, Unit>? _disconnectCommand;
        public ReactiveCommand<Unit, Unit> DisconnectCommand =>
            _disconnectCommand ??= ReactiveCommand.Create(() => OnDisconnect?.Invoke(PermissionInfo));

        private ReactiveCommand<Unit, Unit>? _dappClickCommand;
        public ReactiveCommand<Unit, Unit> DappClickCommand =>
            _dappClickCommand ??= ReactiveCommand.Create(() => OnDappClick?.Invoke(PermissionInfo));

        private ReactiveCommand<Unit, Unit>? _copyCommand;
        public ReactiveCommand<Unit, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create(() =>
        {
            try
            {
                App.Clipboard.SetTextAsync(ConnectedAddress);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });
    }

    public class DappsViewModel : ViewModelBase
    {
        private readonly IAtomexApp _atomexApp;
        private IWalletBeaconClient _beaconWalletClient;

        [Reactive] public ObservableCollection<DappViewModel> Dapps { get; set; }
        public SelectAddressViewModel SelectAddressViewModel { get; private set; }
        private TezosConfig Tezos { get; }
        private ConnectDappViewModel ConnectDappViewModel { get; }

        public DappsViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }

        public DappsViewModel(IAtomexApp atomexApp)
        {
            _atomexApp = atomexApp ?? throw new ArgumentNullException(nameof(atomexApp));

            if (_atomexApp.Account == null)
                return;

            var pathToDb = $"{Path.GetDirectoryName(_atomexApp.Account.Wallet.PathToWallet)}/beacon.db";
            var beaconOptions = new BeaconOptions
            {
                AppName = "Atomex desktop",
                AppUrl = "https://atomex.me",
                IconUrl = "https://bcd-static-assets.fra1.digitaloceanspaces.com/dapps/atomex/atomex_logo.jpg",
                KnownRelayServers = Constants.KnownRelayServers,

                DatabaseConnectionString = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? $"Filename={pathToDb}; Connection=Shared;"
                    : $"Filename={pathToDb}; Mode=Exclusive;"
            };

            _atomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
            Tezos = (TezosConfig)_atomexApp.Account.Currencies.GetByName(TezosConfig.Xtz);

            ConnectDappViewModel = new ConnectDappViewModel
            {
                OnBack = () => App.DialogService.Show(SelectAddressViewModel!),
                OnConnect = Connect
            };

            _ = Task.Run(async () =>
            {
                _beaconWalletClient = BeaconClientFactory
                    .Create<IWalletBeaconClient>(beaconOptions, new SerilogLoggerProvider(Log.Logger));
                _beaconWalletClient.OnBeaconMessageReceived += OnBeaconWalletClientMessageReceived;
                _beaconWalletClient.OnConnectedClientsListChanged += OnDappsListChanged;

                await _beaconWalletClient.InitAsync();

                _beaconWalletClient.Connect();

                GetAllDapps();

                Log.Debug("{@Sender}: WalletClient connected {@Connected}", "Beacon", _beaconWalletClient.Connected);
                Log.Debug("{@Sender}: WalletClient logged in {@LoggedIn}", "Beacon", _beaconWalletClient.LoggedIn);
            });
        }

        public void CreateAddresses()
        {
            SelectAddressViewModel = new SelectAddressViewModel(_atomexApp.Account, Tezos, SelectAddressMode.Connect)
            {
                BackAction = () => { App.DialogService.Close(); },
                ConfirmAction = walletAddressViewModel =>
                {
                    ConnectDappViewModel.AddressToConnect = walletAddressViewModel.Address;
                    App.DialogService.Show(ConnectDappViewModel);
                }
            };
        }

        private void OnDappsListChanged(object? sender, ConnectedClientsListChangedEventArgs? e)
        {
            GetAllDapps();
            Log.Information("{@Sender}: connected dapp: {@Dapp}", "Beacon", e?.Metadata.Name);
        }

        private void GetAllDapps()
        {
            var permissionInfoList = _beaconWalletClient
                .PermissionInfoRepository
                .ReadAllAsync()
                .Result;

            Dapps = new ObservableCollection<DappViewModel>(permissionInfoList
                .Select(permissionInfo => new DappViewModel
                {
                    PermissionInfo = permissionInfo,
                    OnDisconnect = disconnectPeer =>
                    {
                        var disconnectViewModel = new DisconnectViewModel
                        {
                            DappName = permissionInfo.AppMetadata.Name,
                            OnDisconnect = async () => await _beaconWalletClient
                                .RemovePeerAsync(disconnectPeer.SenderId)
                        };

                        App.DialogService.Show(disconnectViewModel);
                    },
                    OnDappClick = clickedPermissionInfo =>
                    {
                        var showDappViewModel = new ShowDappViewModel
                        {
                            DappName = clickedPermissionInfo.AppMetadata.Name,
                            DappId = clickedPermissionInfo.SenderId,
                            Address = clickedPermissionInfo.Address,
                            Permissions = clickedPermissionInfo.Scopes,
                            OnDisconnect = () =>
                            {
                                var disconnectViewModel = new DisconnectViewModel
                                {
                                    DappName = clickedPermissionInfo.AppMetadata.Name,
                                    OnDisconnect = async () => await _beaconWalletClient
                                        .RemovePeerAsync(clickedPermissionInfo.SenderId)
                                };

                                App.DialogService.Show(disconnectViewModel);
                            }
                        };

                        App.DialogService.Show(showDappViewModel);
                    }
                }));
        }

        private async Task Connect(string qrCodeString)
        {
            if (_atomexApp.Account == null)
                return;

            var pairingRequest = _beaconWalletClient.GetPairingRequest(qrCodeString);

            await _beaconWalletClient.AddPeerAsync(pairingRequest);

            App.DialogService.Close();
        }

        private async void OnBeaconWalletClientMessageReceived(object? sender, BeaconMessageEventArgs e)
        {
            var message = e.Request;

            if (message == null)
                return;

            var permissions = await _beaconWalletClient
                .PermissionInfoRepository
                .TryReadBySenderIdAsync(message.SenderId);

            Log.Debug("{@Sender}: message with type {@Type}, from {@SenderId} received",
                "Beacon", message.Type, message.SenderId);

            switch (message.Type)
            {
                case BeaconMessageType.permission_request:
                {
                    await PermissionRequestHandler(message);
                    break;
                }
                case BeaconMessageType.operation_request:
                {
                    await OperationRequestHandler(message, permissions?.Address);
                    break;
                }
                case BeaconMessageType.sign_payload_request:
                {
                    await SignPayloadRequestHandler(message, permissions?.Address);
                    break;
                }
            }
        }

        private async Task PermissionRequestHandler(BaseBeaconMessage message)
        {
            if (message is not PermissionRequest permissionRequest)
                return;

            if (!string.Equals(permissionRequest.Network.Type.ToString(), _atomexApp.Account.Network.ToString(),
                    StringComparison.CurrentCultureIgnoreCase))
            {
                await _beaconWalletClient.SendResponseAsync(
                    receiverId: message.SenderId,
                    response: new NetworkNotSupportedBeaconError(permissionRequest.Id,
                        _beaconWalletClient.SenderId));
                return;
            }

            var addressToConnect = await _atomexApp
                .Account
                .GetAddressAsync(Tezos.Name, ConnectDappViewModel.AddressToConnect);

            var securedPublicKey = _atomexApp.Account.Wallet.GetPublicKey(
                Tezos,
                addressToConnect.KeyPath,
                addressToConnect.KeyType);

            var publicKey = securedPublicKey.ToUnsecuredBytes();

            var response = new PermissionResponse(
                id: permissionRequest.Id,
                senderId: _beaconWalletClient.SenderId,
                appMetadata: _beaconWalletClient.Metadata,
                network: permissionRequest.Network,
                scopes: permissionRequest.Scopes,
                publicKey: PubKey.FromBytes(publicKey).ToString(),
                version: permissionRequest.Version);

            var permissionRequestViewModel = new PermissionRequestViewModel
            {
                DappName = permissionRequest.AppMetadata.Name,
                Address = addressToConnect.Address,
                Permissions = permissionRequest.Scopes,
                OnReject = async () =>
                {
                    await _beaconWalletClient.SendResponseAsync(
                        receiverId: message.SenderId,
                        response: new BeaconAbortedError(permissionRequest.Id, _beaconWalletClient.SenderId));

                    await _beaconWalletClient.RemovePeerAsync(
                        message.SenderId);

                    App.DialogService.Close();

                    Log.Information(
                        "{@Sender}: Rejected permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                        "Beacon",
                        permissionRequest.Scopes.Aggregate(string.Empty,
                            (res, scope) => res + $"{scope}, "),
                        permissionRequest.AppMetadata.Name, addressToConnect.Address);
                },
                OnAllow = async () =>
                {
                    await _beaconWalletClient.SendResponseAsync(
                        receiverId: message.SenderId,
                        response);

                    App.DialogService.Close();

                    Log.Information(
                        "{@Sender}: Issued permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                        "Beacon",
                        permissionRequest.Scopes.Aggregate(string.Empty,
                            (res, scope) => res + $"{scope}, "),
                        permissionRequest.AppMetadata.Name, addressToConnect.Address);
                }
            };

            App.DialogService.Show(permissionRequestViewModel);
        }

        private async Task OperationRequestHandler(BaseBeaconMessage message, string? address)
        {
            if (message is not OperationRequest operationRequest)
                return;

            var permissionInfo = _beaconWalletClient
                .PermissionInfoRepository
                .TryReadBySenderIdAsync(operationRequest.SenderId)
                .Result;

            if (permissionInfo == null)
            {
                await _beaconWalletClient.SendResponseAsync(
                    receiverId: message.SenderId,
                    response: new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));

                return;
            }

            var connectedWalletAddress = await _atomexApp
                .Account
                .GetAddressAsync(Tezos.Name, address ?? string.Empty);

            var rpc = new TezosRpc(Tezos.GetRpcSettings());

            var head = await rpc
                .GetHeaderAsync()
                .ConfigureAwait(false);

            var managerKey = await rpc
                .GetManagerKeyAsync(connectedWalletAddress.Address)
                .ConfigureAwait(false);

            var revealed = managerKey != null;

            var account = await rpc
                .GetAccountAsync(connectedWalletAddress.Address, head.Hash)
                .ConfigureAwait(false);

            var counter = int.Parse(account.Counter);

            var operations = new List<TezosOperationParameters>();

            if (!revealed)
            {
                var securedPublicKey = _atomexApp.Account.Wallet.GetPublicKey(
                    Tezos,
                    connectedWalletAddress.KeyPath,
                    connectedWalletAddress.KeyType);

                var publicKey = securedPublicKey.ToUnsecuredBytes();

                operations.Add(new TezosOperationParameters
                {
                    Content = new RevealContent
                    {
                        Counter = ++counter,
                        Fee = 0,
                        GasLimit = 1_000_000,
                        Source = connectedWalletAddress.Address,
                        PublicKey = PubKey.FromBytes(publicKey).ToString(),
                        StorageLimit = 0
                    },
                    Fee = Fee.FromNetwork(defaultValue: 0),
                    From = connectedWalletAddress.Address,
                    GasLimit = GasLimit.FromNetwork(defaultValue: 1_000_000),
                    StorageLimit = StorageLimit.FromNetwork(defaultValue: 0)
                });
            }

            operations.AddRange(operationRequest.OperationDetails.Select(o =>
            {
                if (!long.TryParse(o.Amount, out var amount))
                    amount = 0;

                var txContent = new TransactionContent
                {
                    Source = connectedWalletAddress.Address,
                    Destination = o.Destination,
                    Amount = amount,
                    Counter = ++counter,
                    Fee = 0,
                    GasLimit = 500_000,
                    StorageLimit = 5000,
                };

                if (o.Parameters != null)
                {
                    try
                    {
                        txContent.Parameters = new Parameters
                        {
                            Entrypoint = o.Parameters?["entrypoint"]!.ToString(),
                            Value = Micheline.FromJson(o.Parameters?["value"]!.ToString())
                        };
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception during parsing Beacon operation params");
                    }
                }

                return new TezosOperationParameters
                {
                    Content = txContent,
                    Fee = Fee.FromNetwork(defaultValue: 0),
                    GasLimit = GasLimit.FromNetwork(defaultValue: 500_000),
                    StorageLimit = StorageLimit.FromNetwork(defaultValue: 5000),
                    From = connectedWalletAddress.Address
                };
            }));

            var (isSuccess, error) = await TezosOperationFiller
                .AutoFillAsync(
                    rpc: rpc,
                    operations,
                    head.Hash,
                    Tezos.GetFillOperationSettings())
                .ConfigureAwait(false);

            if (error != null)
            {
                await _beaconWalletClient.SendResponseAsync(
                    receiverId: message.SenderId,
                    response: new TransactionInvalidBeaconError(operationRequest.Id,
                        _beaconWalletClient.SenderId));

                Log.Error("{@Sender}: error during AutoFill transaction, {@Msg}", "Beacon", error.Value.Message);
                return;
            }

            var forgedOperations = await new LocalForge()
                .ForgeOperationGroupAsync(
                    head.Hash,
                    operations.Select(p => p.Content));

            var operationsViewModel = new ObservableCollection<BaseBeaconOperationViewModel>();

            foreach (var item in operations.Select((value, idx) => new { idx, value }))
            {
                var operation = item.value;
                var index = item.idx;

                switch (operation.Content)
                {
                    case TransactionContent transactionOperation:
                        operationsViewModel.Add(new TransactionContentViewModel
                        {
                            Id = index + 1,
                            Operation = transactionOperation,
                            QuotesProvider = _atomexApp.QuotesProvider,
                        });
                        break;
                    case RevealContent revealOperation:
                        operationsViewModel.Add(new RevealContentViewModel
                        {
                            Id = index + 1,
                            Operation = revealOperation,
                            QuotesProvider = _atomexApp.QuotesProvider,
                        });
                        break;
                }
            }

            var operationRequestViewModel = new OperationRequestViewModel
            {
                DappName = permissionInfo.AppMetadata.Name,
                DappLogo = permissionInfo.AppMetadata.Icon,
                Operations = operationsViewModel,
                OnReject = async () =>
                {
                    await _beaconWalletClient.SendResponseAsync(
                        receiverId: message.SenderId,
                        response: new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));

                    App.DialogService.Close();
                },

                OnConfirm = async () =>
                {
                    var wallet = (HdWallet)_atomexApp.Account.Wallet;
                    var keyStorage = wallet.KeyStorage;

                    using var securePrivateKey = keyStorage.GetPrivateKey(
                        currency: Tezos,
                        keyPath: connectedWalletAddress.KeyPath,
                        keyType: connectedWalletAddress.KeyType);

                    var privateKey = securePrivateKey.ToUnsecuredBytes();

                    var signedMessage = TezosSigner.SignHash(
                        data: forgedOperations,
                        privateKey: privateKey,
                        watermark: Watermark.Generic,
                        isExtendedKey: privateKey.Length == 64);

                    if (signedMessage == null)
                    {
                        Log.Error("Beacon transaction signing error");

                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: message.SenderId,
                            response: new TransactionInvalidBeaconError(operationRequest.Id,
                                _beaconWalletClient.SenderId));

                        return;
                    }

                    string? operationId = null;

                    try
                    {
                        var injectedOperation = await rpc
                            .InjectOperationsAsync(signedMessage.SignedBytes)
                            .ConfigureAwait(false);

                        operationId = injectedOperation.ToString();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Beacon transaction broadcast error {@Description}", ex.Message);

                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: message.SenderId,
                            response: new BroadcastBeaconError(operationRequest.Id,
                                _beaconWalletClient.SenderId));

                        return;
                    }

                    var response = new OperationResponse(
                        id: operationRequest.Id,
                        senderId: _beaconWalletClient.SenderId,
                        transactionHash: operationId,
                        operationRequest.Version);

                    await _beaconWalletClient.SendResponseAsync(
                        receiverId: message.SenderId,
                        response);

                    App.DialogService.Close();

                    Log.Information("{@Sender}: operation done with transaction hash: {@Hash}",
                        "Beacon",
                        operationId);
                }
            };

            App.DialogService.Show(operationRequestViewModel);
        }

        private async Task SignPayloadRequestHandler(BaseBeaconMessage message, string? address)
        {
            if (message is not SignPayloadRequest signRequest)
                return;

            byte[] dataToSign;

            try
            {
                dataToSign = Hex.FromString(signRequest.Payload);
            }
            catch (Exception)
            {
                // data is not in HEX format
                dataToSign = Encoding.UTF8.GetBytes(signRequest.Payload);
            }

            var connectedWalletAddress = await _atomexApp
                .Account
                .GetAddressAsync(Tezos.Name, address ?? string.Empty);

            var permissionInfo = await _beaconWalletClient
                .PermissionInfoRepository
                .TryReadBySenderIdAsync(message.SenderId);

            var signatureRequestViewModel = new SignatureRequestViewModel
            {
                DappName = permissionInfo!.AppMetadata.Name,
                Payload = signRequest.Payload,
                OnSign = async () =>
                {
                    var hdWallet = _atomexApp.Account.Wallet as HdWallet;

                    using var privateKey = hdWallet!.KeyStorage.GetPrivateKey(
                        currency: Tezos,
                        keyPath: connectedWalletAddress.KeyPath,
                        keyType: connectedWalletAddress.KeyType);

                    var signedMessage = TezosSigner.SignHash(
                        data: dataToSign,
                        privateKey: privateKey.ToUnsecuredBytes(),
                        watermark: null,
                        isExtendedKey: privateKey.Length == 64);

                    var response = new SignPayloadResponse(
                        signature: signedMessage.EncodedSignature,
                        version: signRequest.Version,
                        id: signRequest.Id,
                        senderId: _beaconWalletClient.SenderId);

                    await _beaconWalletClient.SendResponseAsync(
                        receiverId: message.SenderId, response);

                    App.DialogService.Close();

                    Log.Information(
                        "{@Sender}: signed payload for {@Dapp} with signature: {@Signature}",
                        "Beacon",
                        permissionInfo.AppMetadata.Name,
                        signedMessage.EncodedSignature);
                },
                OnReject = async () =>
                {
                    await _beaconWalletClient.SendResponseAsync(
                        receiverId: message.SenderId,
                        response: new BeaconAbortedError(signRequest.Id, _beaconWalletClient.SenderId));

                    App.DialogService.Close();

                    Log.Information(
                        "{@Sender}: user Aborted signing payload from {@Dapp}",
                        "Beacon",
                        permissionInfo.AppMetadata.Name);
                }
            };

            App.DialogService.Show(signatureRequestViewModel);
        }

        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            if (args.AtomexClient != null && _atomexApp.Account != null)
                return;

            _beaconWalletClient.Disconnect();
            _atomexApp.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
            _beaconWalletClient.OnBeaconMessageReceived -= OnBeaconWalletClientMessageReceived;
            _beaconWalletClient.OnConnectedClientsListChanged -= OnDappsListChanged;
        }

        private void DesignerMode()
        {
        }
    }
}