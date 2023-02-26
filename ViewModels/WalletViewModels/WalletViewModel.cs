using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Network = NBitcoin.Network;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Bitcoin;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class WalletViewModel : ViewModelBase, IWalletViewModel
    {
        protected const int TRANSACTIONS_LOAD_LIMIT = 20;

        protected readonly IAtomexApp _app;
        protected int _transactionsLoaded;
        protected bool _isTransactionsLoading;

        [Reactive] public ObservableRangeCollection<TransactionViewModelBase> Transactions { get; set; }
        [Reactive] public TransactionViewModelBase? SelectedTransaction { get; set; }
        private TransactionViewModelBase? PreviousSelectedTransaction { get; set; }
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [ObservableAsProperty] public bool IsBalanceUpdating { get; }
        [Reactive] public SortDirection? CurrentSortDirection { get; set; }
        [Reactive] public int SelectedTabIndex { get; set; }
        public AddressesViewModel AddressesViewModel { get; set; }
        protected Action<CurrencyConfig>? SetConversionTab { get; }
        private Action<string>? SetWertCurrency { get; }
        protected Action<ViewModelBase?> ShowRightPopupContent { get; }
        public string Header { get; set; }
        public CurrencyConfig Currency => CurrencyViewModel.Currency;

        protected CancellationTokenSource _cancellation;

        public WalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public WalletViewModel(
            IAtomexApp app,
            Action<ViewModelBase?> showRightPopupContent,
            CurrencyConfig? currency = null,
            Action<CurrencyConfig>? setConversionTab = null,
            Action<string>? setWertCurrency = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            //_app.LocalStorage.RemoveTransactionsMetadataAsync("BTC").WaitForResult();

            ShowRightPopupContent = showRightPopupContent
                ?? throw new ArgumentNullException(nameof(showRightPopupContent));
            
            SetConversionTab = setConversionTab;
            SetWertCurrency = setWertCurrency;

            if (currency != null)
            {
                CurrencyViewModel = CurrencyViewModelCreator.CreateOrGet(currency);
                Header = CurrencyViewModel.Header;
                
                //LoadAddresses();
                //_ = LoadTransactionsAsync();
            }

            this.WhenAnyValue(vm => vm.CurrentSortDirection)
                .WhereNotNull()
                .Where(_ => Transactions != null)
                .Subscribe(sortDirection =>
                {
                    // update transaction for new sort direction
                    _ = LoadTransactionsAsync(reset: true);
                });

            this.WhenAnyValue(vm => vm.SelectedTransaction)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(selectedTransaction =>
                {
                    if (_isTransactionsLoading && selectedTransaction == null)
                    {
                        _isTransactionsLoading = false;
                        return;
                    }

                    if (PreviousSelectedTransaction != null && selectedTransaction != null &&
                        PreviousSelectedTransaction.Id != selectedTransaction.Id)
                    {
                        Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            ShowRightPopupContent?.Invoke(null);

                            await Task.Delay(WalletMainViewModel.DelayBeforeSwitchingSwapDetailsMs);

                            ShowRightPopupContent?.Invoke(selectedTransaction);
                        });
                    }
                    else
                    {
                        ShowRightPopupContent?.Invoke(selectedTransaction);
                    }

                    PreviousSelectedTransaction = selectedTransaction;
                });

            UpdateCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsBalanceUpdating);

            CurrentSortDirection = SortDirection.Desc;
            Transactions = new ObservableRangeCollection<TransactionViewModelBase>();

            SubscribeToServices();
        }

        protected virtual void SubscribeToServices()
        {
            _app.LocalStorage.BalanceChanged += OnBalanceChangedEventHandler;
            _app.LocalStorage.TransactionsChanged += OnTransactionsChangedEventHandler;
        }

        protected virtual void OnBalanceChangedEventHandler(object? sender, BalanceChangedEventArgs args)
        {
            //try
            //{
            //    if (!args.Currencies.Contains(Currency.Name))
            //        return;

            //    // update transactions list
            //    await LoadTransactionsAsync();
            //}
            //catch (Exception e)
            //{
            //    Log.Error(e, "Account balance updated event handler error");
            //}
        }

        private async void OnTransactionsChangedEventHandler(object? sender, TransactionsChangedEventArgs args)
        {
            try
            {
                if (Currency.Name != args.Currency)
                    return;

                // update transactions list
                //await LoadTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Account transactions changed event handler error");
            }
        }

        protected virtual void LoadAddresses()
        {
            AddressesViewModel = new AddressesViewModel(_app, CurrencyViewModel.Currency);
        }

        protected readonly SemaphoreSlim LoadTransactionsSemaphore = new(1, 1);

        protected virtual async Task LoadTransactionsAsync(bool reset)
        {
            await LoadTransactionsSemaphore.WaitAsync();

            Log.Debug("LoadTransactionsAsync for {@currency}", Currency.Name);

            try
            {
                if (_app.Account == null)
                    return;

                _isTransactionsLoading = true;

                if (reset)
                {
                    // reset exists transactions
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Transactions.Clear();
                        _transactionsLoaded = 0;
                    });
                }

                var newTransactions = await Task.Run(async () =>
                {
                    return (await _app.Account
                        .GetTransactionsWithMetadataAsync(
                            currency: Currency.Name,
                            offset: _transactionsLoaded,
                            limit: TRANSACTIONS_LOAD_LIMIT,
                            sort: CurrentSortDirection != null ? CurrentSortDirection.Value : SortDirection.Desc)
                        .ConfigureAwait(false))
                        .ToList();
                });

                _transactionsLoaded += newTransactions.Count;

                var vmsToUpdate = new List<TransactionViewModel>();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var selectedTransactionId = SelectedTransaction?.Id;

                    var transactionViewModels = newTransactions
                        .Select(t =>
                        {
                            var vms = TransactionViewModelCreator
                                .CreateViewModels(
                                    tx: t.Tx,
                                    metadata: t.Metadata,
                                    config: Currency);

                            for (var i = vms.Count - 1; i >= 0; i--)
                            {
                                var vm = vms[i];

                                if (vm.TransactionMetadata != null)
                                {
                                    if (!vm.Type.HasFlag(TransactionType.Input) &&
                                        !vm.Type.HasFlag(TransactionType.Output))
                                    {
                                        // remove transactions view models for non-users addresses
                                        vms.RemoveAt(i);
                                    }
                                }
                                else
                                {
                                    vmsToUpdate.Insert(0, vm); // insert with safe order
                                }

                                vm.UpdateClicked += UpdateTransactionEventHandler;
                                vm.RemoveClicked += RemoveTransactionEventHandler;
                                vm.OnClose = () => ShowRightPopupContent?.Invoke(null);
                            }

                            return vms;
                        })
                        .Aggregate(new List<TransactionViewModelBase>(), (l, vms) => l.AddRangeEx(vms));

                    Transactions.AddRange(transactionViewModels);

                    if (selectedTransactionId != null)
                        SelectedTransaction = Transactions.FirstOrDefault(t => t.Id == selectedTransactionId);
                });

                // resolve view models
                if (vmsToUpdate.Any())
                {
                    await ResolveTransactionsMetadataAsync(vmsToUpdate)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransactionsAsync canceled for {@currency}", Currency?.Name);
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Currency?.Name);
            }

            finally
            {
                LoadTransactionsSemaphore.Release();
            }
        }

        private ReactiveCommand<Unit, Unit>? _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(OnSendClick);

        private ReactiveCommand<Unit, Unit>? _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(OnReceiveClick);

        private ReactiveCommand<Unit, Unit>? _exchangeCommand;
        public ReactiveCommand<Unit, Unit> ExchangeCommand =>
            _exchangeCommand ??= ReactiveCommand.Create(OnConvertClick);

        private ReactiveCommand<Unit, Unit>? _updateCommand;
        public ReactiveCommand<Unit, Unit> UpdateCommand =>
            _updateCommand ??= ReactiveCommand.CreateFromTask(OnUpdateClick);

        private ReactiveCommand<Unit, Unit>? _cancelUpdateCommand;
        public ReactiveCommand<Unit, Unit> CancelUpdateCommand => _cancelUpdateCommand ??= ReactiveCommand.Create(
            () => _cancellation?.Cancel());

        private ReactiveCommand<Unit, Unit>? _buyCommand;
        public ReactiveCommand<Unit, Unit> BuyCommand => _buyCommand ??= ReactiveCommand.Create(
            () => SetWertCurrency?.Invoke(CurrencyViewModel.Header));

        private ReactiveCommand<TxSortField, Unit>? _setSortTypeCommand;
        public ReactiveCommand<TxSortField, Unit> SetSortTypeCommand =>
            _setSortTypeCommand ??= ReactiveCommand.Create<TxSortField>(sortField =>
            {
                CurrentSortDirection = CurrentSortDirection == SortDirection.Asc
                    ? SortDirection.Desc
                    : SortDirection.Asc;
            });

        private ReactiveCommand<Unit, Unit>? _reachEndOfScroll;
        public ReactiveCommand<Unit, Unit> ReachEndOfScroll =>
            _reachEndOfScroll ??= ReactiveCommand.Create(OnReachEndOfScroll);

        protected virtual void OnSendClick()
        {
            var sendViewModel = SendViewModelCreator.CreateViewModel(_app, Currency);

            App.DialogService.Show(sendViewModel.SelectFromViewModel);
        }

        protected virtual void OnReceiveClick()
        {
            var receiveViewModel = new ReceiveViewModel(_app, Currency);

            App.DialogService.Show(receiveViewModel);
        }

        protected virtual void OnConvertClick()
        {
            SetConversionTab?.Invoke(Currency);
        }

        protected virtual async Task OnUpdateClick()
        {
            _cancellation = new CancellationTokenSource();

            try
            {
                await Task.Run(async () =>
                {
                    var scanner = new WalletScanner(_app.Account);

                    await scanner.UpdateBalanceAsync(
                        currency: Currency.Name,
                        skipUsed: true,
                        cancellationToken: _cancellation.Token);

                }, _cancellation.Token);

                //await LoadTransactionsAsync();
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "WalletViewModel.OnUpdateClick");
                // todo: message to user!?
            }
        }

        protected virtual void OnReachEndOfScroll()
        {
            _ = LoadTransactionsAsync(reset: false);
        }

        private void UpdateTransactionEventHandler(object? sender, TransactionEventArgs args)
        {
            // todo:
        }

        private async void RemoveTransactionEventHandler(object? sender, TransactionEventArgs args)
        {
            if (_app.Account == null)
                return;

            try
            {
                var txId = $"{args.Transaction.Id}:{args.Transaction.Currency}";

                var isRemoved = await _app
                    .LocalStorage
                    .RemoveTransactionByIdAsync(txId);

                if (!isRemoved)
                    return;

                ShowRightPopupContent?.Invoke(null);

                var vmsToRemove = Transactions.Where(t => t.Id == args.Transaction.Id);

                if (vmsToRemove != null)
                    Transactions.RemoveRange(vmsToRemove);
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction remove error");
            }
        }

        protected async Task ResolveTransactionsMetadataAsync(
            IEnumerable<TransactionViewModelBase> viewModels)
        {
            var resolved = new Dictionary<string, ITransactionMetadata>();

            await Task.Run(async () =>
            {
                foreach (var vm in viewModels)
                {
                    if (!resolved.TryGetValue(vm.Transaction.Id, out var metadata))
                    {
                        metadata = await _app.Account
                            .ResolveTransactionMetadataAsync(vm.Transaction)
                            .ConfigureAwait(false);

                        resolved.Add(vm.Transaction.Id, metadata);
                    }
                }

                await _app.LocalStorage
                    .UpsertTransactionsMetadataAsync(
                        resolved.Values,
                        notifyIfNewOrChanged: false)
                    .ConfigureAwait(false);
            });

            // update view models in UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var vm in viewModels)
                {
                    if (resolved.TryGetValue(vm.Transaction.Id, out var metadata))
                    {
                        vm.UpdateMetadata(metadata, Currency);

                        if (!vm.Type.HasFlag(TransactionType.Input) &&
                            !vm.Type.HasFlag(TransactionType.Output))
                        {
                            // remove transactions view models for non-users addresses
                            Transactions.Remove(vm);
                        }
                    }
                }
            });
        }

        protected virtual void DesignerMode()
        {
            var currencies = DesignTime.TestNetCurrencies.ToList();

            CurrencyViewModel = CurrencyViewModelCreator.CreateOrGet(currencies[3], subscribeToUpdates: false);
            CurrencyViewModel.TotalAmount = 0.01012345m;
            CurrencyViewModel.TotalAmountInBase = 16.51m;
            CurrencyViewModel.AvailableAmount = 0.01010005m;
            CurrencyViewModel.AvailableAmountInBase = 16.00m;
            CurrencyViewModel.UnconfirmedAmount = 0.00002m;
            CurrencyViewModel.UnconfirmedAmountInBase = 0.5m;

            var transactions = new List<TransactionViewModelBase>
            {
                new BitcoinBasedTransactionViewModel(
                    new BitcoinTransaction("BTC", Transaction.Create(Network.TestNet)),
                    null, //new TransactionMetadata(),
                    DesignTime.TestNetCurrencies.Get<BitcoinConfig>("BTC"))
                {
                    Description = "Sent 0.00124 BTC",
                    Amount = -0.00124m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode
                },
                new BitcoinBasedTransactionViewModel(
                    new BitcoinTransaction("BTC", Transaction.Create(Network.TestNet)),
                    new TransactionMetadata(),
                    DesignTime.TestNetCurrencies.Get<BitcoinConfig>("BTC"))
                {
                    Description = "Received 1.00666 BTC",
                    Amount = 1.00666m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode
                }
            };

            Transactions = new ObservableRangeCollection<TransactionViewModelBase>(
                transactions.SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));
        }
    }
}