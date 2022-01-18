using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Blockchain;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.ReceiveViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet;
using Avalonia.Media;
using Avalonia.Threading;
using NBitcoin;
using ReactiveUI;
using Serilog;
using DialogHost;
using Network = NBitcoin.Network;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class WalletViewModel : ViewModelBase, IWalletViewModel
    {
        private ObservableCollection<TransactionViewModel> _transactions;

        public ObservableCollection<TransactionViewModel> Transactions
        {
            get => _transactions;
            set
            {
                _transactions = value;
                OnPropertyChanged(nameof(Transactions));
            }
        }

        private CurrencyViewModel _currencyViewModel;

        public CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set
            {
                _currencyViewModel = value;
                OnPropertyChanged(nameof(CurrencyViewModel));
            }
        }

        protected IAtomexApp App { get; set; }
        protected Action<CurrencyConfig> SetConversionTab { get; set; }

        public string Header => CurrencyViewModel.Header;
        public CurrencyConfig Currency => CurrencyViewModel.Currency;

        public IBrush Background => IsSelected
            ? CurrencyViewModel.IconBrush
            : CurrencyViewModel.UnselectedIconBrush;

        public IBrush OpacityMask => IsSelected
            ? CurrencyViewModel.IconBrush is ImageBrush ? null : CurrencyViewModel.IconMaskBrush
            : CurrencyViewModel.IconMaskBrush;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(Background));
                OnPropertyChanged(nameof(OpacityMask));
                OnPropertyChanged(nameof(CurrencyOpacityValue));
            }
        }
        
        public double CurrencyOpacityValue => _isSelected ? 1 : 0.4;
        
        private bool _isBalanceUpdating;
        public bool IsBalanceUpdating
        {
            get => _isBalanceUpdating;
            set
            {
                _isBalanceUpdating = value;
                OnPropertyChanged(nameof(IsBalanceUpdating));
            }
        }

        protected CancellationTokenSource _cancellation { get; set; }

        public WalletViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public WalletViewModel(
            IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            CurrencyConfig currency)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            SetConversionTab = setConversionTab ?? throw new ArgumentNullException(nameof(setConversionTab));
            if (currency != null)
                CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);

            SubscribeToServices();

            // update transactions list
            _ = LoadTransactionsAsync();
        }

        protected virtual void SubscribeToServices()
        {
            App.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
            App.Account.UnconfirmedTransactionAdded += OnUnconfirmedTransactionAdded;
        }

        protected virtual async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name == args.Currency)
                {
                    // update transactions list
                    await LoadTransactionsAsync();
                }
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
                if (Currency.Name == args.Transaction.Currency)
                {
                    // update transactions list
                    await LoadTransactionsAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Account unconfirmed transaction added event handler error");
            }
        }

        protected virtual async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for {@currency}", Currency.Name);

            try
            {
                if (App.Account == null)
                    return;

                var transactions = (await App.Account
                        .GetTransactionsAsync(Currency.Name))
                    .ToList();

                await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Transactions = new ObservableCollection<TransactionViewModel>(
                            transactions.Select(t => TransactionViewModelCreator
                                    .CreateViewModel(t, Currency))
                                .ToList()
                                .SortList((t1, t2) => t2.Time.CompareTo(t1.Time))
                                .ForEachDo(t =>
                                {
                                    t.UpdateClicked += UpdateTransactonEventHandler;
                                    t.RemoveClicked += RemoveTransactonEventHandler;
                                }));
                    },
                    DispatcherPriority.Background);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransactionsAsync canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Currency?.Name);
            }
        }

        private ICommand _sendCommand;
        public ICommand SendCommand => _sendCommand ??= ReactiveCommand.Create(OnSendClick);

        private ICommand _receiveCommand;
        public ICommand ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(OnReceiveClick);

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ??= ReactiveCommand.Create(OnConvertClick);

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ??= ReactiveCommand.Create(OnUpdateClick);

        private ICommand _addressesCommand;
        public ICommand AddressesCommand => _addressesCommand ??= ReactiveCommand.Create(OnAddressesClick);

        private ICommand _cancelUpdateCommand;
        public ICommand CancelUpdateCommand => _cancelUpdateCommand ??= ReactiveCommand.Create(() =>
        {
            _cancellation?.Cancel();
        });

        protected virtual void OnSendClick()
        {
            var sendViewModel = SendViewModelCreator.CreateViewModel(App, Currency);
            Desktop.App.DialogService.Show(sendViewModel.SelectFromViewModel);
        }

        protected virtual void OnReceiveClick()
        {
            var receiveViewModel = new ReceiveViewModel(App, Currency);

            Desktop.App.DialogService.Show(receiveViewModel);
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
                var scanner = new HdWalletScanner(App.Account);

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

        protected virtual void OnAddressesClick()
        {
            Desktop.App.DialogService.Show(new AddressesViewModel(App, Currency));
        }

        private void UpdateTransactonEventHandler(object sender, TransactionEventArgs args)
        {
            // todo:
        }

        private async void RemoveTransactonEventHandler(object sender, TransactionEventArgs args)
        {
            if (App.Account == null)
                return;

            try
            {
                var txId = $"{args.Transaction.Id}:{args.Transaction.Currency}";

                var isRemoved = await App.Account
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
            var currencies = DesignTime.Currencies.ToList();

            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currencies[3], subscribeToUpdates: false);
            CurrencyViewModel.TotalAmount             = 0.01012345m;
            CurrencyViewModel.TotalAmountInBase       = 16.51m;
            CurrencyViewModel.AvailableAmount         = 0.01010005m;
            CurrencyViewModel.AvailableAmountInBase   = 16.00m;
            CurrencyViewModel.UnconfirmedAmount       = 0.00002m;
            CurrencyViewModel.UnconfirmedAmountInBase = 0.5m;

            var transactions = new List<TransactionViewModel>
            {
                new BitcoinBasedTransactionViewModel(new BitcoinBasedTransaction("BTC", Transaction.Create(Network.TestNet)), DesignTime.Currencies.Get<BitcoinConfig>("BTC"))
                {
                    Description  = "Sent 0.00124 BTC",
                    Amount       = -0.00124m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Time         = DateTime.Now,
                },
                new BitcoinBasedTransactionViewModel(new BitcoinBasedTransaction("BTC", Transaction.Create(Network.TestNet)), DesignTime.Currencies.Get<BitcoinConfig>("BTC"))
                {
                    Description  = "Received 1.00666 BTC",
                    Amount       = 1.00666m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Time         = DateTime.Now,
                }
            };

            Transactions = new ObservableCollection<TransactionViewModel>(
                transactions.SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));
        }

        private int _dgSelectedIndex = -1;
        public int DGSelectedIndex
        {
            get => _dgSelectedIndex;
            set
            {
                _dgSelectedIndex = value;
                OnPropertyChanged(nameof(DGSelectedIndex));
            }
        }

        public void CellPointerPressed(int cellIndex)
        {
            if (cellIndex == DGSelectedIndex)
            {
                DGSelectedIndex = -1;
                return;
            }

            DGSelectedIndex = cellIndex;
        }

        protected string _sortInfo;
        public string SortInfo
        {
            get => _sortInfo;
            set
            {
                SortType newSortType = SortType.Asc;
                if (!String.IsNullOrEmpty(_sortInfo))
                {
                    var lastSortType = _sortInfo.Split(Convert.ToChar("/"))[1];
                    var lastSortedColumn = _sortInfo.Split(Convert.ToChar("/"))[0];

                    if (String.Equals(lastSortedColumn, value))
                    {
                        if (lastSortType == SortType.Asc.ToString()) newSortType = SortType.Desc;
                        if (lastSortType == SortType.Desc.ToString()) newSortType = SortType.Asc;
                    }
                }

                _sortInfo = $"{value}/{newSortType}";
                OnPropertyChanged(nameof(SortInfo));

                SortTransactions(value, newSortType);
            }
        }

        protected virtual void SortTransactions(string columnName, SortType sortType)
        {
            DGSelectedIndex = -1;
            if (columnName.ToLower() == "time" && sortType == SortType.Asc)
            {
                Transactions = new ObservableCollection<TransactionViewModel>(Transactions.OrderBy(tx => tx.LocalTime));
            }

            if (columnName.ToLower() == "time" && sortType == SortType.Desc)
            {
                Transactions =
                    new ObservableCollection<TransactionViewModel>(Transactions.OrderByDescending(tx => tx.LocalTime));
            }

            if (columnName.ToLower() == "amount" && sortType == SortType.Asc)
            {
                Transactions = new ObservableCollection<TransactionViewModel>(Transactions.OrderBy(tx => tx.Amount));
            }

            if (columnName.ToLower() == "amount" && sortType == SortType.Desc)
            {
                Transactions =
                    new ObservableCollection<TransactionViewModel>(Transactions.OrderByDescending(tx => tx.Amount));
            }

            if (columnName.ToLower() == "state" && sortType == SortType.Asc)
            {
                Transactions = new ObservableCollection<TransactionViewModel>(Transactions.OrderBy(tx => tx.State));
            }

            if (columnName.ToLower() == "state" && sortType == SortType.Desc)
            {
                Transactions =
                    new ObservableCollection<TransactionViewModel>(Transactions.OrderByDescending(tx => tx.State));
            }

            if (columnName.ToLower() == "type" && sortType == SortType.Asc)
            {
                Transactions = new ObservableCollection<TransactionViewModel>(Transactions.OrderBy(tx => tx.Type));
            }

            if (columnName.ToLower() == "type" && sortType == SortType.Desc)
            {
                Transactions =
                    new ObservableCollection<TransactionViewModel>(Transactions.OrderByDescending(tx => tx.Type));
            }
        }
    }
}