﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading;
using Serilog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.DappsViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosWalletViewModel : WalletViewModel
    {
        [Reactive] public ObservableCollection<Delegation> Delegations { get; set; }
        [Reactive] public SortDirection? CurrentDelegationSortDirection { get; set; }
        [Reactive] public DelegationSortField? CurrentDelegationSortField { get; set; }
        [Reactive] public string? DelegationAddressPopupOpened { get; set; }
        [Reactive] public DappsViewModel DappsViewModel { get; set; }
        [ObservableAsProperty] public bool IsTokensUpdating { get; }

        private bool CanDelegate { get; set; }
        private bool HasDelegations { get; set; }
        private DelegateViewModel DelegateViewModel { get; }
        private TezosTokensViewModel TezosTokensViewModel { get; set; }
        private CollectiblesViewModel CollectiblesViewModel { get; set; }
        private TezosConfig? Tezos { get; }

        public TezosWalletViewModel()
            : base()
        {
        }

        public TezosWalletViewModel(
            IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            Action<string>? setWertCurrency,
            Action<ViewModelBase?> showRightPopupContent,
            Action<TezosTokenViewModel> showTezosToken,
            Action<IEnumerable<TezosTokenViewModel>> showTezosCollection,
            CurrencyConfig currency)
            : base(app, currency, showRightPopupContent, setConversionTab, setWertCurrency)
        {
            Tezos = currency as TezosConfig;
            Delegations = new ObservableCollection<Delegation>();

            this.WhenAnyValue(
                    vm => vm.CurrentDelegationSortDirection,
                    vm => vm.CurrentDelegationSortField)
                .WhereAllNotNull()
                .SubscribeInMainThread(_ => SortDelegations(Delegations));

            DelegateCommand
                .Merge(UndelegateCommand)
                .SubscribeInMainThread(_ => DelegationAddressPopupOpened = null);

            UpdateTokensCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsTokensUpdating);

            _ = LoadDelegationInfoAsync();

            DelegateViewModel = new DelegateViewModel(_app);
            DappsViewModel = new DappsViewModel(_app);
            TezosTokensViewModel = new TezosTokensViewModel(_app, showTezosToken, setConversionTab);
            CollectiblesViewModel = new CollectiblesViewModel(_app, showTezosCollection);

            CurrentDelegationSortField = DelegationSortField.ByBalance;
            CurrentDelegationSortDirection = SortDirection.Desc;
        }

        private void SortDelegations(IEnumerable<Delegation> delegations)
        {
            Delegations = CurrentDelegationSortField switch
            {
                DelegationSortField.ByRoi when CurrentDelegationSortDirection == SortDirection.Desc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderByDescending(d => d.Baker?.EstimatedRoi ?? 0)),
                DelegationSortField.ByRoi when CurrentDelegationSortDirection == SortDirection.Asc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderBy(d => d.Baker?.EstimatedRoi ?? 0)),

                DelegationSortField.ByStatus when CurrentDelegationSortDirection == SortDirection.Desc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderByDescending(d => d.Status)),
                DelegationSortField.ByStatus when CurrentDelegationSortDirection == SortDirection.Asc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderBy(d => d.Status)),

                DelegationSortField.ByBalance when CurrentDelegationSortDirection == SortDirection.Desc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderByDescending(d => d.Balance)),
                DelegationSortField.ByBalance when CurrentDelegationSortDirection == SortDirection.Asc
                    => new ObservableCollection<Delegation>(
                        delegations.OrderBy(d => d.Balance)),
                _ => new ObservableCollection<Delegation>(delegations)
            };
        }

        protected override void SubscribeToServices()
        {
            base.SubscribeToServices();

            _app.LocalStorage.BalanceChanged += OnBalanceChangedEventHandler;
        }

        protected async void OnBalanceChangedEventHandler(object? sender, BalanceChangedEventArgs args)
        {
            try
            {
                if (args.Currency != Currency.Name)
                    return;

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

                var delegations = new List<Delegation>();

                var tzktApi = Tezos.GetTzktApi();

                var rpc = new TezosRpc(Tezos.GetRpcSettings());
                var head = await rpc
                    .GetHeaderAsync()
                    .ConfigureAwait(false); //await tzktApi.GetHeaderAsync();

                var currentCycle = _app.Account.Network == Network.MainNet
                    ? Math.Floor((head.Level - 1) / 4096m)
                    : Math.Floor((head.Level - 1) / 2048m);

                foreach (var wa in addresses)
                {
                    var accountData = await rpc
                        .GetAccountAsync(wa.Address)
                        .ConfigureAwait(false);

                    var @delegate = accountData.Delegate;

                    if (string.IsNullOrEmpty(@delegate))
                    {
                        delegations.Add(new Delegation
                        {
                            Baker          = null,
                            Address        = wa.Address,
                            ExplorerUri    = Tezos.BbUri,
                            Balance        = wa.Balance.ToTez(),
                            DelegationTime = null,
                            Status         = DelegationStatus.NotDelegated
                        });

                        continue;
                    }

                    var baker = await BbApi
                        .GetBaker(@delegate, _app.Account.Network)
                        .ConfigureAwait(false) ?? new BakerData { Address = @delegate };

                    var (account, error) = await tzktApi.GetAccountAsync(wa.Address);

                    if (error != null || account == null)
                        continue;

                    var txCycle = _app.Account.Network == Network.MainNet
                        ? Math.Floor((account.DelegationLevel - 1) / 4096m)
                        : Math.Floor((account.DelegationLevel - 1) / 2048m);

                    delegations.Add(new Delegation
                    {
                        Baker          = baker,
                        Address        = wa.Address,
                        ExplorerUri    = Tezos.BbUri,
                        Balance        = wa.Balance.ToTez(),
                        DelegationTime = DateTime.Parse(account.DelegationTime),
                        Status         = currentCycle - txCycle < 2 ? DelegationStatus.Pending :
                            currentCycle - txCycle < 7 ? DelegationStatus.Confirmed :
                            DelegationStatus.Active
                    });
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CanDelegate = balance.Confirmed > 0;
                    HasDelegations = delegations.Count > 0;

                    SortDelegations(delegations);
                },
                DispatcherPriority.Background);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadDelegationInfoAsync canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadDelegationInfoAsync error");
            }
        }

        private ReactiveCommand<string, Unit>? _delegateCommand;
        public ReactiveCommand<string, Unit> DelegateCommand =>
            _delegateCommand ??= _delegateCommand = ReactiveCommand.Create<string>(OnDelegateClick);

        private ReactiveCommand<string, Unit>? _undelegateCommand;
        public ReactiveCommand<string, Unit> UndelegateCommand =>
            _undelegateCommand ??= _undelegateCommand = ReactiveCommand.Create<string>(
                undelegateAddress => _ = DelegateViewModel.UndelegateAsync(undelegateAddress));

        private ReactiveCommand<string, Unit>? _openAddressInExplorerCommand;
        public ReactiveCommand<string, Unit> OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??=
            _openAddressInExplorerCommand = ReactiveCommand.Create<string>(
                address => App.OpenBrowser($"{Tezos?.BbUri}{address}"));

        private ReactiveCommand<DelegationSortField, Unit>? _setDelegationSortTypeCommand;
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

        private ReactiveCommand<Unit, Unit>? _connectDappCommand;
        public ReactiveCommand<Unit, Unit> ConnectDappCommand =>
            _connectDappCommand ??= ReactiveCommand.Create(() =>
            {
                App.DialogService.Show(DappsViewModel.ConnectDappViewModel);
            });

        private ReactiveCommand<string, Unit>? _openDelegationPopupCommand;
        public ReactiveCommand<string, Unit> OpenDelegationPopupCommand => _openDelegationPopupCommand ??=
            (_openDelegationPopupCommand = ReactiveCommand.Create<string>(
                address => DelegationAddressPopupOpened = address));

        private ReactiveCommand<Unit, Unit>? _manageTokensCommand;
        public ReactiveCommand<Unit, Unit> ManageTokensCommand =>
            _manageTokensCommand ??= _manageTokensCommand = ReactiveCommand.Create(() =>
            {
                var manageAssetsViewModel = new ManageAssetsViewModel
                {
                    AvailableAssets = new ObservableCollection<AssetWithSelection>(
                        TezosTokensViewModel
                            .InitialTokens
                            .Select(token => new AssetWithSelection
                            {
                                Asset = token,
                                IsSelected =
                                    !_app.Account.UserData.DisabledTokens?.Contains(token.TokenBalance.Symbol) ?? true
                            })
                    ),
                    OnAssetsChanged = selectedSymbols =>
                    {
                        var disabledTokens = TezosTokensViewModel
                            .InitialTokens
                            .Select(token => token.TokenBalance.Symbol)
                            .Where(symbol => !selectedSymbols.Contains(symbol))
                            .ToArray();

                        TezosTokensViewModel.DisabledTokens = disabledTokens;
                    },
                    OnHideZeroBalancesChanges =
                        hideLowBalances => TezosTokensViewModel.HideLowBalances = hideLowBalances,

                    HideZeroBalances = _app.Account.UserData.HideTokensWithLowBalance ?? false
                };

                App.DialogService.Show(manageAssetsViewModel);
            });

        private ReactiveCommand<Unit, Unit>? _manageCollectiblesCommand;
        public ReactiveCommand<Unit, Unit> ManageCollectiblesCommand =>
            _manageCollectiblesCommand ??= _manageCollectiblesCommand = ReactiveCommand.Create(() =>
            {
                var manageCollectiblesViewModel = new ManageAssetsViewModel
                {
                    AvailableAssets = new ObservableCollection<AssetWithSelection>(
                        CollectiblesViewModel
                            .InitialCollectibles
                            .OrderByDescending(collectible => collectible.TotalAmount != 0)
                            .ThenBy(collectible => collectible.Name)
                            .Select(collectible => new AssetWithSelection
                            {
                                Asset = collectible,
                                IsSelected = !_app.Account.UserData.DisabledCollectibles?
                                    .Contains(collectible.ContractAddress) ?? true
                            })
                    ),
                    OnAssetsChanged = selectedCollectibles =>
                    {
                        var disabledCollectibles = CollectiblesViewModel
                            .InitialCollectibles
                            .Select(collectible => collectible.ContractAddress)
                            .Where(contractAddress => !selectedCollectibles.Contains(contractAddress))
                            .ToArray();

                        CollectiblesViewModel.DisabledCollectibles = disabledCollectibles;
                    }
                };

                App.DialogService.Show(manageCollectiblesViewModel);
            });


        private ReactiveCommand<Unit, Unit>? _updateTokensCommand;
        public ReactiveCommand<Unit, Unit> UpdateTokensCommand => _updateTokensCommand ??=
            (_updateTokensCommand = ReactiveCommand.CreateFromTask(UpdateTokens));

        private async Task UpdateTokens()
        {
            _cancellation = new CancellationTokenSource();

            try
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var fa12TokensScanner = new TezosTokensWalletScanner(tezosAccount, TezosHelper.Fa12);
                var fa2TokensScanner = new TezosTokensWalletScanner(tezosAccount, TezosHelper.Fa2);

                await fa12TokensScanner
                    .UpdateBalanceAsync(cancellationToken: _cancellation.Token);

                await fa2TokensScanner
                    .UpdateBalanceAsync(cancellationToken: _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Tezos tokens update canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens update error");
            }
        }

        private void OnDelegateClick(string addressToDelegate)
        {
            var delegation = Delegations.First(delegation => delegation.Address == addressToDelegate);
            DelegateViewModel.InitializeWith(delegation);
            App.DialogService.Show(DelegateViewModel);
        }

#if DEBUG
        protected override void DesignerMode()
        {
            base.DesignerMode();

            SelectedTabIndex = 3;

            TezosTokensViewModel = new TezosTokensViewModel();

            Delegations = new ObservableCollection<Delegation>()
            {
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png",
                        Name = "Tez Phoenix Reborn 2.0",
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png",
                        Name = "Tez Phoenix Reborn 2.0"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png",
                        Name = "Tez Phoenix Reborn 2.0"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png",
                        Name = "Tez Phoenix Reborn 2.0"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new()
                {
                    Baker = new BakerData
                    {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png",
                        Name = "Tez Phoenix Reborn 2.0"
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