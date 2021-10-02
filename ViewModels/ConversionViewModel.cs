using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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
using Atomex.Swaps.Helpers;
using Atomex.Wallet.Abstract;
using Avalonia;
using Avalonia.Threading;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ConversionViewModel : ViewModelBase, IConversionViewModel
    {
        protected IAtomexApp App { get; }

        private ISymbols Symbols
        {
            get
            {
#if DEBUG
                if (Env.IsInDesignerMode())
                    return DesignTime.Symbols;
#endif
                return App.SymbolsProvider
                    .GetSymbols(App.Account.Network);
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
                return App.Account.Currencies;
            }
        }

        private List<CurrencyViewModel> _currencyViewModels;

        private List<CurrencyViewModel> _fromCurrencies;

        public List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            private set
            {
                _fromCurrencies = value;
                OnPropertyChanged(nameof(FromCurrencies));
            }
        }

        private List<CurrencyViewModel> _toCurrencies;

        public List<CurrencyViewModel> ToCurrencies
        {
            get => _toCurrencies;
            private set
            {
                _toCurrencies = value;
                OnPropertyChanged(nameof(ToCurrencies));
            }
        }

        private int _fromCurrencyIndex;

        public int FromCurrencyIndex
        {
            get => _fromCurrencyIndex;
            set
            {
                _fromCurrencyIndex = value;
                OnPropertyChanged(nameof(FromCurrencyIndex));

                FromCurrency = FromCurrencies.ElementAt(_fromCurrencyIndex).Currency;
            }
        }

        private int _toCurrencyIndex;

        public int ToCurrencyIndex
        {
            get => _toCurrencyIndex;
            set
            {
                _toCurrencyIndex = value;
                OnPropertyChanged(nameof(ToCurrencyIndex));

                ToCurrency = ToCurrencies.ElementAtOrDefault(_toCurrencyIndex)?.Currency;
            }
        }

        protected CurrencyConfig _fromCurrency;

        public virtual CurrencyConfig FromCurrency
        {
            get => _fromCurrency;
            set
            {
                _fromCurrency = FromCurrencies
                    .FirstOrDefault(c => c.Currency.Name == value?.Name)
                    ?.Currency;

                OnPropertyChanged(nameof(FromCurrency));

                if (_fromCurrency == null)
                    return;

                var oldToCurrency = ToCurrency;

                ToCurrencies = _currencyViewModels
                    .Where(c => Symbols.SymbolByCurrencies(c.Currency, _fromCurrency) != null)
                    .ToList();

                if (oldToCurrency != null &&
                    oldToCurrency.Name != _fromCurrency.Name &&
                    ToCurrencies.FirstOrDefault(c => c.Currency.Name == oldToCurrency.Name) != null)
                {
                    var ToCurrencyVM = ToCurrencies
                        .FirstOrDefault(c => c.Currency.Name == oldToCurrency.Name);
                    ToCurrencyIndex = ToCurrencies.IndexOf(ToCurrencyVM);
                }
                else
                {
                    var ToCurrencyVM = ToCurrencies
                        .First();
                    ToCurrencyIndex = ToCurrencies.IndexOf(ToCurrencyVM);
                }

                FromCurrencyViewModel = _currencyViewModels
                    .First(c => c.Currency.Name == _fromCurrency.Name);

                _amount = 0;
                _ = UpdateAmountAsync(_amount, updateUi: true);
            }
        }

        private CurrencyConfig _toCurrency;

        public CurrencyConfig ToCurrency
        {
            get => _toCurrency;
            set
            {
                _toCurrency = value;
                OnPropertyChanged(nameof(ToCurrency));

                if (_toCurrency == null)
                    return;

                ToCurrencyViewModel = _currencyViewModels.First(c => c.Currency.Name == _toCurrency.Name);

#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    _ = Task.Run(async () =>
                    {
                        await UpdateRedeemAndRewardFeesAsync();
                        OnQuotesUpdatedEventHandler(App.Terminal, null);
                        OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
                    });
#if DEBUG
                }
