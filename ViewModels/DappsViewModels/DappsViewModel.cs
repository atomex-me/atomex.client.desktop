using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Wallet.Tezos;
using Atomex.Wallets.Tezos;
using Beacon.Sdk.BeaconClients;
using Beacon.Sdk.BeaconClients.Abstract;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Constants = Beacon.Sdk.Constants;
using Network = Atomex.Core.Network;
using Hex = Atomex.Common.Hex;
using ILogger = Serilog.ILogger;


namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class DappViewModel : ViewModelBase
    {
        public Peer Peer { get; set; }
        public PermissionInfo? PermissionInfo { get; set; }
        public string Name => Peer.Name;
        public string ConnectedAddress => Peer.ConnectedAddress;
        public Action<Peer> OnDisconnect { get; set; }
        public Action<Peer> OnDappClick { get; set; }

        private ReactiveCommand<Unit, Unit>? _openDappSiteCommand;

        public ReactiveCommand<Unit, Unit> OpenDappSiteCommand =>
            _openDappSiteCommand ??= ReactiveCommand.Create(() =>
            {
                if (PermissionInfo != null && Uri.TryCreate(PermissionInfo.Website, UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
            });

        private ReactiveCommand<Unit, Unit>? _disconnectCommand;

        public ReactiveCommand<Unit, Unit> DisconnectCommand =>
            _disconnectCommand ??= ReactiveCommand.Create(() => OnDisconnect?.Invoke(Peer));

        private ReactiveCommand<Unit, Unit>? _dappClickCommand;

        public ReactiveCommand<Unit, Unit> DappClickCommand =>
            _dappClickCommand ??= ReactiveCommand.Create(() => OnDappClick?.Invoke(Peer));

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

            ILogger serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();
            ILoggerProvider loggerProvider = new SerilogLoggerProvider(serilogLogger);

            _atomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
            Tezos = (TezosConfig)_atomexApp.Account.Currencies.GetByName(TezosConfig.Xtz);

            ConnectDappViewModel = new ConnectDappViewModel
            {
                OnBack = () => App.DialogService.Show(SelectAddressViewModel!),
                OnConnect = Connect
            };

            _ = Task.Run(async () =>
            {
                _beaconWalletClient = BeaconClientFactory.Create<IWalletBeaconClient>(beaconOptions, loggerProvider);
                _beaconWalletClient.OnBeaconMessageReceived += OnBeaconWalletClientMessageReceived;
                _beaconWalletClient.OnDappsListChanged += OnDappsListChanged;
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

        private void OnDappsListChanged(object? sender, DappConnectedEventArgs e)
        {
            GetAllDapps();
            Log.Information("{@Sender}: connected dapp: {@Dapp}", "Beacon", e?.dappMetadata.Name);
        }

        private void GetAllDapps()
        {
            var peers = _beaconWalletClient.GetAllPeers();

            Dapps = new ObservableCollection<DappViewModel>(
                peers.Select(peer => new DappViewModel
                {
                    Peer = peer,
                    PermissionInfo = _beaconWalletClient
                        .PermissionInfoRepository
                        .TryReadBySenderIdAsync(peer.SenderId).Result,
                    OnDisconnect = disconnectPeer =>
                    {
                        var disconnectViewModel = new DisconnectViewModel
                        {
                            DappName = disconnectPeer.Name,
                            OnDisconnect = async () => await _beaconWalletClient
                                .RemovePeerAsync(disconnectPeer.SenderId)
                        };

                        App.DialogService.Show(disconnectViewModel);
                    },
                    OnDappClick = clickedPeer =>
                    {
                        var permissionInfo = _beaconWalletClient
                            .PermissionInfoRepository
                            .TryReadBySenderIdAsync(clickedPeer.SenderId).Result;

                        if (permissionInfo == null)
                            return;

                        var showDappViewModel = new ShowDappViewModel
                        {
                            DappName = clickedPeer.Name,
                            DappId = clickedPeer.SenderId,
                            Address = clickedPeer.ConnectedAddress,
                            Permissions = permissionInfo.Scopes,
                            OnDisconnect = () =>
                            {
                                var disconnectViewModel = new DisconnectViewModel
                                {
                                    DappName = clickedPeer.Name,
                                    OnDisconnect = async () => await _beaconWalletClient
                                        .RemovePeerAsync(clickedPeer.SenderId)
                                };

                                App.DialogService.Show(disconnectViewModel);
                            }
                        };

                        App.DialogService.Show(showDappViewModel);
                    }
                }));
        }

        private async Task Connect(string qrCodeString, string addressToConnect)
        {
            if (_atomexApp.Account == null)
                return;

            var pairingRequest = GetPairingRequest();
            await _beaconWalletClient.AddPeerAsync(
                pairingRequest,
                addressToConnect);

            App.DialogService.Close();

            P2PPairingRequest GetPairingRequest()
            {
                var decodedQr = Base58.Parse(qrCodeString);
                var message = Encoding.UTF8.GetString(decodedQr.ToArray());
                return JsonConvert.DeserializeObject<P2PPairingRequest>(message);
            }
        }

        private async void OnBeaconWalletClientMessageReceived(object? sender, BeaconMessageEventArgs e)
        {
            var message = e.Request;
            var peer = _beaconWalletClient.GetPeer(message.SenderId);

            if (peer == null)
            {
                await _beaconWalletClient.SendResponseAsync(
                    receiverId: e.SenderId,
                    response: new BeaconAbortedError(message.Id, _beaconWalletClient.SenderId));
                return;
            }

            Log.Debug("{@Sender}: msg with type {@Type}, from {@Peer} received",
                "Beacon",
                peer.Name,
                message.Id);

            var connectedWalletAddress = await _atomexApp
                .Account
                .GetAddressAsync(Tezos.Name, peer.ConnectedAddress);

            switch (message.Type)
            {
                case BeaconMessageType.permission_request:
                {
                    if (message is not PermissionRequest permissionRequest)
                        return;

                    if (permissionRequest.Network.Type == NetworkType.mainnet &&
                        _atomexApp.Account.Network != Network.MainNet)
                    {
                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: e.SenderId,
                            response: new NetworkNotSupportedBeaconError(permissionRequest.Id,
                                _beaconWalletClient.SenderId));
                        return;
                    }

                    if (string.IsNullOrEmpty(permissionRequest.Network.RpcUrl))
                        permissionRequest.Network.RpcUrl = $"https://rpc.tzkt.io/{permissionRequest.Network.Type}";

                    if (string.IsNullOrEmpty(permissionRequest.Network.Name))
                        permissionRequest.Network.Name = string.Concat(
                            permissionRequest.Network.Type.ToString()[0].ToString().ToUpper(),
                            permissionRequest.Network.Type.ToString().AsSpan(1));

                    var response = new PermissionResponse(
                        id: permissionRequest.Id,
                        senderId: _beaconWalletClient.SenderId,
                        appMetadata: _beaconWalletClient.Metadata,
                        network: permissionRequest.Network,
                        scopes: permissionRequest.Scopes,
                        publicKey: PubKey.FromBase64(connectedWalletAddress.PublicKey).ToString(),
                        address: connectedWalletAddress.Address,
                        version: permissionRequest.Version);

                    var permissionRequestViewModel = new PermissionRequestViewModel
                    {
                        DappName = permissionRequest.AppMetadata.Name,
                        Address = connectedWalletAddress.Address,
                        Permissions = permissionRequest.Scopes,
                        OnReject = async () =>
                        {
                            await _beaconWalletClient.SendResponseAsync(
                                receiverId: e.SenderId,
                                response: new BeaconAbortedError(permissionRequest.Id, _beaconWalletClient.SenderId));
                            await _beaconWalletClient.RemovePeerAsync(
                                message.SenderId);

                            App.DialogService.Close();
                            Log.Information(
                                "{@Sender}: Rejected permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                                "Beacon",
                                permissionRequest.Scopes.Aggregate(string.Empty,
                                    (res, scope) => res + $"{scope}, "),
                                permissionRequest.AppMetadata.Name, connectedWalletAddress.Address);
                        },
                        OnAllow = async () =>
                        {
                            await _beaconWalletClient.SendResponseAsync(
                                receiverId: e.SenderId,
                                response);

                            App.DialogService.Close();
                            Log.Information(
                                "{@Sender}: Issued permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                                "Beacon",
                                permissionRequest.Scopes.Aggregate(string.Empty,
                                    (res, scope) => res + $"{scope}, "),
                                permissionRequest.AppMetadata.Name, connectedWalletAddress.Address);
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
                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: e.SenderId,
                            response: new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));

                        return;
                    }

                    var rpc = new Rpc(Tezos.RpcNodeUri);

                    var head = await rpc
                        .GetHeader()
                        .ConfigureAwait(false);

                    var managerKey = await rpc
                        .GetManagerKey(connectedWalletAddress.Address)
                        .ConfigureAwait(false);

                    var revealed = managerKey.Value<string>() != null;

                    var account = await rpc
                        .GetAccountForBlock(head["hash"]!.ToString(), connectedWalletAddress.Address)
                        .ConfigureAwait(false);

                    var counter = int.Parse(account["counter"].ToString());

                    var operations = new List<ManagerOperationContent>();

                    if (!revealed)
                    {
                        operations.Add(new RevealContent
                        {
                            Counter = ++counter,
                            Fee = 0,
                            GasLimit = 1_000_000,
                            Source = connectedWalletAddress.Address,
                            PublicKey = PubKey.FromBase64(connectedWalletAddress.PublicKey).ToString(),
                            StorageLimit = 0
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

                        if (o.Parameters == null) return txContent;
                        
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
                        
                        return txContent;
                    }));

                    var error = await TezosOperationFiller
                        .AutoFillAsync(
                            operations,
                            head["hash"]!.ToString(),
                            head["chain_id"]!.ToString(),
                            Tezos)
                        .ConfigureAwait(false);

                    if (error != null)
                    {
                        await _beaconWalletClient.SendResponseAsync(
                            receiverId: e.SenderId,
                            response: new TransactionInvalidBeaconError(operationRequest.Id,
                                _beaconWalletClient.SenderId));

                        Log.Error("{@Sender}: error during AutoFill transaction, {@Msg}", "Beacon", error.Description);
                        return;
                    }

                    var forgedOperations = await TezosForge.ForgeAsync(
                        operations: operations,
                        branch: head["hash"]!.ToString());

                    var operationsViewModel = new ObservableCollection<BaseBeaconOperationViewModel>();

                    foreach (var item in operations.Select((value, idx) => new { idx, value }))
                    {
                        var operation = item.value;
                        var index = item.idx;

                        switch (operation)
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
                        DappName = peer.Name,
                        DappLogo = permissionInfo.AppMetadata.Icon,
                        Operations = operationsViewModel,
                        OnReject = async () =>
                        {
                            await _beaconWalletClient.SendResponseAsync(
                                receiverId: e.SenderId,
                                response: new BeaconAbortedError(operationRequest.Id, _beaconWalletClient.SenderId));

                            App.DialogService.Close();
                        },

                        OnConfirm = async () =>
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
                                    receiverId: e.SenderId,
                                    response: new TransactionInvalidBeaconError(operationRequest.Id,
                                        _beaconWalletClient.SenderId));

                                return;
                            }

                            string operationId = null;

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
                                    receiverId: e.SenderId,
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
                                receiverId: e.SenderId,
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
                    catch (Exception)
                    {
                        // data is not in HEX format
                        dataToSign = Encoding.UTF8.GetBytes(signRequest.Payload);
                    }

                    var permissionInfo = await _beaconWalletClient
                        .PermissionInfoRepository
                        .TryReadBySenderIdAsync(message.SenderId);

                    var signatureRequestViewModel = new SignatureRequestViewModel()
                    {
                        DappName = permissionInfo!.AppMetadata.Name,
                        Payload = signRequest.Payload,
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
                                receiverId: e.SenderId,
                                response);

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
                                receiverId: e.SenderId,
                                response: new BeaconAbortedError(signRequest.Id, _beaconWalletClient.SenderId));

                            App.DialogService.Close();

                            Log.Information(
                                "{@Sender}: user Aborted signing payload from {@Dapp}",
                                "Beacon",
                                permissionInfo.AppMetadata.Name);
                        }
                    };

                    App.DialogService.Show(signatureRequestViewModel);
                    break;
                }
                case BeaconMessageType.broadcast_request:
                    break;
                case BeaconMessageType.acknowledge:
                    break;
                case BeaconMessageType.disconnect:
                    break;
                case BeaconMessageType.error:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            if (args.AtomexClient != null && _atomexApp.Account != null)
                return;

            _beaconWalletClient.Disconnect();
            _atomexApp.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
            _beaconWalletClient.OnBeaconMessageReceived -= OnBeaconWalletClientMessageReceived;
            _beaconWalletClient.OnDappsListChanged -= OnDappsListChanged;
        }

        private void DesignerMode()
        {
        }
    }
}