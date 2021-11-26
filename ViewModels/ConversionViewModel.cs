using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Windows.Input;

using Avalonia.Threading;
using ReactiveUI;
using Serilog;

using Atomex.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.Services;
using Atomex.Swaps;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ConversionViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;

        private ISymbols Symbols
        {
            get
            {
#if DEBUG
                if (Env.IsInDesignerMode())
                    return DesignTime.Symbols;
#endif
                return _app.SymbolsProvider
                    .GetSymbols(_app.Account.Network);
            }
        }
        private ICurrencies Currencies
        {
            get
            {
#if DEBUG
                if (Env.IsInDesignerMode())
                    return DesignTime.Currencies;
#endif
                return _app.Account.Currencies;
            }
        }

        private IFromSource FromSource { get; set; }
        private ObservableCollection<WalletAddressViewModel> ToAddresses { get; set; }
        private string To { get; set; }
        private string RedeemAddress { get; set; }

        private List<CurrencyViewModel> _fromCurrencies;
        public List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            set => this.RaiseAndSetIfChanged(ref _fromCurrencies, value);
        }

        private ObservableAsPropertyHelper<List<CurrencyViewModel>> _toCurrencies;
        public List<CurrencyViewModel> ToCurrencies => _toCurrencies.Value;

        private CurrencyViewModel _fromCurrencyViewModel;
        public CurrencyViewModel FromCurrencyViewModel
        {
            get => _fromCurrencyViewModel;
            set => this.RaiseAndSetIfChanged(ref _fromCurrencyViewModel, value);
        }

        private CurrencyConfig? FromCurrency => FromCurrencyViewModel?.Currency;

        private CurrencyViewModel _toCurrencyViewModel;
        public CurrencyViewModel ToCurrencyViewModel
        {
            get => _toCurrencyViewModel;
            set => this.RaiseAndSetIfChanged(ref _toCurrencyViewModel, value);
        }

        private CurrencyConfig? ToCurrency => ToCurrencyViewModel?.Currency;

        private readonly ObservableAsPropertyHelper<string> _priceFormat;
        public string PriceFormat => _priceFormat.Value;

        protected decimal _amount;
        public string AmountString
        {
            get => _amount.ToString(FromCurrencyViewModel.CurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var amount))
                {
                    if (amount == 0)
                        _amount = amount;

                    OnPropertyChanged(nameof(AmountString));
                    return;
                }

                _amount = amount.TruncateByFormat(FromCurrencyViewModel.CurrencyFormat);

                if (_amount > long.MaxValue)
                    _amount = long.MaxValue;

                OnPropertyChanged(nameof(AmountString));

                _ = UpdateAmountAsync(_amount, updateUi: false);
            }
        }

        private decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set => this.RaiseAndSetIfChanged(ref _amountInBase, value);
        }

        private bool _isAmountUpdating;
        public bool IsAmountUpdating
        {
            get => _isAmountUpdating;
            set => this.RaiseAndSetIfChanged(ref _isAmountUpdating, value);
        }

        private bool _isAmountValid = true;
        public bool IsAmountValid
        {
            get => _isAmountValid;
            set => this.RaiseAndSetIfChanged(ref _isAmountValid, value);
        }

        private decimal _targetAmount;
        public decimal TargetAmount
        {
            get => _targetAmount;
            set => this.RaiseAndSetIfChanged(ref _targetAmount, value);
        }

        private decimal _targetAmountInBase;
        public decimal TargetAmountInBase
        {
            get => _targetAmountInBase;
            set => this.RaiseAndSetIfChanged(ref _targetAmountInBase, value);
        }

        private decimal _estimatedOrderPrice;

        private decimal _estimatedPrice;
        public decimal EstimatedPrice
        {
            get => _estimatedPrice;
            set => this.RaiseAndSetIfChanged(ref _estimatedPrice, value);
        }

        private decimal _estimatedMaxAmount;
        public decimal EstimatedMaxAmount
        {
            get => _estimatedMaxAmount;
            set => this.RaiseAndSetIfChanged(ref _estimatedMaxAmount, value);
        }

        private decimal _estimatedMakerNetworkFee;
        public decimal EstimatedMakerNetworkFee
        {
            get => _estimatedMakerNetworkFee;
            set => this.RaiseAndSetIfChanged(ref _estimatedMakerNetworkFee, value);
        }

        private decimal _estimatedMakerNetworkFeeInBase;
        public decimal EstimatedMakerNetworkFeeInBase
        {
            get => _estimatedMakerNetworkFeeInBase;
            set => this.RaiseAndSetIfChanged(ref _estimatedMakerNetworkFeeInBase, value);
        }

        protected decimal _estimatedPaymentFee;
        public decimal EstimatedPaymentFee
        {
            get => _estimatedPaymentFee;
            set => this.RaiseAndSetIfChanged(ref _estimatedPaymentFee, value);
        }

        private decimal _estimatedPaymentFeeInBase;
        public decimal EstimatedPaymentFeeInBase
        {
            get => _estimatedPaymentFeeInBase;
            set => this.RaiseAndSetIfChanged(ref _estimatedPaymentFeeInBase, value);
        }

        private decimal _estimatedRedeemFee;
        public decimal EstimatedRedeemFee
        {
            get => _estimatedRedeemFee;
            set => this.RaiseAndSetIfChanged(ref _estimatedRedeemFee, value);
        }

        private decimal _estimatedRedeemFeeInBase;
        public decimal EstimatedRedeemFeeInBase
        {
            get => _estimatedRedeemFeeInBase;
            set => this.RaiseAndSetIfChanged(ref _estimatedRedeemFeeInBase, value);
        }

        private decimal _estimatedTotalNetworkFeeInBase;
        public decimal EstimatedTotalNetworkFeeInBase
        {
            get => _estimatedTotalNetworkFeeInBase;
            set => this.RaiseAndSetIfChanged(ref _estimatedTotalNetworkFeeInBase, value);
        }

        private decimal _rewardForRedeem;
        public decimal RewardForRedeem
        {
            get => _rewardForRedeem;
            set => this.RaiseAndSetIfChanged(ref _rewardForRedeem, value);
        }

        private decimal _rewardForRedeemInBase;
        public decimal RewardForRedeemInBase
        {
            get => _rewardForRedeemInBase;
            set => this.RaiseAndSetIfChanged(ref _rewardForRedeemInBase, value);
        }

        private bool _hasRewardForRedeem;
        public bool HasRewardForRedeem
        {
            get => _hasRewardForRedeem;
            set => this.RaiseAndSetIfChanged(ref _hasRewardForRedeem, value);
        }

        protected string _warning;
        public string Warning
        {
            get => _warning;
            set => this.RaiseAndSetIfChanged(ref _warning, value);
        }

        protected bool _isCriticalWarning;
        public bool IsCriticalWarning
        {
            get => _isCriticalWarning;
            set => this.RaiseAndSetIfChanged(ref _isCriticalWarning, value);
        }

        private bool _canConvert = true;
        public bool CanConvert
        {
            get => _canConvert;
            set => this.RaiseAndSetIfChanged(ref _canConvert, value);
        }

        private ObservableCollection<SwapViewModel> _swaps;
        public ObservableCollection<SwapViewModel> Swaps
        {
            get => _swaps;
            set => this.RaiseAndSetIfChanged(ref _swaps, value);
        }

        private bool _isNoLiquidity;
        public bool IsNoLiquidity
        {
            get => _isNoLiquidity;
            set => this.RaiseAndSetIfChanged(ref _isNoLiquidity, value);
        }

        public int ColumnSpan => DetailsVisible ? 1 : 2;
        public bool DetailsVisible => DGSelectedIndex != -1;
        private SwapDetailsViewModel? SwapDetailsViewModel => DetailsVisible ? Swaps[DGSelectedIndex].Details : null;

        // current selected swap in DataGrid
        private int _dgSelectedIndex = -1;
        public int DGSelectedIndex
        {
            get => _dgSelectedIndex;
            set
            {
                _dgSelectedIndex = value;

                OnPropertyChanged(nameof(DGSelectedIndex));
                OnPropertyChanged(nameof(ColumnSpan));
                OnPropertyChanged(nameof(DetailsVisible));
                OnPropertyChanged(nameof(SwapDetailsViewModel));
            }
        }

        public void CellPointerPressed(int cellIndex)
        {
            DGSelectedIndex = cellIndex;
        }

        public ConversionViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public ConversionViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            // ToCurrencies
            _toCurrencies = this
                .WhenAnyValue(vm => vm.FromCurrencyViewModel)
                .WhereNotNull()
                .Select(vm => FromCurrencies
                    .Where(fc => Symbols.SymbolByCurrencies(fc.Currency, vm.Currency) != null)
                    .ToList())
                .ToProperty(this, nameof(ToCurrencies));

            // PriceFormat
            _priceFormat = this
                .WhenAnyValue(vm => vm.FromCurrencyViewModel, vm => vm.ToCurrencyViewModel)
                .WhereAllNotNull()
                .Select(t =>
                {
                    var symbol = Symbols.SymbolByCurrencies(t.Item1.Currency, t.Item2.Currency); 
                    return symbol != null ? Currencies.GetByName(symbol.Quote).Format : null;
                })
                .WhereNotNull()
                .ToProperty(this, nameof(PriceFormat));

            // Currencies changes => reset amount
            this.WhenAnyValue(vm => vm.FromCurrencyViewModel, vm => vm.ToCurrencyViewModel)
                .WhereAllNotNull()
                .Subscribe(t => { _ = UpdateAmountAsync(value: 0, updateUi: true); });

            SubscribeToServices();
        }

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ??= ReactiveCommand.Create(OnConvertClick);

        private ICommand _maxAmountCommand;
        public ICommand MaxAmountCommand => _maxAmountCommand ??= ReactiveCommand.Create(async () =>
        {
            try
            {
                var swapParams = await Atomex.ViewModels.Helpers
                    .EstimateSwapPaymentParamsAsync(
                        from: FromSource,
                        to: To,
                        amount: EstimatedMaxAmount,
                        fromCurrency: FromCurrency,
                        toCurrency: ToCurrency,
                        account: _app.Account,
                        atomexClient: _app.Terminal,
                        symbolsProvider: _app.SymbolsProvider);

                _amount = Math.Min(swapParams.Amount, EstimatedMaxAmount);
                _ = UpdateAmountAsync(_amount, updateUi: true);
            }
            catch (Exception e)
            {
                Log.Error(e, "Max amount command error.");
            }
        });

        private ICommand _swapCurrenciesCommand;
        public ICommand SwapCurrenciesCommand => _swapCurrenciesCommand ??= ReactiveCommand.Create(() =>
        {
            if (FromCurrencyViewModel == null || ToCurrencyViewModel == null)
                return;

            var previousFromCurrency = FromCurrencyViewModel;
            FromCurrencyViewModel = ToCurrencyViewModel;

            _toCurrencies.

            ToCurrencyViewModel = previousFromCurrency;
        });

        public void SetFromCurrency(CurrencyConfig fromCurrency)
        {
            // TODO

            //var fromCurrencyVm = FromCurrencies
            //    .FirstOrDefault(c => c.Currency?.Name == fromCurrency?.Name);

            //FromCurrencyIndex = FromCurrencies.IndexOf(fromCurrencyVm);
        }

        private void SubscribeToServices()
        {
            _app.AtomexClientChanged += OnTerminalChangedEventHandler;

            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;
        }

        protected virtual async Task UpdateAmountAsync(decimal value, bool updateUi = false)
        {
            Warning = string.Empty;

            try
            {
                IsAmountUpdating = true;

                // estimate max payment amount and max fee
                var swapParams = await Atomex.ViewModels.Helpers
                    .EstimateSwapPaymentParamsAsync(
                        from: FromSource,
                        to: To,
                        amount: value,
                        fromCurrency: FromCurrency,
                        toCurrency: ToCurrency,
                        account: _app.Account,
                        atomexClient: _app.Terminal,
                        symbolsProvider: _app.SymbolsProvider);

                IsCriticalWarning = false;

                if (swapParams.Error != null)
                {
                    Warning = swapParams.Error.Code switch
                    {
                        Errors.InsufficientFunds => Resources.CvInsufficientFunds,
                        Errors.InsufficientChainFunds => string.Format(CultureInfo.InvariantCulture,
                            Resources.CvInsufficientChainFunds, FromCurrency.FeeCurrencyName),
                        _ => Resources.CvError
                    };
                }
                else
                {
                    Warning = string.Empty;
                }

                _estimatedPaymentFee = swapParams.PaymentFee;
                _estimatedMakerNetworkFee = swapParams.MakerNetworkFee;

                //OnPropertyChanged(nameof(CurrencyFormat));
                //OnPropertyChanged(nameof(TargetCurrencyFormat));
                OnPropertyChanged(nameof(EstimatedPaymentFee));
                OnPropertyChanged(nameof(EstimatedMakerNetworkFee));
                //OnPropertyChanged(nameof(FromAmountString));

                IsAmountValid = _amount <= swapParams.Amount.TruncateByFormat(FromCurrencyViewModel.CurrencyFormat);

                if (updateUi)
                    OnPropertyChanged(nameof(AmountString));

                await UpdateRedeemAndRewardFeesAsync();

#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    OnQuotesUpdatedEventHandler(_app.Terminal, null);
                    OnBaseQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
#if DEBUG
                }
#endif
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        private void UpdateTargetAmountInBase(ICurrencyQuotesProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (ToCurrencyViewModel.CurrencyCode == null)
                return;

            if (FromCurrencyViewModel.BaseCurrencyCode == null)
                return;

            var quote = provider.GetQuote(ToCurrencyViewModel.CurrencyCode, FromCurrencyViewModel.BaseCurrencyCode);

            TargetAmountInBase = _targetAmount * (quote?.Bid ?? 0m);
        }

        protected async Task UpdateRedeemAndRewardFeesAsync()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                return;
#endif
            //var walletAddress = await App.Account
            //    .GetCurrencyAccount<ILegacyCurrencyAccount>(ToCurrency.Name)
            //    .GetRedeemAddressAsync();

            //if (RedeemAdderss == null)
            //    return;

            // TODO

            //var walletAddress = await _app.Account
            //    .GetCurrencyAccount(ToCurrency.Name)
            //    .GetAddressAsync(RedeemAddress);

            //_estimatedRedeemFee = await ToCurrency
            //    .GetEstimatedRedeemFeeAsync(walletAddress, withRewardForRedeem: false);

            //_rewardForRedeem = await RewardForRedeemHelper
            //    .EstimateAsync(
            //        account: _app.Account,
            //        quotesProvider: _app.QuotesProvider,
            //        feeCurrencyQuotesProvider: symbol => _app.Terminal?.GetOrderBook(symbol)?.TopOfBook(),
            //        walletAddress: walletAddress);

            //_hasRewardForRedeem = _rewardForRedeem != 0;

            //await Dispatcher.UIThread.InvokeAsync(() =>
            //{
            //    OnPropertyChanged(nameof(EstimatedRedeemFee));
            //    OnPropertyChanged(nameof(RewardForRedeem));
            //    OnPropertyChanged(nameof(HasRewardForRedeem));

            //}, DispatcherPriority.Background);
        }

        private void OnTerminalChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            var terminal = args.AtomexClient;

            if (terminal?.Account == null)
            {
                DGSelectedIndex = -1;
                return;
            }

            terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            terminal.SwapUpdated += OnSwapEventHandler;

            FromCurrencies = terminal.Account.Currencies
                .Where(c => c.IsSwapAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            FromCurrencyViewModel = FromCurrencies.First(c => c.Currency.Name == "BTC");
            ToCurrencyViewModel = ToCurrencies.First(c => c.Currency.Name == "LTC");

            OnSwapEventHandler(this, args: null);
        }

        protected async void OnBaseQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider provider)
                return;

            if (FromCurrencyViewModel == null ||
                FromCurrencyViewModel.CurrencyCode == null ||
                FromCurrencyViewModel.BaseCurrencyCode == null ||
                ToCurrencyViewModel == null ||
                ToCurrencyViewModel.CurrencyCode == null)
                return;

            var fromCurrencyPrice = provider.GetQuote(FromCurrencyViewModel.CurrencyCode, FromCurrencyViewModel.BaseCurrencyCode)?.Bid ?? 0m;
            _amountInBase = _amount * fromCurrencyPrice;

            var fromCurrencyFeePrice = provider.GetQuote(FromCurrency.FeeCurrencyName, FromCurrencyViewModel.BaseCurrencyCode)?.Bid ?? 0m;
            _estimatedPaymentFeeInBase = _estimatedPaymentFee * fromCurrencyFeePrice;

            var toCurrencyFeePrice = provider.GetQuote(ToCurrency.FeeCurrencyName, FromCurrencyViewModel.BaseCurrencyCode)?.Bid ?? 0m;
            _estimatedRedeemFeeInBase = _estimatedRedeemFee * toCurrencyFeePrice;

            var toCurrencyPrice = provider.GetQuote(ToCurrencyViewModel.CurrencyCode, FromCurrencyViewModel.BaseCurrencyCode)?.Bid ?? 0m;
            _rewardForRedeemInBase = _rewardForRedeem * toCurrencyPrice;

            _estimatedMakerNetworkFeeInBase = _estimatedMakerNetworkFee * fromCurrencyPrice;

            _estimatedTotalNetworkFeeInBase =
                _estimatedPaymentFeeInBase +
                (!_hasRewardForRedeem ? _estimatedRedeemFeeInBase : 0) +
                _estimatedMakerNetworkFeeInBase +
                (_hasRewardForRedeem ? _rewardForRedeemInBase : 0);

            if (_amountInBase != 0 && _estimatedTotalNetworkFeeInBase / _amountInBase > 0.3m)
            {
                _isCriticalWarning = true;
                _warning = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.CvTooHighNetworkFee,
                    FormattableString.Invariant($"{_estimatedTotalNetworkFeeInBase:$0.00}"),
                    FormattableString.Invariant($"{_estimatedTotalNetworkFeeInBase / _amountInBase:0.00%}"));
            }
            else if (_amountInBase != 0 && _estimatedTotalNetworkFeeInBase / _amountInBase > 0.1m)
            {
                _isCriticalWarning = false;
                _warning = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.CvSufficientNetworkFee,
                    FormattableString.Invariant($"{_estimatedTotalNetworkFeeInBase:$0.00}"),
                    FormattableString.Invariant($"{_estimatedTotalNetworkFeeInBase / _amountInBase:0.00%}"));
            }

            _canConvert = _amountInBase == 0 || _estimatedTotalNetworkFeeInBase / _amountInBase <= 0.75m;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(AmountInBase));
                OnPropertyChanged(nameof(EstimatedPaymentFeeInBase));
                OnPropertyChanged(nameof(EstimatedRedeemFeeInBase));
                OnPropertyChanged(nameof(RewardForRedeemInBase));

                OnPropertyChanged(nameof(EstimatedMakerNetworkFeeInBase));
                OnPropertyChanged(nameof(EstimatedTotalNetworkFeeInBase));

                OnPropertyChanged(nameof(IsCriticalWarning));
                OnPropertyChanged(nameof(Warning));
                OnPropertyChanged(nameof(CanConvert));

                UpdateTargetAmountInBase(provider);

            }, DispatcherPriority.Background);
        }

        protected async void OnQuotesUpdatedEventHandler(object? sender, MarketDataEventArgs args)
        {
            try
            {
                var swapPriceEstimation = await Atomex.ViewModels.Helpers.EstimateSwapPriceAsync(
                    amount: _amount,
                    fromCurrency: FromCurrency,
                    toCurrency: ToCurrency,
                    account: _app.Account,
                    atomexClient: _app.Terminal,
                    symbolsProvider: _app.SymbolsProvider);

                if (swapPriceEstimation == null)
                    return;

                _targetAmount = swapPriceEstimation.TargetAmount;
                _estimatedPrice = swapPriceEstimation.Price;
                _estimatedOrderPrice = swapPriceEstimation.OrderPrice;
                _estimatedMaxAmount = swapPriceEstimation.MaxAmount;
                _isNoLiquidity = swapPriceEstimation.IsNoLiquidity;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OnPropertyChanged(nameof(EstimatedPrice));
                    OnPropertyChanged(nameof(EstimatedMaxAmount));
                    OnPropertyChanged(nameof(PriceFormat));
                    OnPropertyChanged(nameof(IsNoLiquidity));
                    //OnPropertyChanged(nameof(TargetCurrencyFormat));
                    OnPropertyChanged(nameof(TargetAmount));

                    UpdateTargetAmountInBase(_app.QuotesProvider);

                }, DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Log.Error(e, "Quotes updated event handler error");
            }
        }

        private async void OnSwapEventHandler(object? sender, SwapEventArgs? args)
        {
            try
            {
                var swaps = await _app.Account
                    .GetSwapsAsync();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var swapViewModels = swaps
                        .Select(s =>
                            SwapViewModelFactory.CreateSwapViewModel(s, Currencies, () => { DGSelectedIndex = -1; }))
                        .ToList()
                        .SortList((s1, s2) => s2.Time.ToUniversalTime()
                            .CompareTo(s1.Time.ToUniversalTime()));

                    var previousSwapsCount = Swaps?.Count;
                    Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);

                    if (previousSwapsCount < swapViewModels?.Count)
                        DGSelectedIndex = 0;

                    if (DetailsVisible)
                        OnPropertyChanged(nameof(SwapDetailsViewModel));

                }, DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Log.Error(e, "Swaps update error");
            }
        }

        private void OnConvertClick()
        {
            if (_amount == 0)
            {
                App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: Resources.CvZeroAmount,
                        backAction: () => App.DialogService.Close()));
                return;
            }

            if (!IsAmountValid)
            {
                App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: Resources.CvBigAmount,
                        backAction: () => App.DialogService.Close()));
                return;
            }

            if (EstimatedPrice == 0)
            {
                App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: Resources.CvNoLiquidity,
                        backAction: () => App.DialogService.Close()));
                return;
            }

            if (!_app.Terminal.IsServiceConnected(TerminalService.All))
            {
                App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: Resources.CvServicesUnavailable,
                        backAction: () => App.DialogService.Close()));
                return;
            }

            var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
            if (symbol == null)
            {
                App.DialogService.Show(
                    MessageViewModel.Error(
                        text: Resources.CvNotSupportedSymbol,
                        backAction: () => App.DialogService.Close()));
                return;
            }

            var side = symbol.OrderSideForBuyCurrency(ToCurrency);
            var price = EstimatedPrice;
            var baseCurrency = Currencies.GetByName(symbol.Base);
            var qty = AmountHelper.AmountToQty(side, _amount, price, baseCurrency.DigitsMultiplier);

            if (qty < symbol.MinimumQty)
            {
                var minimumAmount =
                    AmountHelper.QtyToAmount(side, symbol.MinimumQty, price, FromCurrency.DigitsMultiplier);
                var message = string.Format(CultureInfo.InvariantCulture, Resources.CvMinimumAllowedQtyWarning,
                    minimumAmount, FromCurrency.Name);

                App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: message,
                        backAction: () => App.DialogService.Close()));

                return;
            }

            var viewModel = new ConversionConfirmationViewModel(_app)
            {
                FromCurrencyViewModel = FromCurrencyViewModel,
                ToCurrencyViewModel = ToCurrencyViewModel,
                PriceFormat = PriceFormat,

                Amount = _amount,
                AmountInBase = AmountInBase,
                TargetAmount = TargetAmount,
                TargetAmountInBase = TargetAmountInBase,

                EstimatedPrice = EstimatedPrice,
                EstimatedOrderPrice = _estimatedOrderPrice,
                EstimatedPaymentFee = EstimatedPaymentFee,
                EstimatedRedeemFee = EstimatedRedeemFee,
                EstimatedMakerNetworkFee = EstimatedMakerNetworkFee,

                EstimatedPaymentFeeInBase = EstimatedPaymentFeeInBase,
                EstimatedRedeemFeeInBase = EstimatedRedeemFeeInBase,
                EstimatedMakerNetworkFeeInBase = EstimatedMakerNetworkFeeInBase,
                EstimatedTotalNetworkFeeInBase = EstimatedTotalNetworkFeeInBase,

                RewardForRedeem = RewardForRedeem,
                RewardForRedeemInBase = RewardForRedeemInBase,
                HasRewardForRedeem = HasRewardForRedeem
            };

            viewModel.OnSuccess += OnSuccessConvertion;

            App.DialogService.Show(viewModel);
        }

        private void OnSuccessConvertion(object sender, EventArgs e)
        {
            _amount = Math.Min(_amount, EstimatedMaxAmount); // recalculate amount
            _ = UpdateAmountAsync(_amount, updateUi: true);
        }

        private void DesignerMode()
        {
            var btc = DesignTime.Currencies.Get<BitcoinConfig>("BTC");
            var ltc = DesignTime.Currencies.Get<LitecoinConfig>("LTC");

            var currencyViewModels = new List<CurrencyViewModel>
            {
                CurrencyViewModelCreator.CreateViewModel(btc, subscribeToUpdates: false),
                CurrencyViewModelCreator.CreateViewModel(ltc, subscribeToUpdates: false)
            };

            FromCurrencies = currencyViewModels;
            FromCurrencyViewModel = currencyViewModels.FirstOrDefault(c => c.Currency.Name == btc.Name);
            ToCurrencyViewModel = currencyViewModels.FirstOrDefault(c => c.Currency.Name == ltc.Name);

            var swapViewModels = new List<SwapViewModel>()
            {
                SwapViewModelFactory.CreateSwapViewModel(new Swap
                    {
                        Symbol    = "LTC/BTC",
                        Price     = 0.0000888m,
                        Qty       = 0.001000m,
                        Side      = Side.Buy,
                        TimeStamp = DateTime.UtcNow
                    },
                    DesignTime.Currencies),
                SwapViewModelFactory.CreateSwapViewModel(new Swap
                    {
                        Symbol    = "LTC/BTC",
                        Price     = 0.0100808m,
                        Qty       = 0.0043000m,
                        Side      = Side.Sell,
                        TimeStamp = DateTime.UtcNow
                    },
                    DesignTime.Currencies)
            };

            Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);

            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds,
                FromCurrency.FeeCurrencyName);
        }
    }
}