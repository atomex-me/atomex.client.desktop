using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Desktop.ViewModels.ReceiveViewModels;
using Atomex.Core;
using Atomex.Wallet;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Serilog;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class Delegation
    {
        public BakerData Baker { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
        public IBitmap GetBakerLogo => App.ImageService.GetImage(Baker.Logo);
    }

    public class TezosWalletViewModel : WalletViewModel
    {
        private const int DelegationCheckIntervalInSec = 20;

        private bool _canDelegate;
        public bool CanDelegate
        {
            get => _canDelegate;
            set { _canDelegate = value; this.RaisePropertyChanged(nameof(CanDelegate)); }
        }

        private bool _hasDelegations;
        public bool HasDelegations
        {
            get => _hasDelegations;
            set { _hasDelegations = value; this.RaisePropertyChanged(nameof(HasDelegations)); }
        }

        private List<Delegation> _delegations;
        public List<Delegation> Delegations
        {
            get => _delegations;
            set { _delegations = value; this.RaisePropertyChanged(nameof(Delegations)); }
        }

        public TezosWalletViewModel()
            : base()
        {
        }

        public DelegateViewModel DelegateVM { get; set; }

        public TezosWalletViewModel(IAtomexApp app, Action<CurrencyConfig> setConversionTab, CurrencyConfig currency)
            : base(app, setConversionTab, currency)
        {
            Delegations = new List<Delegation>();
            
            _ = LoadDelegationInfoAsync();
            
            DelegateVM = new DelegateViewModel(App, async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(DelegationCheckIntervalInSec))
                    .ConfigureAwait(false);
            
                await Dispatcher.UIThread.InvokeAsync(OnUpdateClick);
            });
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name == args.Currency)
                {
                    // update transactions list
                    await LoadTransactionsAsync();

                    // update delegation info
                    await LoadDelegationInfoAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async Task LoadDelegationInfoAsync()
        {
            try
            {
                var tezos = Currency as TezosConfig;

                var balance = await App.Account
                    .GetBalanceAsync(tezos.Name)
                    .ConfigureAwait(false);

                var addresses = await App.Account
                    .GetUnspentAddressesAsync(tezos.Name)
                    .ConfigureAwait(false);

                var rpc = new Rpc(tezos.RpcNodeUri);

                var delegations = new List<Delegation>();

                foreach (var wa in addresses)
                {
                    var accountData = await rpc
                        .GetAccount(wa.Address)
                        .ConfigureAwait(false);

                    var @delegate = accountData["delegate"]?.ToString();

                    if (string.IsNullOrEmpty(@delegate))
                        continue;

                    var baker = await BbApi
                        .GetBaker(@delegate, App.Account.Network)
                        .ConfigureAwait(false) ?? new BakerData { Address = @delegate };

                    delegations.Add(new Delegation
                    {
                        Baker = baker,
                        Address = wa.Address,
                        Balance = wa.Balance
                    });

                    if (!string.IsNullOrEmpty(baker.Logo))
                    {
                        _ = Task.Run(() =>
                        {
                            _ = Desktop.App.ImageService.LoadImageFromUrl(baker.Logo);
                        });
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CanDelegate = balance.Available > 0;
                    Delegations = delegations;
                    HasDelegations = delegations.Count > 0;
                },
                DispatcherPriority.Background);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadDelegationInfoAsync canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadDelegationInfoAsync error.");
            }
        }

        private ICommand _delegateCommand;
        public ICommand DelegateCommand => _delegateCommand ??= (_delegateCommand = ReactiveCommand.Create(OnDelegateClick));

        //private ICommand _undelegateCommand;
        //public ICommand UndelegateCommand => _undelegateCommand ?? (_undelegateCommand = new Command(OnUndelegateClick));

        private async void OnDelegateClick()
        {
            _ = DialogHost.DialogHost.Show(DelegateVM);
            await Task.Delay(2000);
            var receiveViewModel = new ReceiveViewModel(App, Currency);
            DialogHost.DialogHost.GetDialogSession("MainDialogHost")?.Close(false);
            _ = DialogHost.DialogHost.Show(receiveViewModel, Desktop.App.MainDialogHostIdentifier);
            await Task.Delay(2000);
            DialogHost.DialogHost.GetDialogSession("MainDialogHost")?.Close(false);
            _ = DialogHost.DialogHost.Show(DelegateVM);
            await Task.Delay(2000);
            DialogHost.DialogHost.GetDialogSession("MainDialogHost")?.Close(false);
            _ = DialogHost.DialogHost.Show(receiveViewModel, Desktop.App.MainDialogHostIdentifier);
        }

        //private void OnUndelegateClick()
        //{
        //}

        protected override void DesignerMode()
        {
            base.DesignerMode();

            Delegations = new List<Delegation>()
            {
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                }
            };

            HasDelegations = true;
        }
    }
}