using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;
using ReactiveUI;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Blockchain.Tezos.Tzkt;
using Atomex.Client.Desktop.Common;
using Atomex.Core;
using Atomex.Wallet;
using ReactiveUI.Fody.Helpers;


namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public enum DelegationSortField
    {
        ByRoi,
        ByStatus
    }

    public class TezosWalletViewModel : WalletViewModel
    {
        private const int DelegationCheckIntervalInSec = 20;
        [Reactive] public ObservableCollection<Delegation> Delegations { get; set; }
        [Reactive] public SortDirection? CurrentDelegationSortDirection { get; set; }
        [Reactive] public DelegationSortField? CurrentDelegationSortField { get; set; }
        [Reactive] public bool SortByRoiAndAsc { get; set; }
        [Reactive] public bool SortByRoiAndDesc { get; set; }
        [Reactive] public bool SortDelegationByStatusAndAsc { get; set; }
        [Reactive] public bool SortDelegationByStatusAndDesc { get; set; }
        [Reactive] public bool SortByRoi { get; set; }
        [Reactive] public bool SortDelegationByStatus { get; set; }

        private bool CanDelegate { get; set; }
        private bool HasDelegations { get; set; }
        private DelegateViewModel DelegateViewModel { get; set; }
        private TezosConfig? Tezos { get; set; }

        public TezosWalletViewModel()
            : base()
        {
        }

        public TezosWalletViewModel(IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            Action<string> setWertCurrency,
            Action<ViewModelBase?> showRightPopupContent,
            CurrencyConfig currency)
            : base(app, setConversionTab, setWertCurrency, showRightPopupContent, currency)
        {
            Tezos = currency as TezosConfig;
            Delegations = new ObservableCollection<Delegation>();

            this.WhenAnyValue(
                    vm => vm.CurrentDelegationSortDirection,
                    vm => vm.CurrentDelegationSortField)
                .WhereAllNotNull()
                .SubscribeInMainThread(_ =>
                {
                    SortDelegations(Delegations);

                    SortByRoiAndAsc =
                        CurrentDelegationSortField == DelegationSortField.ByRoi &&
                        CurrentDelegationSortDirection == SortDirection.Asc;
                    SortByRoiAndDesc =
                        CurrentDelegationSortField == DelegationSortField.ByRoi &&
                        CurrentDelegationSortDirection == SortDirection.Desc;
                    SortDelegationByStatusAndAsc =
                        CurrentDelegationSortField == DelegationSortField.ByStatus &&
                        CurrentDelegationSortDirection == SortDirection.Asc;
                    SortDelegationByStatusAndDesc =
                        CurrentDelegationSortField == DelegationSortField.ByStatus &&
                        CurrentDelegationSortDirection == SortDirection.Desc;

                    SortByRoi = CurrentDelegationSortField == DelegationSortField.ByRoi;
                    SortDelegationByStatus = CurrentDelegationSortField == DelegationSortField.ByStatus;
                });

            _ = LoadDelegationInfoAsync();
            DelegateViewModel = new DelegateViewModel(_app, async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(DelegationCheckIntervalInSec))
                    .ConfigureAwait(false);
                await Dispatcher.UIThread.InvokeAsync(OnUpdateClick);
            });

            CurrentDelegationSortField = DelegationSortField.ByStatus;
            CurrentDelegationSortDirection = SortDirection.Desc;
        }

        private void SortDelegations(IEnumerable<Delegation> delegations)
        {
            Delegations = CurrentDelegationSortField switch
            {
                DelegationSortField.ByRoi when CurrentDelegationSortDirection == SortDirection.Desc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderByDescending(d => d.Baker.EstimatedRoi)),
                DelegationSortField.ByRoi when CurrentDelegationSortDirection == SortDirection.Asc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderBy(d => d.Baker.EstimatedRoi)),

                DelegationSortField.ByStatus when CurrentDelegationSortDirection == SortDirection.Desc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderByDescending(d => d.Status)),
                DelegationSortField.ByStatus when CurrentDelegationSortDirection == SortDirection.Asc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderBy(d => d.Status)),
                _ => new ObservableCollection<Delegation>(delegations)
            };
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
                var balance = await _app.Account
                    .GetBalanceAsync(Tezos.Name)
                    .ConfigureAwait(false);

                var addresses = await _app.Account
                    .GetUnspentAddressesAsync(Tezos.Name)
                    .ConfigureAwait(false);

                var rpc = new Rpc(Tezos.RpcNodeUri);

                var delegations = new List<Delegation>();

                var tzktApi = new TzktApi(Tezos);
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
                        ExplorerUri = Tezos.BbUri,
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
                        HasDelegations = delegations.Count > 0;
                        SortDelegations(delegations);
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

        private ReactiveCommand<string, Unit> _openAddressInExplorerCommand;

        public ReactiveCommand<string, Unit> OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??=
            (_openAddressInExplorerCommand = ReactiveCommand.Create<string>(
                address => App.OpenBrowser($"{Tezos?.BbUri}{address}")));


        private ReactiveCommand<DelegationSortField, Unit> _setDelegationSortTypeCommand;

        public ReactiveCommand<DelegationSortField, Unit> SetDelegationSortTypeCommand =>
            _setDelegationSortTypeCommand ??= ReactiveCommand.Create<DelegationSortField>(sortField =>
            {
                if (CurrentDelegationSortField != sortField)
                    CurrentDelegationSortField = sortField;
                else
                    CurrentDelegationSortDirection = CurrentDelegationSortDirection == SortDirection.Asc
                        ? SortDirection.Desc
                        : SortDirection.Asc;
            });

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