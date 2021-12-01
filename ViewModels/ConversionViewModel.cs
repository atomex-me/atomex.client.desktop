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
using ReactiveUI.Fody.Helpers;

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

        [Reactive]
        public List<CurrencyViewModel> FromCurrencies { get; set; }

        [Reactive]
        public List<CurrencyViewModel> ToCurrencies { get; set; }

        [Reactive]
        public CurrencyViewModel FromCurrencyViewModel { get; set; }

        private CurrencyConfig? FromCurrency => FromCurrencyViewModel?.Currency;

        [Reactive]
        public CurrencyViewModel ToCurrencyViewModel { get; set; }

        private CurrencyConfig? ToCurrency => ToCurrencyViewModel?.Currency;

        [ObservableAsProperty]
        public string PriceFormat { get; }

        private decimal _amount;
        public string AmountString
        {
            get => _amount.ToString(FromCurrencyViewModel?.CurrencyFormat ?? "0.0", CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(
                    s: value,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.InvariantCulture,
                    result: out var amount))
                {
                    _amount = 0;
                }
                else
                {
                    _amount = amount.TruncateByFormat(FromCurrencyViewModel.CurrencyFormat);

                    if (_amount > long.MaxValue)
                        _amount = long.MaxValue;
                }

                this.RaisePropertyChanged(nameof(AmountString));
            }
        }

        [Reactive]
        public decimal AmountInBase { get; set; }

        [Reactive]
        public bool IsAmountUpdating { get; set; }

        [Reactive]
        public bool IsAmountValid { get; set; }

        [Reactive]
        public decimal TargetAmount { get; set; }

        [Reactive]
        public decimal TargetAmountInBase { get; set; }

        private decimal _estimatedOrderPrice;

        [Reactive]
        public decimal EstimatedPrice { get; set; }

        [Reactive]
        public decimal EstimatedMaxAmount { get; set; }

        [Reactive]
        public decimal EstimatedMakerNetworkFee { get; set; }

        [Reactive]
        public decimal EstimatedMakerNetworkFeeInBase { get; set; }

        [Reactive]
        public decimal EstimatedPaymentFee { get; set; }

        [Reactive]
        public decimal EstimatedPaymentFeeInBase { get; set; }

        [Reactive]
        public decimal EstimatedRedeemFee { get; set; }

        [Reactive]
        public decimal EstimatedRedeemFeeInBase { get; set; }

        [Reactive]
        public decimal EstimatedTotalNetworkFeeInBase { get; set; }

        [Reactive]
        public decimal RewardForRedeem { get; set; }

        [Reactive]
        public decimal RewardForRedeemInBase { get; set; }

        [Reactive]
        public bool HasRewardForRedeem { get; set; }

        [Reactive]
        public string Warning { get; set; }

        [Reactive]
        public bool IsCriticalWarning { get; set; }

        [Reactive]
        public bool CanConvert { get; set; }

        [Reactive]
        public ObservableCollection<SwapViewModel> Swaps { get; set; }

        [Reactive]
        public bool IsNoLiquidity { get; set; }

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
            IsAmountValid = true;
            CanConvert = true;

            // "From" currency changed => Update "To" currencies list
            this.WhenAnyValue(vm => vm.FromCurrencyViewModel)
                .WhereNotNull()
                .Subscribe(c =>
                {
                    ToCurrencies = FromCurrencies
                        .Where(fc => Symbols.SymbolByCurrencies(fc.Currency, c.Currency) != null)
                        .ToList();
                });

            // "From" or "To" currencies changed => update PriceFormat
            this
                .WhenAnyValue(vm => vm.FromCurrencyViewModel, vm => vm.ToCurrencyViewModel)
                .WhereAllNotNull()
                .Select(t =>
                {
                    var symbol = Symbols.SymbolByCurrencies(t.Item1.Currency, t.Item2.Currency); 
                    return symbol != null ? Currencies.GetByName(symbol.Quote).Format : null;
                })
                .WhereNotNull()
                .ToPropertyEx(this, vm => vm.PriceFormat);

            // "From" currency changed => estimate swap params with zero amount
            this.WhenAnyValue(vm => vm.FromCurrencyViewModel)
                .WhereNotNull()
                .Subscribe(t => { _ = EstimateSwapParamsAsync(amount: 0, updateUi: true); });

            // Amount changed => estimate swap params with new amount
            this.WhenAnyValue(vm => vm.AmountString)
                .Subscribe(a => { _ = EstimateSwapParamsAsync(amount: _amount, updateUi: true); });

            // Amount changed => update AmountInBase
            this.WhenAnyValue(vm => vm.AmountString)
                .Subscribe(amount => UpdateAmountInBase());

            // TargetAmount changed => update TargetAmountInBase
            this.WhenAnyValue(vm => vm.TargetAmount)
                .Subscribe(amount => UpdateTargetAmountInBase());

            // EstimatedPaymentFee changed => update EstimatedPaymentFeeInBase
            this.WhenAnyValue(vm => vm.EstimatedPaymentFee)
                .Subscribe(amount => UpdateEstimatedPaymentFeeInBase());

            // EstimatedRedeemFee changed => update EstimatedRedeemFeeInBase
            this.WhenAnyValue(vm => vm.EstimatedRedeemFee)
                .Subscribe(amount => UpdateEstimatedRedeemFeeInBase());

            // RewardForRedeem changed => update RewardForRedeemInBase
            this.WhenAnyValue(vm => vm.RewardForRedeem)
                .Subscribe(amount => UpdateRewardForRedeemInBase());

            // EstimatedMakerNetworkFee changed => update EstimatedMakerNetworkFeeInBase
            this.WhenAnyValue(vm => vm.EstimatedMakerNetworkFee)
                .Subscribe(amount => UpdateEstimatedMakerNetworkFeeInBase());

            // If fees in base currency changed => update TotalNetworkFeeInBase
            this.WhenAnyValue(
                    vm => vm.EstimatedPaymentFeeInBase,
                    vm => vm.HasRewardForRedeem,
                    vm => vm.EstimatedRedeemFeeInBase,
                    vm => vm.EstimatedMakerNetworkFeeInBase,
                    vm => vm.RewardForRedeemInBase)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(t => UpdateTotalNetworkFeeInBase());

            // AmountInBase or EstimatedTotalNetworkFeeInBase changed => check the ratio of the fee to the amount
            this.WhenAnyValue(vm => vm.AmountInBase, vm => vm.EstimatedTotalNetworkFeeInBase)
                .Subscribe(t => CheckAmountToFeeRatio());

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

                AmountString = Math.Min(swapParams.Amount, EstimatedMaxAmount).ToString();
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

        protected virtual async Task EstimateSwapParamsAsync(
            decimal amount,
            bool updateUi = false)
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
                        amount: amount,
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
                        Errors.InsufficientChainFunds => string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.CvInsufficientChainFunds,
                            FromCurrency.FeeCurrencyName),
                        _ => Resources.CvError
                    };
                }
                else
                {
                    Warning = string.Empty;
                }

                EstimatedPaymentFee = swapParams.PaymentFee;
                EstimatedMakerNetworkFee = swapParams.MakerNetworkFee;

                //OnPropertyChanged(nameof(CurrencyFormat));
                //OnPropertyChanged(nameof(TargetCurrencyFormat));
                OnPropertyChanged(nameof(EstimatedPaymentFee));
                OnPropertyChanged(nameof(EstimatedMakerNetworkFee));
                
                if (FromCurrencyViewModel?.CurrencyFormat != null)
                    IsAmountValid = _amount <= swapParams.Amount.TruncateByFormat(FromCurrencyViewModel.CurrencyFormat);

                if (updateUi)

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

        private decimal TryGetAmountInBase(
            decimal amount,
            string? currency,
            string? baseCurrency,
            ICurrencyQuotesProvider provider,
            decimal defaultAmountInBase = 0)
        {
            if (currency == null || baseCurrency == null || provider == null)
                return defaultAmountInBase;

            var quote = provider.GetQuote(currency, baseCurrency);
            return amount * (quote?.Bid ?? 0m);
        }

        private void UpdateAmountInBase() => AmountInBase = TryGetAmountInBase(
            amount: _amount,
            currency: FromCurrencyViewModel?.CurrencyCode,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: AmountInBase);

        private void UpdateTargetAmountInBase() => TargetAmountInBase = TryGetAmountInBase(
            amount: TargetAmount,
            currency: ToCurrencyViewModel?.CurrencyCode,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: TargetAmountInBase);

        private void UpdateEstimatedPaymentFeeInBase() => EstimatedPaymentFeeInBase = TryGetAmountInBase(
            amount: EstimatedPaymentFee,
            currency: FromCurrency?.FeeCurrencyName,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: EstimatedPaymentFeeInBase);

        private void UpdateEstimatedRedeemFeeInBase() => EstimatedRedeemFeeInBase = TryGetAmountInBase(
            amount: EstimatedRedeemFee,
            currency: ToCurrency?.FeeCurrencyName,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: EstimatedRedeemFeeInBase);

        private void UpdateRewardForRedeemInBase() => RewardForRedeemInBase = TryGetAmountInBase(
            amount: RewardForRedeem,
            currency: ToCurrencyViewModel?.CurrencyCode,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: RewardForRedeemInBase);

        private void UpdateEstimatedMakerNetworkFeeInBase() => EstimatedMakerNetworkFeeInBase = TryGetAmountInBase(
            amount: EstimatedMakerNetworkFee,
            currency: FromCurrencyViewModel?.CurrencyCode,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: EstimatedMakerNetworkFeeInBase);

        private void UpdateTotalNetworkFeeInBase() => 
            EstimatedTotalNetworkFeeInBase = EstimatedPaymentFeeInBase +
                (!HasRewardForRedeem ? EstimatedRedeemFeeInBase : 0) +
                EstimatedMakerNetworkFeeInBase +
                (HasRewardForRedeem ? RewardForRedeemInBase : 0);

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

        private void CheckAmountToFeeRatio()
        {
            if (AmountInBase != 0 && EstimatedTotalNetworkFeeInBase / AmountInBase > 0.3m)
            {
                IsCriticalWarning = true;
                Warning = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.CvTooHighNetworkFee,
                    FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase:$0.00}"),
                    FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase / AmountInBase:0.00%}"));
            }
            else if (AmountInBase != 0 && EstimatedTotalNetworkFeeInBase / AmountInBase > 0.1m)
            {
                IsCriticalWarning = false;
                Warning = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.CvSufficientNetworkFee,
                    FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase:$0.00}"),
                    FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase / AmountInBase:0.00%}"));
            }

            CanConvert = AmountInBase == 0 || EstimatedTotalNetworkFeeInBase / AmountInBase <= 0.75m;
        }

        protected async void OnBaseQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateAmountInBase();
                UpdateEstimatedPaymentFeeInBase();
                UpdateEstimatedRedeemFeeInBase();
                UpdateRewardForRedeemInBase();
                UpdateEstimatedMakerNetworkFeeInBase();
                UpdateTotalNetworkFeeInBase();

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

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _estimatedOrderPrice = swapPriceEstimation.OrderPrice;

                    TargetAmount       = swapPriceEstimation.TargetAmount;
                    EstimatedPrice     = swapPriceEstimation.Price;
                    EstimatedMaxAmount = swapPriceEstimation.MaxAmount;
                    IsNoLiquidity      = swapPriceEstimation.IsNoLiquidity;

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
                        .Select(s => SwapViewModelFactory.CreateSwapViewModel(
                            swap: s,
                            currencies: Currencies,
                            onCloseSwap: () => { DGSelectedIndex = -1; }))
                        .ToList()
                        .SortList((s1, s2) =>
                            s2.Time.ToUniversalTime().CompareTo(s1.Time.ToUniversalTime()));

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

            var side         = symbol.OrderSideForBuyCurrency(ToCurrency);
            var price        = EstimatedPrice;
            var baseCurrency = Currencies.GetByName(symbol.Base);

            var qty = AmountHelper.AmountToQty(
                side: side,
                amount: _amount,
                price: price,
                digitsMultiplier: baseCurrency.DigitsMultiplier);

            if (qty < symbol.MinimumQty)
            {
                var minimumAmount = AmountHelper.QtyToAmount(
                    side: side,
                    qty: symbol.MinimumQty,
                    price: price,
                    digitsMultiplier: FromCurrency.DigitsMultiplier);

                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.CvMinimumAllowedQtyWarning,
                    minimumAmount,
                    FromCurrency.Name);

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
                ToCurrencyViewModel   = ToCurrencyViewModel,
                PriceFormat           = PriceFormat,

                Amount             = _amount,
                AmountInBase       = AmountInBase,
                TargetAmount       = TargetAmount,
                TargetAmountInBase = TargetAmountInBase,

                EstimatedPrice           = EstimatedPrice,
                EstimatedOrderPrice      = _estimatedOrderPrice,
                EstimatedPaymentFee      = EstimatedPaymentFee,
                EstimatedRedeemFee       = EstimatedRedeemFee,
                EstimatedMakerNetworkFee = EstimatedMakerNetworkFee,

                EstimatedPaymentFeeInBase      = EstimatedPaymentFeeInBase,
                EstimatedRedeemFeeInBase       = EstimatedRedeemFeeInBase,
                EstimatedMakerNetworkFeeInBase = EstimatedMakerNetworkFeeInBase,
                EstimatedTotalNetworkFeeInBase = EstimatedTotalNetworkFeeInBase,

                RewardForRedeem       = RewardForRedeem,
                RewardForRedeemInBase = RewardForRedeemInBase,
                HasRewardForRedeem    = HasRewardForRedeem
            };

            viewModel.OnSuccess += OnSuccessConvertion;

            App.DialogService.Show(viewModel);
        }

        private void OnSuccessConvertion(object sender, EventArgs e)
        {
            _amount = Math.Min(_amount, EstimatedMaxAmount); // recalculate amount
            _ = EstimateSwapParamsAsync(_amount, updateUi: true);
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
            FromCurrencyViewModel = currencyViewModels.First(c => c.Currency.Name == btc.Name);
            ToCurrencyViewModel = currencyViewModels.First(c => c.Currency.Name == ltc.Name);

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

            Warning = string.Format(
                CultureInfo.InvariantCulture,
                Resources.CvInsufficientChainFunds,
                FromCurrency?.FeeCurrencyName);
        }
    }
}