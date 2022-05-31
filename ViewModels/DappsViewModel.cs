using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Cryptography;
using Atomex.Services;
using Atomex.ViewModels;
using Atomex.Wallet;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Beacon.Sdk;
using Beacon.Sdk.Beacon;
using Beacon.Sdk.Beacon.Operation;
using Beacon.Sdk.Beacon.Permission;
using Matrix.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Netezos.Keys;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class DappViewModel : ViewModelBase
    {
        public string Logo { get; set; }
        public IBitmap BitmapLogo => App.ImageService.GetImage(Logo);
        public string Name { get; set; }
        public string ConnectedAddress { get; set; }
        public DateTime ConnectTime { get; set; }


        private ReactiveCommand<Unit, Unit> _openInExplorerCommand;

        public ReactiveCommand<Unit, Unit> OpenInExplorerCommand => _openInExplorerCommand ??= ReactiveCommand.Create(
            () => { Log.Information(ConnectedAddress); });

        private ReactiveCommand<Unit, Unit> _copyCommand;

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
        public WalletAddressViewModel AddressToConnect { get; set; }
        public ConnectDappViewModel ConnectDappViewModel { get; set; }

        public DappsViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }

        public DappsViewModel(IAtomexApp atomexApp)
        {
            AtomexApp = atomexApp ?? throw new ArgumentNullException(nameof(atomexApp));

            var beaconServices = new ServiceCollection();
            beaconServices.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            beaconServices.AddMatrixClient();
            beaconServices.AddBeaconClient();
            BeaconServicesProvider = beaconServices.BuildServiceProvider();

            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;

            Tezos = (TezosConfig)AtomexApp.Account.Currencies.GetByName(TezosConfig.Xtz);
            SelectAddressViewModel =
                new SelectAddressViewModel(AtomexApp.Account, Tezos, SelectAddressMode.Connect)
                {
                    BackAction = () => { App.DialogService.Close(); },
                    ConfirmAction = walletAddressViewModel =>
                    {
                        AddressToConnect = walletAddressViewModel;
                        ConnectDappViewModel!.AddressToConnect = AddressToConnect.Address;
                        App.DialogService.Show(ConnectDappViewModel!);
                    }
                };

            ConnectDappViewModel = new ConnectDappViewModel
            {
                OnBack = () => App.DialogService.Show(SelectAddressViewModel),
                OnConnect = Connect
            };

            _ = Task.Run(async () =>
            {
                if (AtomexApp.AtomexClient.Account == null) return;
                
                BeaconWalletClient = BeaconServicesProvider.GetRequiredService<IWalletBeaconClient>();
                BeaconWalletClient.OnBeaconMessageReceived += OnBeaconWalletClientMessageReceived;
                await BeaconWalletClient.InitAsync();
                BeaconWalletClient.Connect();
                
                Log.Debug("{@Sender}: WalletClient connected {@Connected}", "Beacon", BeaconWalletClient.Connected);
                Log.Debug("{@Sender}: WalletClient logged in {@LoggedIn}", "Beacon", BeaconWalletClient.LoggedIn);
            });
        }

        private async void Connect(string qrCodeString)
        {
            if (AtomexApp.AtomexClient.Account == null) return;
            
            Log.Debug("{@Sender}: WalletClient connected {@Connected}", "Beacon", BeaconWalletClient.Connected);
            Log.Debug("{@Sender}: WalletClient logged in {@LoggedIn}", "Beacon", BeaconWalletClient.LoggedIn);

            var pairingRequest = ConnectToPeer();
            await BeaconWalletClient.AddPeerAsync(pairingRequest);

            P2PPairingRequest ConnectToPeer()
            {
                var decodedQr = Base58Check.Decode(qrCodeString);
                var message = Encoding.UTF8.GetString(decodedQr.ToArray());
                return JsonConvert.DeserializeObject<P2PPairingRequest>(message);
            }
        }
        
        private void OnBeaconWalletClientMessageReceived(object? sender, BeaconMessageEventArgs e)
        {
            var message = e.Request;

            Log.Debug("{@Sender}: msg with type {@Type}, id {@Id} received",
                "Beacon",
                message.Type,
                message.Id);

            switch (message.Type)
            {
                case BeaconMessageType.permission_request:
                {
                    var request = message as PermissionRequest;

                    var network = request!.Network.Type switch
                    {
                        NetworkType.mainnet => new Network
                        {
                            Type = NetworkType.mainnet,
                            Name = "Mainnet",
                            RpcUrl = "https://rpc.tzkt.io/mainnet"
                        },
                        _ => new Network
                        {
                            Type = NetworkType.ithacanet,
                            Name = "Ithacanet",
                            RpcUrl = "https://rpc.tzkt.io/ithacanet"
                        }
                    };

                    // change response sign to encrypt permission
                    var scopes = request.Scopes
                        .Select(s => s == PermissionScope.sign ? PermissionScope.encrypt : s)
                        .ToList();
                    
                    var hdWallet = AtomexApp.Account.Wallet as HdWallet;
                    
                    using var privateKey = hdWallet!.KeyStorage.GetPrivateKey(
                        currency: Tezos,
                        keyIndex: AddressToConnect.WalletAddress.KeyIndex,
                        keyType: AddressToConnect.WalletAddress.KeyType);
                    
                    var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();
                    
                    var base58Private = unsecuredPrivateKey.Length == 32
                        ? Base58Check.Encode(unsecuredPrivateKey, Prefix.Edsk)
                        : Base58Check.Encode(unsecuredPrivateKey, Prefix.EdskSecretKey);
                    
                    var walletKey = Key.FromBase58(base58Private);

                    var response = new PermissionResponse(
                        id: request.Id,
                        senderId: BeaconWalletClient.SenderId,
                        appMetadata: BeaconWalletClient.Metadata,
                        network: network,
                        scopes: scopes,
                        publicKey: walletKey.PubKey.ToString(),
                        address: walletKey.PubKey.Address,
                        version: request.Version);

                    _ = BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);
                    break;
                }
                case BeaconMessageType.operation_request:
                {
                    var request = message as OperationRequest;

                    if (request!.OperationDetails.Count <= 0) return;

                    var operation = request.OperationDetails[0];

                    if (long.TryParse(operation?.Amount, out var amount))
                    {
                        // string transactionHash =
                        //     await MakeTransactionAsync(walletKey, operation.Destination, amount);

                        var response = new OperationResponse(
                            id: request.Id,
                            senderId: BeaconWalletClient.SenderId,
                            transactionHash: "txHash",
                            request.Version);

                        _ = BeaconWalletClient.SendResponseAsync(receiverId: e.SenderId, response);
                    }

                    break;
                }
                case BeaconMessageType.sign_payload_request:
                    break;
                case BeaconMessageType.broadcast_request:
                    break;
                case BeaconMessageType.permission_response:
                    break;
                case BeaconMessageType.sign_payload_response:
                    break;
                case BeaconMessageType.operation_response:
                    break;
                case BeaconMessageType.broadcast_response:
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
            if (args.AtomexClient?.Account == null) return;
        }

        private void DesignerMode()
        {
            Dapps = new ObservableCollection<DappViewModel>
            {
                new()
                {
                    Logo = "atomex_logo",
                    Name = "objkt.com",
                    ConnectedAddress = "tz1gh1J44YMxugkf7AZ8q1MQZEu88T4RVa4i",
                    ConnectTime = DateTime.Now
                },
                new()
                {
                    Logo = "atomex_logo",
                    Name = "Plenty",
                    ConnectedAddress = "tz1gh1J44YMxugkf7AZ8q1MQZEu88T4RVa4i",
                    ConnectTime = DateTime.Now
                }
            };
        }
    }
}