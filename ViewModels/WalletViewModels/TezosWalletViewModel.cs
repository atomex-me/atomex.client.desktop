using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;
using ReactiveUI;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Blockchain.Tezos.Tzkt;
using Atomex.Core;
using Atomex.Wallet;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosWalletViewModel : WalletViewModel
    {
        private const int DelegationCheckIntervalInSec = 20;

        [Reactive] public bool CanDelegate { get; set; }
        [Reactive] public bool HasDelegations { get; set; }
        [Reactive] public ObservableCollection<Delegation> Delegations { get; set; }
        private DelegateViewModel DelegateViewModel { get; set; }

        public TezosWalletViewModel()
            : base()
        {
        }

        public TezosWalletViewModel(IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            Action<string> setWertCurrency,
            CurrencyConfig currency)
            : base(app, setConversionTab, setWertCurrency, currency)
        {
            Delegations = new ObservableCollection<Delegation>();

            _ = LoadDelegationInfoAsync();

            DelegateViewModel = new DelegateViewModel(_app, async () =>
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
                if (Currency.Name != args.Currency) return;

                // update transactions list
                await LoadTransactionsAsync();

                // update delegation info
                await LoadDelegationInfoAsync();
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

                var balance = await _app.Account
                    .GetBalanceAsync(tezos.Name)
                    .ConfigureAwait(false);

                var addresses = await _app.Account
                    .GetUnspentAddressesAsync(tezos.Name)
                    .ConfigureAwait(false);

                var rpc = new Rpc(tezos.RpcNodeUri);

                var delegations = new List<Delegation>();
                
                var tzktApi = new TzktApi(tezos);
                var head = await tzktApi.GetHeadLevelAsync();
                var headLevel = head.Value;
                    
                var currentCycle = _app.Account.Network == Network.MainNet
                    ? Math.Floor((headLevel - 1) / 4096)
                    : Math.Floor((headLevel - 1) / 2048);

                foreach (var wa in addresses)
                {
                    var accountData = await rpc
                        .GetAccount(wa.Address)
                        .ConfigureAwait(false);

                    var @delegate = accountData["delegate"]?.ToString();

                    if (string.IsNullOrEmpty(@delegate))
                        continue;

                    var baker = await BbApi
                        .GetBaker(@delegate, _app.Account.Network)
                        .ConfigureAwait(false) ?? new BakerData { Address = @delegate };
                        
                    var account = await tzktApi.GetAccountByAddressAsync(wa.Address);

                    var txCycle = _app.Account.Network == Network.MainNet
                        ? Math.Floor((account.Value.DelegationLevel - 1) / 4096)
                        : Math.Floor((account.Value.DelegationLevel - 1) / 2048);

                    delegations.Add(new Delegation
                    {
                        Baker = baker,
                        Address = wa.Address,
                        Balance = wa.Balance,
                        DelegationTime = account.Value.DelegationTime,
                        Status = currentCycle - txCycle < 2 ? DelegationStatus.Pending :
                            currentCycle - txCycle < 7 ? DelegationStatus.Confirmed :
                            DelegationStatus.Active
                    });

                    if (!string.IsNullOrEmpty(baker.Logo))
                    {
                        _ = Task.Run(() => { _ = App.ImageService.LoadImageFromUrl(baker.Logo); });
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        CanDelegate = balance.Available > 0;
                        Delegations = new ObservableCollection<Delegation>(delegations);
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

        private ReactiveCommand<Unit, Unit> _delegateCommand;

        public ReactiveCommand<Unit, Unit> DelegateCommand =>
            _delegateCommand ??= (_delegateCommand = ReactiveCommand.Create(OnDelegateClick));

        private void OnDelegateClick()
        {
            App.DialogService.Show(DelegateViewModel);
        }

#if DEBUG
        protected override void DesignerMode()
        {
            base.DesignerMode();

            Delegations = new ObservableCollection<Delegation>()
            {
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                }
            };

            HasDelegations = true;
        }
#endif
    }
}