using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using Atomex.Wallet;
using Avalonia.Controls;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Error;
using Beacon.Sdk.Beacon.Operation;
using Beacon.Sdk.Beacon.Permission;
using Beacon.Sdk.Beacon.Sign;
using Beacon.Sdk.Core.Domain.Entities;
using Netezos.Encoding;
using Netezos.Keys;
using Netezos.Forging.Models;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Atomex.Blockchain.Tezos.Internal;
using Beacon.Sdk.BeaconClients;
using Beacon.Sdk.BeaconClients.Abstract;
using Serilog.Extensions.Logging;
using Constants = Beacon.Sdk.Constants;
using Hex = Atomex.Common.Hex;


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
        private const int GasLimitPerBlock = 5_200_000;
        public const int StorageLimitPerOperation = 5000;

        private readonly IAtomexApp _atomexApp;
        private IWalletBeaconClient _beaconWalletClient;
        private TezosConfig Tezos { get; }
        [Reactive] public ObservableCollection<DappViewModel> Dapps { get; set; }
        public ConnectDappViewModel ConnectDappViewModel { get; }

        public DappsViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }

        public DappsViewModel(IAtomexApp atomexApp)
        {
            _atomexApp = atomexApp ?? throw new ArgumentNullException(nameof(atomexApp));
            if (_atomexApp.Account == null) return;

            var pathToWallet = Path.GetDirectoryName(_atomexApp.Account.Wallet.PathToWallet);
            if (pathToWallet == null) return;

            App.ConnectTezosDapp = qrCodeData => _ = TryPairFromDeeplinkData(qrCodeData);

            var pathToDb = Path.Combine(pathToWallet, "beacon.db");
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

                if (!string.IsNullOrEmpty(App.MainWindowViewModel.StartupData))
                {
                    await TryPairFromDeeplinkData(App.MainWindowViewModel.StartupData);
                }
            });
        }

        private async Task TryPairFromDeeplinkData(string deeplinkData)
        {
            try
            {
                var uri = new Uri(deeplinkData);
                var type = HttpUtility.ParseQueryString(uri.Query).Get("type");
                var data = HttpUtility.ParseQueryString(uri.Query).Get("data");
                if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(data) || type != "tzip10") return;

                await Connect(data);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during parsing deeplink with data {Data}", deeplinkData);
            }
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
            if (_atomexApp.Account == null || !_beaconWalletClient.Connected || !_beaconWalletClient.LoggedIn)
                return;

            try
            {
                var pairingRequest = _beaconWalletClient.GetPairingRequest(qrCodeString);
                await _beaconWalletClient.AddPeerAsync(pairingRequest);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Can't connect to peer");
            }

            App.DialogService.Close();
        }

        private async void OnBeaconWalletClientMessageReceived(object? sender, BeaconMessageEventArgs e)
        {
            var message = e.Request;
            if (message == null) return;

            var permissions = _beaconWalletClient
                .PermissionInfoRepository
                .TryReadBySenderIdAsync(message.SenderId)
                .Result;

            Log.Debug("{@Sender}: message with type {@Type}, from {@SenderId} received",
                "Beacon", message.Type, message.SenderId);

            var connectedWalletAddress = await _atomexApp
                .Account
                .GetAddressAsync(Tezos.Name, permissions?.Address ?? string.Empty);

            switch (message.Type)
            {
                case BeaconMessageType.permission_request:
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

                    async Task OnRejectOrCloseAction()
                    {
                        await _beaconWalletClient.SendResponseAsync(receiverId: message.SenderId,
                            response: new BeaconAbortedError(permissionRequest.Id, _beaconWalletClient.SenderId));
                        await _beaconWalletClient.RemovePeerAsync(message.SenderId);

                        App.DialogService.Close();
                        Log.Information("{@Sender}: Rejected permissions [{@PermissionsList}] to dapp {@Dapp}",
                            "Beacon",
                            permissionRequest.Scopes.Aggregate(string.Empty, (res, scope) => res + $"{scope}, "),
                            permissionRequest.AppMetadata.Name);
                    }

                    var permissionRequestViewModel = new PermissionRequestViewModel(_atomexApp.Account, Tezos,
                        () => _ = OnRejectOrCloseAction())
                    {
                        DappName = permissionRequest.AppMetadata.Name,
                        Permissions = permissionRequest.Scopes,
                        OnReject = OnRejectOrCloseAction,
                        OnAllow = async selectedAddress =>
                        {
                            var addressToConnect = await _atomexApp
                                .Account
                                .GetAddressAsync(Tezos.Name, selectedAddress.Address);

                            var response = new PermissionResponse(
                                id: permissionRequest.Id,
                                senderId: _beaconWalletClient.SenderId,
                                appMetadata: _beaconWalletClient.Metadata,
                                network: permissionRequest.Network,
                                scopes: permissionRequest.Scopes,
                                publicKey: PubKey.FromBase64(addressToConnect.PublicKey).ToString(),
                                version: permissionRequest.Version);

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
                    break;
                }
                case BeaconMessageType.operation_request:
                {
                    if (message is not OperationRequest operationRequest)
                        return;

                    var permissionInfo = _beaconWalletClient
                        .PermissionInfoRepository
                        .TryReadBySenderIdAsync(operationRequest.SenderId)
                        .Result;

                    if (permissionInfo == null)
                    {
                        Log.Error("Can't find permission info");

                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: message.SenderId,
                            response: new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));
                        return;
                    }

                    var rpc = new Rpc(Tezos.RpcNodeUri);
                    JObject account;
                    bool revealed;

                    try
                    {
                        var head = await rpc
                            .GetHeader()
                            .ConfigureAwait(false);

                        var managerKey = await rpc
                            .GetManagerKey(connectedWalletAddress.Address)
                            .ConfigureAwait(false);

                        revealed = managerKey.Value<string>() != null;
                        account = await rpc
                            .GetAccountForBlock(head["hash"]!.ToString(), connectedWalletAddress.Address)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during querying rpc, {Message}", ex.Message);
                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: message.SenderId,
                            response: new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));

                        return;
                    }

                    var counter = int.Parse(account["counter"]!.ToString());
                    var operations = new List<ManagerOperationContent>();

                    var totalOperations = revealed
                        ? operationRequest.OperationDetails.Count
                        : operationRequest.OperationDetails.Count + 1;

                    var operationGasLimit = Math.Min(GasLimitPerBlock / totalOperations, 500_000);

                    if (!revealed)
                    {
                        operations.Add(new RevealContent
                        {
                            Counter = ++counter,
                            Source = connectedWalletAddress.Address,
                            PublicKey = PubKey.FromBase64(connectedWalletAddress.PublicKey).ToString(),
                            Fee = 0,
                            GasLimit = operationGasLimit,
                            StorageLimit = 0
                        });
                    }

                    foreach (var operation in operationRequest.OperationDetails)
                    {
                        if (operation is PartialTezosTransactionOperation transactionOperation)
                        {
                            if (!long.TryParse(transactionOperation.Amount, out var amount))
                                amount = 0;

                            var txContent = new TransactionContent
                            {
                                Source = connectedWalletAddress.Address,
                                Destination = transactionOperation.Destination,
                                Amount = amount,
                                Counter = ++counter,
                                Fee = 0,
                                GasLimit = operationGasLimit,
                                StorageLimit = StorageLimitPerOperation,
                            };

                            if (transactionOperation.Parameters == null)
                            {
                                operations.Add(txContent);
                                continue;
                            }

                            try
                            {
                                txContent.Parameters = new Parameters
                                {
                                    Entrypoint = transactionOperation.Parameters?["entrypoint"]!.ToString(),
                                    Value = Micheline.FromJson(transactionOperation.Parameters?["value"]!
                                        .ToString())
                                };
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Exception during parsing Beacon operation params");
                            }

                            operations.Add(txContent);
                        }

                        if (operation is TezosDelegationOperation delegationOperation)
                        {
                            var delegationContent = new DelegationContent
                            {
                                Source = connectedWalletAddress.Address,
                                Counter = ++counter,
                                Fee = 0,
                                GasLimit = operationGasLimit,
                                StorageLimit = StorageLimitPerOperation,
                                Delegate = delegationOperation.Delegate
                            };

                            operations.Add(delegationContent);
                        }
                    }

                    async Task OnRejectOrCloseAction()
                    {
                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: message.SenderId,
                            response: new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));

                        App.DialogService.Close();
                    }

                    var operationRequestViewModel = new OperationRequestViewModel(operations, connectedWalletAddress,
                        operationGasLimit, Tezos)
                    {
                        QuotesProvider = _atomexApp.QuotesProvider,
                        DappName = permissionInfo.AppMetadata.Name,
                        DappLogo = permissionInfo.AppMetadata.Icon,
                        OnReject = OnRejectOrCloseAction,
                        OnClose = () => _ = OnRejectOrCloseAction(),
                        OnConfirm = async forgedOperations =>
                        {
                            var wallet = (HdWallet)_atomexApp.Account.Wallet;
                            var keyStorage = wallet.KeyStorage;

                            using var securePrivateKey = keyStorage.GetPrivateKey(
                                currency: Tezos,
                                keyIndex: connectedWalletAddress.KeyIndex,
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

                            string? operationId;

                            try
                            {
                                var injectedOperation = await rpc
                                    .InjectOperations(signedMessage.SignedBytes)
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
                    break;
                }
                case BeaconMessageType.sign_payload_request:
                {
                    if (message is not SignPayloadRequest signRequest)
                        return;

                    byte[] dataToSign;

                    try
                    {
                        dataToSign = Hex.FromString(signRequest.Payload);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "{Sender}: Can't parse income payload to sign, {Payload}", "Beacon",
                            signRequest.Payload);

                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: message.SenderId,
                            response: new SignatureTypeNotSupportedBeaconError(signRequest.Id,
                                _beaconWalletClient.SenderId));

                        return;
                    }

                    var permissionInfo = await _beaconWalletClient
                        .PermissionInfoRepository
                        .TryReadBySenderIdAsync(message.SenderId);

                    async Task OnRejectOrCloseAction()
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

                    var signatureRequestViewModel = new SignatureRequestViewModel
                    {
                        DappName = permissionInfo!.AppMetadata.Name,
                        BytesPayload = signRequest.Payload,
                        OnSign = async () =>
                        {
                            var hdWallet = _atomexApp.Account.Wallet as HdWallet;

                            using var privateKey = hdWallet!.KeyStorage.GetPrivateKey(
                                currency: Tezos,
                                keyIndex: connectedWalletAddress.KeyIndex,
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
                        OnReject = OnRejectOrCloseAction,
                        OnClose = () => _ = OnRejectOrCloseAction()
                    };

                    App.DialogService.Show(signatureRequestViewModel);
                    break;
                }
            }
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