#endif
            }
        }

        private CurrencyViewModel _fromCurrencyViewModel;

        public CurrencyViewModel FromCurrencyViewModel
        {
            get => _fromCurrencyViewModel;
            set
            {
                _fromCurrencyViewModel = value;
                OnPropertyChanged(nameof(FromCurrencyViewModel));

                CurrencyFormat = _fromCurrencyViewModel?.CurrencyFormat;
                CurrencyCode = _fromCurrencyViewModel?.CurrencyCode;

                FromFeeCurrencyCode = _fromCurrencyViewModel?.FeeCurrencyCode;
                FromFeeCurrencyFormat = _fromCurrencyViewModel?.FeeCurrencyFormat;

                BaseCurrencyFormat = _fromCurrencyViewModel?.BaseCurrencyFormat;
                BaseCurrencyCode = _fromCurrencyViewModel?.BaseCurrencyCode;

                var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                if (symbol != null)
                {
                    var quoteCurrency = Currencies.GetByName(symbol.Quote);

                    PriceFormat = quoteCurrency.Format;
                }
            }
        }

        private CurrencyViewModel _toCurrencyViewModel;

        public CurrencyViewModel ToCurrencyViewModel
        {
            get => _toCurrencyViewModel;
            set
            {
                _toCurrencyViewModel = value;
                OnPropertyChanged(nameof(ToCurrencyViewModel));

                TargetCurrencyFormat = _toCurrencyViewModel?.CurrencyFormat;
                TargetCurrencyCode = _toCurrencyViewModel?.CurrencyCode;

                TargetFeeCurrencyCode = _toCurrencyViewModel?.FeeCurrencyCode;
                TargetFeeCurrencyFormat = _toCurrencyViewModel?.FeeCurrencyFormat;

                var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                if (symbol != null)
                {
                    var quoteCurrency = Currencies.GetByName(symbol.Quote);

                    PriceFormat = quoteCurrency.Format;
                }
            }
        }

        private string _priceFormat;

        public string PriceFormat
        {
            get => _priceFormat;
            set
            {
                _priceFormat = value;
                OnPropertyChanged(nameof(PriceFormat));
            }
        }

        private string _currencyFormat;

        public string CurrencyFormat
        {
            get => _currencyFormat;
            set
            {
                if (value == null)
                    return;

                _currencyFormat = value;
                OnPropertyChanged(CurrencyFormat);
            }
        }

        private string _targetCurrencyFormat;

        public string TargetCurrencyFormat
        {
            get => _targetCurrencyFormat;
            set
            {
                _targetCurrencyFormat = value;
                OnPropertyChanged(TargetCurrencyFormat);
            }
        }

        private string _baseCurrencyFormat;

        public string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set
            {
                _baseCurrencyFormat = value;
                OnPropertyChanged(nameof(BaseCurrencyFormat));
            }
        }

        protected decimal _amount;

        public string AmountString
        {
            get => _amount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
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


                _amount = amount.TruncateByFormat(CurrencyFormat);

                if (_amount > long.MaxValue)
                    _amount = long.MaxValue;

                OnPropertyChanged(nameof(AmountString));

                _ = UpdateAmountAsync(_amount, updateUi: false);
            }
        }

        public string FromAmountString
        {
            get => _amount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
        }

        private decimal _amountInBase;

        public decimal AmountInBase
        {
            get => _amountInBase;
            set
            {
                _amountInBase = value;
                OnPropertyChanged(nameof(AmountInBase));
            }
        }

        private bool _isAmountUpdating;

        public bool IsAmountUpdating
        {
            get => _isAmountUpdating;
            set
            {
                _isAmountUpdating = value;
                OnPropertyChanged(nameof(IsAmountUpdating));
            }
        }

        private bool _isAmountValid = true;

        public bool IsAmountValid
        {
            get => _isAmountValid;
            set
            {
                _isAmountValid = value;
                OnPropertyChanged(nameof(IsAmountValid));
            }
        }

        private decimal _targetAmount;

        public decimal TargetAmount
        {
            get => _targetAmount;
            set
            {
                _targetAmount = value;
                OnPropertyChanged(nameof(TargetAmount));
            }
        }

        private decimal _targetAmountInBase;

        public decimal TargetAmountInBase
        {
            get => _targetAmountInBase;
            set
            {
                _targetAmountInBase = value;
                OnPropertyChanged(nameof(TargetAmountInBase));
            }
        }

        private string _currencyCode;

        public string CurrencyCode
        {
            get => _currencyCode;
            set
            {
                _currencyCode = value;
                OnPropertyChanged(nameof(CurrencyCode));
            }
        }

        private string _fromFeeCurrencyFormat;

        public string FromFeeCurrencyFormat
        {
            get => _fromFeeCurrencyFormat;
            set
            {
                _fromFeeCurrencyFormat = value;
                OnPropertyChanged(nameof(FromFeeCurrencyFormat));
            }
        }

        private string _fromFeeCurrencyCode;

        public string FromFeeCurrencyCode
        {
            get => _fromFeeCurrencyCode;
            set
            {
                _fromFeeCurrencyCode = value;
                OnPropertyChanged(nameof(FromFeeCurrencyCode));
            }
        }

        private string _targetCurrencyCode;

        public string TargetCurrencyCode
        {
            get => _targetCurrencyCode;
            set
            {
                _targetCurrencyCode = value;
                OnPropertyChanged(nameof(TargetCurrencyCode));
            }
        }

        private string _targetFeeCurrencyCode;

        public string TargetFeeCurrencyCode
        {
            get => _targetFeeCurrencyCode;
            set
            {
                _targetFeeCurrencyCode = value;
                OnPropertyChanged(nameof(TargetFeeCurrencyCode));
            }
        }

        private string _targetFeeCurrencyFormat;

        public string TargetFeeCurrencyFormat
        {
            get => _targetFeeCurrencyFormat;
            set
            {
                _targetFeeCurrencyFormat = value;
                OnPropertyChanged(nameof(TargetFeeCurrencyFormat));
            }
        }

        private string _baseCurrencyCode;

        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set
            {
                _baseCurrencyCode = value;
                OnPropertyChanged(nameof(BaseCurrencyCode));
            }
        }

        private decimal _estimatedOrderPrice;
        private decimal _estimatedPrice;

        public decimal EstimatedPrice
        {
            get => _estimatedPrice;
            set
            {
                _estimatedPrice = value;
                OnPropertyChanged(nameof(EstimatedPrice));
            }
        }

        private decimal _estimatedMaxAmount;

        public decimal EstimatedMaxAmount
        {
            get => _estimatedMaxAmount;
            set
            {
                _estimatedMaxAmount = value;
                OnPropertyChanged(nameof(EstimatedMaxAmount));
            }
        }

        private decimal _estimatedMakerNetworkFee;

        public decimal EstimatedMakerNetworkFee
        {
            get => _estimatedMakerNetworkFee;
            set
            {
                _estimatedMakerNetworkFee = value;
                OnPropertyChanged(nameof(EstimatedMakerNetworkFee));
            }
        }

        private decimal _estimatedMakerNetworkFeeInBase;

        public decimal EstimatedMakerNetworkFeeInBase
        {
            get => _estimatedMakerNetworkFeeInBase;
            set
            {
                _estimatedMakerNetworkFeeInBase = value;
                OnPropertyChanged(nameof(EstimatedMakerNetworkFeeInBase));
            }
        }

        protected decimal _estimatedPaymentFee;

        public decimal EstimatedPaymentFee
        {
            get => _estimatedPaymentFee;
            set
            {
                _estimatedPaymentFee = value;
                OnPropertyChanged(nameof(EstimatedPaymentFee));
            }
        }

        private decimal _estimatedPaymentFeeInBase;

        public decimal EstimatedPaymentFeeInBase
        {
            get => _estimatedPaymentFeeInBase;
            set
            {
                _estimatedPaymentFeeInBase = value;
                OnPropertyChanged(nameof(EstimatedPaymentFeeInBase));
            }
        }

        private decimal _estimatedRedeemFee;

        public decimal EstimatedRedeemFee
        {
            get => _estimatedRedeemFee;
            set
            {
                _estimatedRedeemFee = value;
                OnPropertyChanged(nameof(EstimatedRedeemFee));
            }
        }

        private decimal _estimatedRedeemFeeInBase;

        public decimal EstimatedRedeemFeeInBase
        {
            get => _estimatedRedeemFeeInBase;
            set
            {
                _estimatedRedeemFeeInBase = value;
                OnPropertyChanged(nameof(EstimatedRedeemFeeInBase));
            }
        }

        private decimal _estimatedTotalNetworkFeeInBase;

        public decimal EstimatedTotalNetworkFeeInBase
        {
            get => _estimatedTotalNetworkFeeInBase;
            set
            {
                _estimatedTotalNetworkFeeInBase = value;
                OnPropertyChanged(nameof(EstimatedTotalNetworkFeeInBase));
            }
        }

        private decimal _rewardForRedeem;

        public decimal RewardForRedeem
        {
            get => _rewardForRedeem;
            set
            {
                _rewardForRedeem = value;
                OnPropertyChanged(nameof(RewardForRedeem));
            }
        }

        private decimal _rewardForRedeemInBase;

        public decimal RewardForRedeemInBase
        {
            get => _rewardForRedeemInBase;
            set
            {
                _rewardForRedeemInBase = value;
                OnPropertyChanged(nameof(RewardForRedeemInBase));
            }
        }

        private bool _hasRewardForRedeem;

        public bool HasRewardForRedeem
        {
            get => _hasRewardForRedeem;
            set
            {
                _hasRewardForRedeem = value;
                OnPropertyChanged(nameof(HasRewardForRedeem));
            }
        }

        protected string _warning;

        public string Warning
        {
            get => _warning;
            set
            {
                _warning = value;
                OnPropertyChanged(nameof(Warning));
            }
        }

        protected bool _isCriticalWarning;

        public bool IsCriticalWarning
        {
            get => _isCriticalWarning;
            set
            {
                _isCriticalWarning = value;
                OnPropertyChanged(nameof(IsCriticalWarning));
            }
        }

        private bool _canConvert = true;

        public bool CanConvert
        {
            get => _canConvert;
            set
            {
                _canConvert = value;
                OnPropertyChanged(nameof(CanConvert));
            }
        }

        private ObservableCollection<SwapViewModel> _swaps;

        public ObservableCollection<SwapViewModel> Swaps
        {
            get => _swaps;
            set
            {
                _swaps = value;
                OnPropertyChanged(nameof(Swaps));
            }
        }

        private bool _isNoLiquidity;

        public bool IsNoLiquidity
        {
            get => _isNoLiquidity;
            set
            {
                _isNoLiquidity = value;
                OnPropertyChanged(nameof(IsNoLiquidity));
            }
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
            App = app ?? throw new ArgumentNullException(nameof(app));

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
                        amount: EstimatedMaxAmount,
                        fromCurrency: FromCurrency,
                        toCurrency: ToCurrency,
                        account: App.Account,
                        atomexClient: App.Terminal,
                        symbolsProvider: App.SymbolsProvider);

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
            var temp = _fromCurrency;

            var fromCurrencyVm = FromCurrencies.FirstOrDefault(c => c.Currency.Name == _toCurrency.Name);
            FromCurrencyIndex = FromCurrencies.IndexOf(fromCurrencyVm);

            var toCurrencyVm = ToCurrencies.FirstOrDefault(c => c.Currency.Name == temp.Name);
            ToCurrencyIndex = ToCurrencies.IndexOf(toCurrencyVm);
        });

        public void SetFromCurrency(CurrencyConfig fromCurrency)
        {
            var fromCurrencyVm = FromCurrencies
                .FirstOrDefault(c => c.Currency?.Name == fromCurrency?.Name);
            FromCurrencyIndex = FromCurrencies.IndexOf(fromCurrencyVm);
        }

        private void SubscribeToServices()
        {
            App.AtomexClientChanged += OnTerminalChangedEventHandler;

            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;
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
                        amount: value,
                        fromCurrency: FromCurrency,
                        toCurrency: ToCurrency,
                        account: App.Account,
                        atomexClient: App.Terminal,
                        symbolsProvider: App.SymbolsProvider);

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

                OnPropertyChanged(nameof(CurrencyFormat));
                OnPropertyChanged(nameof(TargetCurrencyFormat));
                OnPropertyChanged(nameof(EstimatedPaymentFee));
                OnPropertyChanged(nameof(EstimatedMakerNetworkFee));
                OnPropertyChanged(nameof(FromAmountString));

                IsAmountValid = _amount <= swapParams.Amount.TruncateByFormat(CurrencyFormat);

                if (updateUi)
                    OnPropertyChanged(nameof(AmountString));

                await UpdateRedeemAndRewardFeesAsync();

