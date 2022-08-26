using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Common;
using Atomex.Cryptography;
using Atomex.Wallet;
using Avalonia.Controls;
using Avalonia.Threading;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Error;
using Beacon.Sdk.Beacon.Operation;
using Beacon.Sdk.Beacon.Permission;
using Beacon.Sdk.Beacon.Sign;
using Beacon.Sdk.Core.Domain.Entities;
using Matrix.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Netezos.Keys;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Network = Atomex.Core.Network;

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
        private readonly IAtomexApp AtomexApp;
        private IWalletBeaconClient BeaconWalletClient;
        private ServiceProvider BeaconServicesProvider;

        [Reactive] public ObservableCollection<DappViewModel> Dapps { get; set; }
        public TezosConfig Tezos { get; set; }
        public SelectAddressViewModel SelectAddressViewModel { get; set; }
        public ConnectDappViewModel ConnectDappViewModel { get; set; }

        public DappsViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }

        public DappsViewModel(IAtomexApp atomexApp)
        {
            AtomexApp = atomexApp ?? throw new ArgumentNullException(nameof(atomexApp));
            if (AtomexApp.Account == null) return;

            var beaconServices = new ServiceCollection();
            beaconServices.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            beaconServices.AddMatrixClient();
            beaconServices.AddBeaconClient(pathToDb: Path.GetDirectoryName(AtomexApp.Account.Wallet.PathToWallet)!,
                appName: "Atomex desktop");
            BeaconServicesProvider = beaconServices.BuildServiceProvider();

            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
            Tezos = (TezosConfig)AtomexApp.Account.Currencies.GetByName(TezosConfig.Xtz);

            SelectAddressViewModel = new SelectAddressViewModel(AtomexApp.Account, Tezos, SelectAddressMode.Connect)
            {
                BackAction = () => { App.DialogService.Close(); },
                ConfirmAction = walletAddressViewModel =>
                {
                    ConnectDappViewModel!.AddressToConnect = walletAddressViewModel.Address;
                    App.DialogService.Show(ConnectDappViewModel);
                }
            };

            ConnectDappViewModel = new ConnectDappViewModel
            {
                OnBack = () => App.DialogService.Show(SelectAddressViewModel),
                OnConnect = Connect
            };

            _ = Task.Run(async () =>
            {
                BeaconWalletClient = BeaconServicesProvider.GetRequiredService<IWalletBeaconClient>();
                BeaconWalletClient.OnBeaconMessageReceived += OnBeaconWalletClientMessageReceived;
                BeaconWalletClient.OnDappsListChanged += OnDappsListChanged;
                await BeaconWalletClient.InitAsync();
                BeaconWalletClient.Connect();
                GetAllDapps();

                Log.Debug("{@Sender}: WalletClient connected {@Connected}", "Beacon", BeaconWalletClient.Connected);
                Log.Debug("{@Sender}: WalletClient logged in {@LoggedIn}", "Beacon", BeaconWalletClient.LoggedIn);
            });
        }

        private void OnDappsListChanged(object? sender, DappConnectedEventArgs e)
        {
            GetAllDapps();
            Log.Information("{@Sender}: connected dapp: {@Dapp}", "Beacon", e?.dappMetadata.Name);
        }

        private void GetAllDapps()
        {
            var peers = BeaconWalletClient.GetAllPeers();

            Dapps = new ObservableCollection<DappViewModel>(
                peers.Select(peer => new DappViewModel
                {
                    Peer = peer,
                    PermissionInfo = BeaconWalletClient.PermissionInfoRepository
                        .TryReadBySenderIdAsync(peer.SenderId).Result,
                    OnDisconnect = disconnectPeer =>
                    {
                        var disconnectViewModel = new DisconnectViewModel
                        {
                            DappName = disconnectPeer.Name,
                            OnDisconnect = async () => await BeaconWalletClient.RemovePeerAsync(disconnectPeer.SenderId)
                        };

                        App.DialogService.Show(disconnectViewModel);
                    },
                    OnDappClick = clickedPeer =>
                    {
                        var permissionInfo = BeaconWalletClient.PermissionInfoRepository
                            .TryReadBySenderIdAsync(clickedPeer.SenderId).Result;
                        if (permissionInfo == null) return;

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
                                    OnDisconnect = async () =>
                                        await BeaconWalletClient.RemovePeerAsync(clickedPeer.SenderId)
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
            if (AtomexApp.Account == null) return;
            var pairingRequest = GetPairingRequest();
            await BeaconWalletClient.AddPeerAsync(pairingRequest, addressToConnect);
            App.DialogService.Close();

            P2PPairingRequest GetPairingRequest()
            {
                var decodedQr = Base58Check.Decode(qrCodeString);
                var message = Encoding.UTF8.GetString(decodedQr.ToArray());
                return JsonConvert.DeserializeObject<P2PPairingRequest>(message);
            }
        }

        private async void OnBeaconWalletClientMessageReceived(object? sender, BeaconMessageEventArgs e)
        {
            var message = e.Request;
            var peer = BeaconWalletClient.GetPeer(message.SenderId);

            if (peer == null)
            {
                _ = BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                    new BeaconAbortedError(message.Id, BeaconWalletClient.SenderId));
                return;
            }

            Log.Debug("{@Sender}: msg with type {@Type}, from {@Peer} received",
                "Beacon",
                peer.Name,
                message.Id);

            var connectedWalletAddress = await AtomexApp.Account
                .GetAddressAsync(Tezos.Name, peer.ConnectedAddress);

            switch (message.Type)
            {
                case BeaconMessageType.permission_request:
                {
                    if (message is not PermissionRequest permissionRequest) return;

                    if (permissionRequest.Network.Type == NetworkType.mainnet &&
                        AtomexApp.Account.Network != Network.MainNet)
                    {
                        // todo: change response Error type;
                        _ = BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                            new BeaconAbortedError(permissionRequest.Id, BeaconWalletClient.SenderId));
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
                        senderId: BeaconWalletClient.SenderId,
                        appMetadata: BeaconWalletClient.Metadata,
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
                            await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                new BeaconAbortedError(permissionRequest.Id, BeaconWalletClient.SenderId));
                            await BeaconWalletClient.RemovePeerAsync(message.SenderId);
                            App.DialogService.Close();
                            Log.Information(
                                "{@Sender}: Rejected permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                                "Beacon",
                                permissionRequest.Scopes.Aggregate(string.Empty,
                                    (res, scope) => res + $"{scope.ToString()}, "),
                                permissionRequest.AppMetadata.Name, connectedWalletAddress.Address);
                        },
                        OnAllow = async () =>
                        {
                            await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);
                            App.DialogService.Close();
                            Log.Information(
                                "{@Sender}: Issued permissions [{@PermissionsList}] to dapp {@Dapp} with address {@Address}",
                                "Beacon",
                                permissionRequest.Scopes.Aggregate(string.Empty,
                                    (res, scope) => res + $"{scope.ToString()}, "),
                                permissionRequest.AppMetadata.Name, connectedWalletAddress.Address);
                        }
                    };

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        App.DialogService.Show(permissionRequestViewModel);
                    });
                    break;
                }
                case BeaconMessageType.operation_request:
                {
                    if (message is not OperationRequest operationRequest) return;
                    var permissionInfo = BeaconWalletClient
                        .PermissionInfoRepository
                        .TryReadBySenderIdAsync(operationRequest.SenderId)
                        .Result;

                    if (operationRequest.OperationDetails.Count != 1 || permissionInfo == null)
                    {
                        await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                            new BeaconAbortedError(operationRequest.Id, BeaconWalletClient.SenderId));
                        return;
                    }

                    var operation = operationRequest.OperationDetails[0];

                    if (long.TryParse(operation.Amount, out var amount))
                    {
                        var autofillOperation = await RunAutofillOperation(
                            fromAddress: connectedWalletAddress.Address,
                            toAddress: operation.Destination,
                            amount: amount,
                            operationParams: operation.Parameters!);

                        if (autofillOperation.HasError)
                        {
                            Log.Error("Autofill error");
                            _ = BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                new BeaconAbortedError(operationRequest.Id, BeaconWalletClient.SenderId));
                            return;
                        }

                        var (tx, isSuccess, isRunSuccess) = autofillOperation.Value;

                        if (!isSuccess || !isRunSuccess)
                        {
                            Log.Error("Autofill isSuccess {@IsSuccess} isRunSuccess {@IsRunSuccess}", isSuccess,
                                isRunSuccess);
                            _ = BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                new BeaconAbortedError(operationRequest.Id, BeaconWalletClient.SenderId));
                            return;
                        }

                        tx.GasLimit = tx.Operations?.Last?["gas_limit"]?.Value<decimal>() ?? Tezos.GasLimit;
                        tx.StorageLimit = tx.Operations?.Last?["storage_limit"]?.Value<decimal>() ?? Tezos.StorageLimit;

                        var operationRequestViewModel = new OperationRequestViewModel(AtomexApp, Tezos)
                        {
                            DappName = peer.Name,
                            DappLogo = permissionInfo.AppMetadata.Icon,
                            Transaction = tx,
                            OnReject = async () =>
                            {
                                await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                    new BeaconAbortedError(operationRequest.Id, BeaconWalletClient.SenderId));
                                App.DialogService.Close();
                            },

                            OnConfirm = async () =>
                            {
                                var wallet = (HdWallet)AtomexApp.Account.Wallet;
                                var keyStorage = wallet.KeyStorage;

                                var signResult = await tx
                                    .SignAsync(keyStorage, connectedWalletAddress, Tezos);

                                if (!signResult)
                                {
                                    Log.Error("Beacon transaction signing error");
                                    _ = BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                        new BeaconAbortedError(operationRequest.Id, BeaconWalletClient.SenderId));
                                    return;
                                }

                                var result = await Tezos.BlockchainApi
                                    .TryBroadcastAsync(tx);

                                if (result.Error != null)
                                {
                                    Log.Error("Beacon transaction broadcast error");
                                    await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                        new BeaconAbortedError(operationRequest.Id, BeaconWalletClient.SenderId));
                                    return;
                                }

                                var response = new OperationResponse(
                                    id: operationRequest.Id,
                                    senderId: BeaconWalletClient.SenderId,
                                    transactionHash: result.Value,
                                    operationRequest.Version);

                                await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);

                                App.DialogService.Close();
                                Log.Information("{@Sender}: operation done with transaction hash: {@Hash}", "Beacon",
                                    result.Value);
                            }
                        };
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            App.DialogService.Show(operationRequestViewModel);
                        });
                    }
                    else
                    {
                        Log.Error("Cant parse amount from operation request");
                    }

                    break;
                }
                case BeaconMessageType.sign_payload_request:
                {
                    if (message is not SignPayloadRequest signRequest) return;

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

                    var permissionInfo =
                        await BeaconWalletClient.PermissionInfoRepository.TryReadBySenderIdAsync(message.SenderId);

                    var signatureRequestViewModel = new SignatureRequestViewModel()
                    {
                        DappName = permissionInfo!.AppMetadata.Name,
                        Payload = signRequest.Payload,
                        OnSign = async () =>
                        {
                            var hdWallet = AtomexApp.Account.Wallet as HdWallet;

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
                                senderId: BeaconWalletClient.SenderId);

                            await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);
                            App.DialogService.Close();
                            Log.Information("{@Sender}: signed payload for {@Dapp} with signature: {@Signature}",
                                "Beacon", permissionInfo.AppMetadata.Name, signedMessage.EncodedSignature);
                        },
                        OnReject = async () =>
                        {
                            await BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId,
                                new BeaconAbortedError(signRequest.Id, BeaconWalletClient.SenderId));
                            App.DialogService.Close();
                            Log.Information("{@Sender}: user Aborted signing payload from {@Dapp}", "Beacon",
                                permissionInfo.AppMetadata.Name);
                        }
                    };

                    await Dispatcher.UIThread.InvokeAsync(() => { App.DialogService.Show(signatureRequestViewModel); });
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

        private async Task<Result<(TezosTransaction tx, bool isSuccess, bool isRunSuccess)>> RunAutofillOperation(
            string fromAddress,
            string toAddress,
            long amount,
            JObject operationParams,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var tx = new TezosTransaction
                {
                    From = fromAddress,
                    To = toAddress,
                    Fee = 0,
                    GasLimit = 1_000_000,
                    StorageLimit = 5000,
                    Amount = amount,
                    Currency = Tezos.Name,
                    CreationTime = DateTime.UtcNow,
                    Params = operationParams,

                    UseRun = true,
                    UseSafeStorageLimit = false,
                    UseOfflineCounter = false
                };

                var walletAddress = AtomexApp.Account
                    .GetCurrencyAccount(TezosConfig.Xtz)
                    .GetAddressAsync(fromAddress, cancellationToken)
                    .WaitForResult();

                using var securePublicKey = AtomexApp.Account.Wallet.GetPublicKey(
                    currency: Tezos,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                var (isSuccess, isRunSuccess, _) = await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: Tezos,
                    headOffset: TezosConfig.HeadOffset,
                    cancellationToken: cancellationToken);

                return (tx, isSuccess, isRunSuccess);
            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill transaction error");
                return new Error(Errors.TransactionCreationError, "Autofill transaction error. Try again later.");
            }
        }

        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            if (args.AtomexClient != null && AtomexApp.Account != null) return;

            BeaconWalletClient.Disconnect();
            AtomexApp.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
            BeaconWalletClient.OnBeaconMessageReceived -= OnBeaconWalletClientMessageReceived;
            BeaconWalletClient.OnDappsListChanged -= OnDappsListChanged;
        }

        private void DesignerMode()
        {
        }
    }
}