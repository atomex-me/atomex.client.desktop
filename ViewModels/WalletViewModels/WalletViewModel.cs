using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using NBitcoin;
using ReactiveUI;
using Serilog;
using Atomex.Blockchain;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet;
using Avalonia.Controls;
using ReactiveUI.Fody.Helpers;
using Network = NBitcoin.Network;


namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public enum TxSortField
    {
        ByAmount,
        ByStatus,
        ByTime
    }

    public enum SortDirection
    {
        Asc,
        Desc
    }

    public class WalletViewModel : ViewModelBase, IWalletViewModel
    {
        protected IAtomexApp _app;
        [Reactive] public ObservableCollection<TransactionViewModelBase> Transactions { get; set; }
        [Reactive] public TransactionViewModelBase? SelectedTransaction { get; set; }
        private TransactionViewModelBase? PreviousSelectedTransaction { get; set; }
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public bool IsBalanceUpdating { get; set; }
        [Reactive] public TxSortField? CurrentSortField { get; set; }
        [Reactive] public SortDirection? CurrentSortDirection { get; set; }
        protected bool IsTransactionsLoading { get; set; }
        public AddressesViewModel AddressesViewModel { get; set; }
        protected Action<CurrencyConfig> SetConversionTab { get; }
        private Action<string> SetWertCurrency { get; }
        private Action<ViewModelBase?> ShowRightPopupContent { get; }

        public string Header => CurrencyViewModel.Header;
        public CurrencyConfig Currency => CurrencyViewModel.Currency;

        [Reactive] public bool SortByTimeAndAsc { get; set; }
        [Reactive] public bool SortByTimeAndDesc { get; set; }
        [Reactive] public bool SortByStatusAndAsc { get; set; }
        [Reactive] public bool SortByStatusAndDesc { get; set; }
        [Reactive] public bool SortByAmountAndAsc { get; set; }
        [Reactive] public bool SortByAmountAndDesc { get; set; }

        [Reactive] public bool SortByTime { get; set; }
        [Reactive] public bool SortByStatus { get; set; }
        [Reactive] public bool SortByAmount { get; set; }

        protected CancellationTokenSource _cancellation { get; set; }

        public WalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public WalletViewModel(
            IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            Action<string> setWertCurrency,
            Action<ViewModelBase?> showRightPopupContent,
            CurrencyConfig currency)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            SetConversionTab = setConversionTab ?? throw new ArgumentNullException(nameof(setConversionTab));
            SetWertCurrency = setWertCurrency ?? throw new ArgumentNullException(nameof(setWertCurrency));
            ShowRightPopupContent =
                showRightPopupContent ?? throw new ArgumentNullException(nameof(showRightPopupContent));

            if (currency != null)
                CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);

            this.WhenAnyValue(vm => vm.CurrentSortField, vm => vm.CurrentSortDirection)
                .WhereAllNotNull()
                .Where(_ => Transactions != null)
                .SubscribeInMainThread(_ => { Transactions = SortTransactions(Transactions); });

            this.WhenAnyValue(vm => vm.SelectedTransaction)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(selectedTransaction =>
                {
                    if (IsTransactionsLoading && selectedTransaction == null)
                    {
                        IsTransactionsLoading = false;
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

            CurrentSortField = TxSortField.ByTime;
            CurrentSortDirection = SortDirection.Desc;

            SubscribeToServices();
            LoadAddresses();
            _ = LoadTransactionsAsync();
        }

        protected virtual void SubscribeToServices()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
            _app.Account.UnconfirmedTransactionAdded += OnUnconfirmedTransactionAdded;
        }

        protected virtual async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name != args.Currency) return;

                // update transactions list
                await LoadTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async void OnUnconfirmedTransactionAdded(object sender, TransactionEventArgs args)
        {
            try
            {
                if (Currency.Name != args.Transaction.Currency) return;

                // update transactions list
                await LoadTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Account unconfirmed transaction added event handler error");
            }
        }

        protected virtual void LoadAddresses()
        {
            AddressesViewModel = new AddressesViewModel(_app, CurrencyViewModel.Currency);
        }

        protected ObservableCollection<TransactionViewModelBase> SortTransactions(
            IEnumerable<TransactionViewModelBase> transactions)
        {
            SortByTimeAndAsc =
                CurrentSortField == TxSortField.ByTime && CurrentSortDirection == SortDirection.Asc;
            SortByTimeAndDesc =
                CurrentSortField == TxSortField.ByTime && CurrentSortDirection == SortDirection.Desc;
            SortByStatusAndAsc =
                CurrentSortField == TxSortField.ByStatus && CurrentSortDirection == SortDirection.Asc;
            SortByStatusAndDesc =
                CurrentSortField == TxSortField.ByStatus && CurrentSortDirection == SortDirection.Desc;
            SortByAmountAndAsc =
                CurrentSortField == TxSortField.ByAmount && CurrentSortDirection == SortDirection.Asc;
            SortByAmountAndDesc =
                CurrentSortField == TxSortField.ByAmount && CurrentSortDirection == SortDirection.Desc;

            SortByTime = CurrentSortField == TxSortField.ByTime;
            SortByStatus = CurrentSortField == TxSortField.ByStatus;
            SortByAmount = CurrentSortField == TxSortField.ByAmount;

            return CurrentSortField switch
            {
                TxSortField.ByTime when CurrentSortDirection == SortDirection.Desc
                    => new ObservableCollection<TransactionViewModelBase>(
                        transactions.OrderByDescending(tx => tx.LocalTime)),
                TxSortField.ByTime when CurrentSortDirection == SortDirection.Asc
                    => new ObservableCollection<TransactionViewModelBase>(
                        transactions.OrderBy(tx => tx.LocalTime)),

                TxSortField.ByAmount when CurrentSortDirection == SortDirection.Desc
                    => new ObservableCollection<TransactionViewModelBase>(
                        transactions.OrderByDescending(tx => tx.Amount)),
                TxSortField.ByAmount when CurrentSortDirection == SortDirection.Asc
                    => new ObservableCollection<TransactionViewModelBase>(
                        transactions.OrderBy(tx => tx.Amount)),

                TxSortField.ByStatus when CurrentSortDirection == SortDirection.Desc
                    => new ObservableCollection<TransactionViewModelBase>(
                        transactions.OrderByDescending(tx => tx.State)),
                TxSortField.ByStatus when CurrentSortDirection == SortDirection.Asc
                    => new ObservableCollection<TransactionViewModelBase>(
                        transactions.OrderBy(tx => tx.State)),
                _ => Transactions
            };
        }

        
        protected readonly SemaphoreSlim LoadTransactionsSemaphore = new(1, 1);
        protected virtual async Task LoadTransactionsAsync()
        {
            await LoadTransactionsSemaphore.WaitAsync();
            Log.Debug("LoadTransactionsAsync for {@currency}", Currency.Name);

            try
            {
                if (_app.Account == null)
                    return;

                IsTransactionsLoading = true;

                var transactions = (await _app.Account
                        .GetTransactionsAsync(Currency.Name))
                    .ToList();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var selectedTransactionId = SelectedTransaction?.Id;

                    Transactions = SortTransactions(
                        transactions
                            .Select(t => TransactionViewModelCreator.CreateViewModel(t, Currency))
                            .ToList()
                            .ForEachDo(t =>
                            {
                                t.UpdateClicked += UpdateTransactionEventHandler;
                                t.RemoveClicked += RemoveTransactionEventHandler;
                            }));

                    if (selectedTransactionId != null)
                        SelectedTransaction = Transactions.FirstOrDefault(t => t.Id == selectedTransactionId);
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransactionsAsync canceled.");
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

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(OnSendClick);

        private ReactiveCommand<Unit, Unit> _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(OnReceiveClick);

        private ReactiveCommand<Unit, Unit> _exchangeCommand;
        public ReactiveCommand<Unit, Unit> ExchangeCommand => _exchangeCommand ??= ReactiveCommand.Create(OnConvertClick);

        private ReactiveCommand<Unit, Unit> _updateCommand;
        public ReactiveCommand<Unit, Unit> UpdateCommand => _updateCommand ??= ReactiveCommand.Create(OnUpdateClick);

        private ReactiveCommand<Unit, Unit> _cancelUpdateCommand;

        public ReactiveCommand<Unit, Unit> CancelUpdateCommand => _cancelUpdateCommand ??= ReactiveCommand.Create(
            () => _cancellation?.Cancel());

        private ReactiveCommand<Unit, Unit> _buyCommand;

        public ReactiveCommand<Unit, Unit> BuyCommand => _buyCommand ??= ReactiveCommand.Create(
            () => SetWertCurrency?.Invoke(CurrencyViewModel.Header));

        private ReactiveCommand<TxSortField, Unit> _setSortTypeCommand;

        public ReactiveCommand<TxSortField, Unit> SetSortTypeCommand =>
            _setSortTypeCommand ??= ReactiveCommand.Create<TxSortField>(sortField =>
            {
                if (CurrentSortField != sortField)
                    CurrentSortField = sortField;
                else
                    CurrentSortDirection = CurrentSortDirection == SortDirection.Asc
                        ? SortDirection.Desc
                        : SortDirection.Asc;
            });

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

        protected virtual async void OnUpdateClick()
        {
            if (IsBalanceUpdating)
                return;

            IsBalanceUpdating = true;

            _cancellation = new CancellationTokenSource();

            try
            {
                var scanner = new HdWalletScanner(_app.Account);

                await scanner.ScanAsync(
                    currency: Currency.Name,
                    skipUsed: true,
                    cancellationToken: _cancellation.Token);

                await LoadTransactionsAsync();
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

            IsBalanceUpdating = false;
        }

        private void UpdateTransactionEventHandler(object sender, TransactionEventArgs args)
        {
            // todo:
        }

        private async void RemoveTransactionEventHandler(object sender, TransactionEventArgs args)
        {
            if (_app.Account == null)
                return;

            try
            {
                var txId = $"{args.Transaction.Id}:{args.Transaction.Currency}";

                var isRemoved = await _app.Account
                    .RemoveTransactionAsync(txId);

                if (isRemoved)
                    await LoadTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction remove error");
            }
        }

        protected virtual void DesignerMode()
        {
            var currencies = DesignTime.TestNetCurrencies.ToList();

            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currencies[3], subscribeToUpdates: false);
            CurrencyViewModel.TotalAmount = 0.01012345m;
            CurrencyViewModel.TotalAmountInBase = 16.51m;
            CurrencyViewModel.AvailableAmount = 0.01010005m;
            CurrencyViewModel.AvailableAmountInBase = 16.00m;
            CurrencyViewModel.UnconfirmedAmount = 0.00002m;
            CurrencyViewModel.UnconfirmedAmountInBase = 0.5m;

            var transactions = new List<TransactionViewModelBase>
            {
                new BitcoinBasedTransactionViewModel(
                    new BitcoinBasedTransaction("BTC", Transaction.Create(Network.TestNet)),
                    DesignTime.TestNetCurrencies.Get<BitcoinConfig>("BTC"))
                {
                    Description = "Sent 0.00124 BTC",
                    Amount = -0.00124m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Time = DateTime.Now,
                },
                new BitcoinBasedTransactionViewModel(
                    new BitcoinBasedTransaction("BTC", Transaction.Create(Network.TestNet)),
                    DesignTime.TestNetCurrencies.Get<BitcoinConfig>("BTC"))
                {
                    Description = "Received 1.00666 BTC",
                    Amount = 1.00666m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Time = DateTime.Now,
                }
            };

            Transactions = new ObservableCollection<TransactionViewModelBase>(
                transactions.SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));
        }
    }
}