#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    OnQuotesUpdatedEventHandler(App.Terminal, null);
                    OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
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

            if (TargetCurrencyCode == null)
                return;

            if (BaseCurrencyCode == null)
                return;

            var quote = provider.GetQuote(TargetCurrencyCode, BaseCurrencyCode);

            TargetAmountInBase = _targetAmount * (quote?.Bid ?? 0m);
        }

        protected async Task UpdateRedeemAndRewardFeesAsync()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                return;
#endif
            var walletAddress = await App.Account
                .GetCurrencyAccount<ILegacyCurrencyAccount>(ToCurrency.Name)
                .GetRedeemAddressAsync();

            _estimatedRedeemFee = await ToCurrency
                .GetEstimatedRedeemFeeAsync(walletAddress, withRewardForRedeem: false);

            _rewardForRedeem = await RewardForRedeemHelper
                .EstimateAsync(
                    account: App.Account,
                    quotesProvider: App.QuotesProvider,
                    feeCurrencyQuotesProvider: symbol => App.Terminal?.GetOrderBook(symbol)?.TopOfBook(),
                    walletAddress: walletAddress);

            _hasRewardForRedeem = _rewardForRedeem != 0;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(EstimatedRedeemFee));
                OnPropertyChanged(nameof(RewardForRedeem));
                OnPropertyChanged(nameof(HasRewardForRedeem));
            }, DispatcherPriority.Background);
        }

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var terminal = args.AtomexClient;

            if (terminal?.Account == null)
                return;

            terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            terminal.SwapUpdated += OnSwapEventHandler;

            _currencyViewModels = terminal.Account.Currencies
                .Where(c => c.IsSwapAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            FromCurrencies = _currencyViewModels.ToList();

            var fromCurrencyVm = FromCurrencies
                .FirstOrDefault(c => c.Currency.Name == "BTC");
            FromCurrencyIndex = FromCurrencies.IndexOf(fromCurrencyVm);

            var toCurrencyVm = ToCurrencies
                .FirstOrDefault(c => c.Currency.Name == "LTC");
            ToCurrencyIndex = ToCurrencies.IndexOf(toCurrencyVm);

            OnSwapEventHandler(this, null);
        }

        protected async void OnBaseQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;

            if (_currencyCode == null || _targetCurrencyCode == null || _baseCurrencyCode == null)
                return;

            var fromCurrencyPrice = provider.GetQuote(_currencyCode, _baseCurrencyCode)?.Bid ?? 0m;
            _amountInBase = _amount * fromCurrencyPrice;

            var fromCurrencyFeePrice = provider.GetQuote(FromCurrency.FeeCurrencyName, _baseCurrencyCode)?.Bid ?? 0m;
            _estimatedPaymentFeeInBase = _estimatedPaymentFee * fromCurrencyFeePrice;

            var toCurrencyFeePrice = provider.GetQuote(ToCurrency.FeeCurrencyName, _baseCurrencyCode)?.Bid ?? 0m;
            _estimatedRedeemFeeInBase = _estimatedRedeemFee * toCurrencyFeePrice;

            var toCurrencyPrice = provider.GetQuote(TargetCurrencyCode, _baseCurrencyCode)?.Bid ?? 0m;
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

        protected async void OnQuotesUpdatedEventHandler(object sender, MarketDataEventArgs args)
        {
            try
            {
                var swapPriceEstimation = await Atomex.ViewModels.Helpers.EstimateSwapPriceAsync(
                    amount: _amount,
                    fromCurrency: FromCurrency,
                    toCurrency: ToCurrency,
                    account: App.Account,
                    atomexClient: App.Terminal,
                    symbolsProvider: App.SymbolsProvider);

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
                    OnPropertyChanged(nameof(TargetCurrencyFormat));
                    OnPropertyChanged(nameof(TargetAmount));

                    UpdateTargetAmountInBase(App.QuotesProvider);
                }, DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Log.Error(e, "Quotes updated event handler error");
            }
        }


        private string GetSwapStateDescription(Swap swap)
        {
            string result = string.Empty;
            if (swap.StateFlags.HasFlag(SwapStateFlags.HasSecretHash) &&
                swap.Status.HasFlag(SwapStatus.Initiated) &&
                swap.Status.HasFlag(SwapStatus.Accepted))
            {
                result = "INITIALIZATION: Orders matched, credentials exchanged.";
            }

            if (swap.StateFlags.HasFlag(SwapStateFlags.HasPartyPayment) &&
                swap.StateFlags.HasFlag(SwapStateFlags.IsPartyPaymentConfirmed))
            {
                result = "INITIALIZATION: Counter party(MM) payment successfully completed.";
            }

            if (swap.StateFlags.HasFlag(SwapStateFlags.IsCanceled))
            {
                result = $"Cancelled on {result}";
            }

            return result;
        }


        private async void OnSwapEventHandler(object sender, SwapEventArgs args)
        {
            try
            {
                // if (args?.Swap != null)
                // {
                //     Log.Fatal($"OnSwapEventHandler {args.Swap}\n");
                //     if (args?.ChangedFlag != null)
                //         Log.Fatal($"ChangedFlag {args.ChangedFlag.ToString()}\n");
                //     Log.Fatal("CURRENT SWAP STATE: {@state}", GetSwapStateDescription(args.Swap));
                // }
                
                var swaps = await App.Account
                    .GetSwapsAsync();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var swapViewModels = swaps
                        .Select(s => SwapViewModelFactory.CreateSwapViewModel(s, Currencies))
                        .ToList()
                        .SortList((s1, s2) => s2.Time.ToUniversalTime()
                            .CompareTo(s1.Time.ToUniversalTime()));

                    Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);
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
                Desktop.App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: Resources.CvZeroAmount,
                        backAction: () => Desktop.App.DialogService.CloseDialog())
                );
                return;
            }

            if (!IsAmountValid)
            {
                Desktop.App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: Resources.CvBigAmount,
                        backAction: () => Desktop.App.DialogService.CloseDialog()));
                return;
            }

            if (EstimatedPrice == 0)
            {
                Desktop.App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: Resources.CvNoLiquidity,
                        backAction: () => Desktop.App.DialogService.CloseDialog()));
                return;
            }

            if (!App.Terminal.IsServiceConnected(TerminalService.All))
            {
                Desktop.App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: Resources.CvServicesUnavailable,
                        backAction: () => Desktop.App.DialogService.CloseDialog()));
                return;
            }

            var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
            if (symbol == null)
            {
                Desktop.App.DialogService.Show(
                    MessageViewModel.Error(
                        text: Resources.CvNotSupportedSymbol,
                        backAction: () => Desktop.App.DialogService.CloseDialog()));
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

                Desktop.App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: message,
                        backAction: () => Desktop.App.DialogService.CloseDialog()));
                return;
            }

            var viewModel = new ConversionConfirmationViewModel(App)
            {
                FromCurrency = FromCurrency,
                ToCurrency = ToCurrency,
                FromCurrencyViewModel = FromCurrencyViewModel,
                ToCurrencyViewModel = ToCurrencyViewModel,

                PriceFormat = PriceFormat,
                CurrencyCode = CurrencyCode,
                CurrencyFormat = CurrencyFormat,
                TargetCurrencyCode = TargetCurrencyCode,
                TargetCurrencyFormat = TargetCurrencyFormat,
                BaseCurrencyCode = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,

                FromFeeCurrencyCode = FromFeeCurrencyCode,
                FromFeeCurrencyFormat = FromFeeCurrencyFormat,
                TargetFeeCurrencyCode = TargetFeeCurrencyCode,
                TargetFeeCurrencyFormat = TargetFeeCurrencyFormat,

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

            Desktop.App.DialogService.Show(viewModel);
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

            _currencyViewModels = new List<CurrencyViewModel>
            {
                CurrencyViewModelCreator.CreateViewModel(btc, subscribeToUpdates: false),
                CurrencyViewModelCreator.CreateViewModel(ltc, subscribeToUpdates: false)
            };

            _fromCurrencies = _currencyViewModels;
            _toCurrencies = _currencyViewModels;
            _fromCurrency = btc;
            _toCurrency = ltc;

            FromCurrencyViewModel = _currencyViewModels.FirstOrDefault(c => c.Currency.Name == _fromCurrency.Name);
            ToCurrencyViewModel = _currencyViewModels.FirstOrDefault(c => c.Currency.Name == _toCurrency.Name);

            var swapViewModels = new List<SwapViewModel>()
            {
                SwapViewModelFactory.CreateSwapViewModel(new Swap
                    {
                        Symbol = "LTC/BTC",
                        Price = 0.0000888m,
                        Qty = 0.001000m,
                        Side = Side.Buy,
                        TimeStamp = DateTime.UtcNow
                    },
                    DesignTime.Currencies),
                SwapViewModelFactory.CreateSwapViewModel(new Swap
                    {
                        Symbol = "LTC/BTC",
                        Price = 0.0100808m,
                        Qty = 0.0043000m,
                        Side = Side.Sell,
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