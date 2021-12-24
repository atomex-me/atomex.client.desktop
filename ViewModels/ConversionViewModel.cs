using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
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
                if (Design.IsDesignMode)
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
                if (Design.IsDesignMode)
                    return DesignTime.Currencies;
#endif
                return _app.Account.Currencies;
            }
        }

        private IFromSource FromSource { get; set; }

        [Reactive]
        public decimal FromBalance { get; set; }

        [Reactive]
        public string ToAddress { get; set; }

        [Reactive]
        public string RedeemFromAddress { get; set; }

        [Reactive]
        public List<CurrencyViewModel>? FromCurrencies { get; set; }

        [Reactive]
        public List<CurrencyViewModel>? ToCurrencies { get; set; }

        [Reactive]
        public CurrencyViewModel? FromCurrencyViewModel { get; set; }

        [Reactive]
        public CurrencyViewModel? ToCurrencyViewModel { get; set; }

        [ObservableAsProperty]
        public string PriceFormat { get; }

        private decimal _amount;
        public string AmountString
        {
            get => _amount.ToString(FromCurrencyViewModel?.CurrencyFormat ?? "0");
            set
            {
                if (!decimal.TryParse(
                    s: value,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.CurrentCulture,
                    result: out var amount))
                {
                    _amount = 0;
                }
                else
                {
                    _amount = amount.TruncateByFormat(FromCurrencyViewModel?.CurrencyFormat ?? "0");

                    if (_amount > long.MaxValue)
                        _amount = long.MaxValue;
                }

                this.RaisePropertyChanged(nameof(AmountString));
            }
        }

        [Reactive]
        public decimal AmountInBase { get; set; }

        [Reactive]
        public bool IsSwapParamsEstimating { get; set; }

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

        [ObservableAsProperty]
        public int ColumnSpan { get; }

        [ObservableAsProperty]
        public bool DetailsVisible { get; }

        [ObservableAsProperty]
        public SwapDetailsViewModel? SwapDetailsViewModel { get; }

        // current selected swap in DataGrid
        [Reactive]
        public int DGSelectedIndex { get; set; }

        public void CellPointerPressed(int cellIndex)
        {
            DGSelectedIndex = cellIndex;
        }

#if DEBUG
        public ConversionViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }
#endif

        public ConversionViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            IsAmountValid = true;
            CanConvert = true;
            DGSelectedIndex = -1;

            // FromCurrencyViewModel changed => Update ToCurrencies
            this.WhenAnyValue(vm => vm.FromCurrencyViewModel)
                .WhereNotNull()
                .Subscribe(c =>
                {
                    ToCurrencies = FromCurrencies
                        ?.Where(fc => Symbols.SymbolByCurrencies(fc.Currency, c.Currency) != null)
                        .ToList();
                });

            // FromCurrencyViewModel currency changed => Update AmountString and AmountInBase if need
            this.WhenAnyValue(vm => vm.FromCurrencyViewModel)
                .WhereNotNull()
                .Subscribe(c =>
                {
                    var tempAmountString = AmountString;

                    AmountString = _amount.ToString(); // update amount string with new "from" currency format

                    if (AmountString == tempAmountString)
                        UpdateAmountInBase(); // force update amount in base in case when amount string not changed
                });

            // FromCurrencyViewModel or ToCurrencyViewModel changed => update PriceFormat
            this.WhenAnyValue(vm => vm.FromCurrencyViewModel, vm => vm.ToCurrencyViewModel)
                .WhereAllNotNull()
                .Select(t =>
                {
                    var symbol = Symbols.SymbolByCurrencies(t.Item1.Currency, t.Item2.Currency); 
                    return symbol != null ? Currencies.GetByName(symbol.Quote).Format : null;
                })
                .WhereNotNull()
                .ToPropertyEx(this, vm => vm.PriceFormat);

            // AmountString, FromCurrencyViewModel or ToCurrencyViewModel changed => estimate swap price and target amount
            this.WhenAnyValue(vm => vm.AmountString, vm => vm.FromCurrencyViewModel, vm => vm.ToCurrencyViewModel)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(a =>
                {
                    _ = EstimateSwapParamsAsync(amount: _amount);
                    OnQuotesUpdatedEventHandler(sender: this, args: null);
                });

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

            this.WhenAnyValue(vm => vm.DGSelectedIndex)
                .Select(i => i != -1)
                .ToPropertyEx(this, vm => vm.DetailsVisible);

            this.WhenAnyValue(vm => vm.DetailsVisible)
                .Select(dv => dv ? 1 : 2)
                .ToPropertyEx(this, vm => vm.ColumnSpan);

            this.WhenAnyValue(vm => vm.DetailsVisible)
                .Select(dv => dv ? Swaps?[DGSelectedIndex]?.Details : null)
                .ToPropertyEx(this, vm => vm.SwapDetailsViewModel);

            SubscribeToServices();
        }

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ??= ReactiveCommand.Create(OnConvertClick);

        private ICommand _maxAmountCommand;
        public ICommand MaxAmountCommand => _maxAmountCommand ??= ReactiveCommand.Create(async () =>
        {
            try
            {
                if (FromCurrencyViewModel == null || ToCurrencyViewModel == null)
                    return;

                var swapParams = await Atomex.ViewModels.Helpers
                    .EstimateSwapParamsAsync(
                        from: FromSource,
                        amount: EstimatedMaxAmount,
                        redeemFromAddress: RedeemFromAddress,
                        fromCurrency: FromCurrencyViewModel?.Currency,
                        toCurrency: ToCurrencyViewModel?.Currency,
                        account: _app.Account,
                        atomexClient: _app.Terminal,
                        symbolsProvider: _app.SymbolsProvider,
                        quotesProvider: _app.QuotesProvider);

                if (swapParams == null)
                    return;

                //if (swapParams.Error != null) {
                //    TODO: warning?
                //}

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
            FromCurrencyViewModel = FromCurrencies?.FirstOrDefault(vm => vm.Currency.Name == fromCurrency.Name);
        }

        private void SubscribeToServices()
        {
            _app.AtomexClientChanged += OnTerminalChangedEventHandler;

            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;
        }

        protected virtual async Task EstimateSwapParamsAsync(decimal amount)
        {
            // estimate max payment amount and max fee
            var swapParams = await Atomex.ViewModels.Helpers
                .EstimateSwapParamsAsync(
                    from: FromSource,
                    amount: amount,
                    redeemFromAddress: RedeemFromAddress,
                    fromCurrency: FromCurrencyViewModel?.Currency,
                    toCurrency: ToCurrencyViewModel?.Currency,
                    account: _app.Account,
                    atomexClient: _app.Terminal,
                    symbolsProvider: _app.SymbolsProvider,
                    quotesProvider: _app.QuotesProvider);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsCriticalWarning = false;

                if (swapParams == null)
                {
                    EstimatedPaymentFee      = 0;
                    EstimatedRedeemFee       = 0;
                    RewardForRedeem          = 0;
                    EstimatedMakerNetworkFee = 0;
                    Warning                  = string.Empty;
                    return;
                }

                if (swapParams.Error != null)
                {
                    Warning = swapParams.Error.Code switch
                    {
                        Errors.InsufficientFunds      => Resources.CvInsufficientFunds,
                        Errors.InsufficientChainFunds => string.Format(
                            provider: CultureInfo.CurrentCulture,
                            format: Resources.CvInsufficientChainFunds,
                            arg0: FromCurrencyViewModel?.Currency.FeeCurrencyName),
                        Errors.FromAddressIsNullOrEmpty => Resources.CvFromAddressIsNullOrEmpty,
                        _ => Resources.CvError
                    };
                }
                else
                {
                    Warning = string.Empty;
                }

                EstimatedPaymentFee      = swapParams.PaymentFee;
                EstimatedRedeemFee       = swapParams.RedeemFee;
                RewardForRedeem          = swapParams.RewardForRedeem;
                EstimatedMakerNetworkFee = swapParams.MakerNetworkFee;

                if (FromCurrencyViewModel?.CurrencyFormat != null)
                    IsAmountValid = _amount <= swapParams.Amount.TruncateByFormat(FromCurrencyViewModel.CurrencyFormat);

            }, DispatcherPriority.Background);
        }

        private static decimal TryGetAmountInBase(
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
            currency: FromCurrencyViewModel?.Currency?.FeeCurrencyName,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: EstimatedPaymentFeeInBase);

        private void UpdateEstimatedRedeemFeeInBase() => EstimatedRedeemFeeInBase = TryGetAmountInBase(
            amount: EstimatedRedeemFee,
            currency: ToCurrencyViewModel?.Currency?.FeeCurrencyName,
            baseCurrency: FromCurrencyViewModel?.BaseCurrencyCode,
            provider: _app.QuotesProvider,
            defaultAmountInBase: 0); // EstimatedRedeemFeeInBase);

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
            ToCurrencyViewModel = ToCurrencies?.First(c => c.Currency.Name == "LTC");

            OnSwapEventHandler(this, args: null);
        }

        private void CheckAmountToFeeRatio()
        {
            if (AmountInBase != 0 && EstimatedTotalNetworkFeeInBase / AmountInBase > 0.3m)
            {
                IsCriticalWarning = true;
                Warning = string.Format(
                    provider: CultureInfo.CurrentCulture,
                    format: Resources.CvTooHighNetworkFee,
                    arg0: FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase:$0.00}"),
                    arg1: FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase / AmountInBase:0.00%}"));
            }
            else if (AmountInBase != 0 && EstimatedTotalNetworkFeeInBase / AmountInBase > 0.1m)
            {
                IsCriticalWarning = false;
                Warning = string.Format(
                    provider: CultureInfo.CurrentCulture,
                    format: Resources.CvSufficientNetworkFee,
                    arg0: FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase:$0.00}"),
                    arg1: FormattableString.Invariant($"{EstimatedTotalNetworkFeeInBase / AmountInBase:0.00%}"));
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

        protected async void OnQuotesUpdatedEventHandler(object? sender, MarketDataEventArgs? args)
        {
            try
            {
                var swapPriceEstimation = await Atomex.ViewModels.Helpers.EstimateSwapPriceAsync(
                    amount: _amount,
                    fromCurrency: FromCurrencyViewModel?.Currency,
                    toCurrency: ToCurrencyViewModel?.Currency,
                    account: _app.Account,
                    atomexClient: _app.Terminal,
                    symbolsProvider: _app.SymbolsProvider);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (swapPriceEstimation == null)
                    {
                        TargetAmount       = 0;
                        EstimatedPrice     = 0;
                        EstimatedMaxAmount = 0;
                        IsNoLiquidity      = false;
                        return;
                    }

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
            if (FromCurrencyViewModel == null)
            {
                // TODO: warning
                return;
            }

            if (ToCurrencyViewModel == null)
            {
                // TODO: warning
                return;
            }

            if (FromSource == null)
            {
                return;
            }

            if (ToAddress == null)
            {
                return;
            }

            //if (RedeemFromAddress == null)
            //{
            //    return;
            //}

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

            var symbol = Symbols.SymbolByCurrencies(FromCurrencyViewModel.Currency, ToCurrencyViewModel.Currency);
            if (symbol == null)
            {
                App.DialogService.Show(
                    MessageViewModel.Error(
                        text: Resources.CvNotSupportedSymbol,
                        backAction: () => App.DialogService.Close()));
                return;
            }

            var side         = symbol.OrderSideForBuyCurrency(ToCurrencyViewModel.Currency);
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
                    digitsMultiplier: FromCurrencyViewModel.Currency.DigitsMultiplier);

                var message = string.Format(
                    provider: CultureInfo.CurrentCulture,
                    format: Resources.CvMinimumAllowedQtyWarning,
                    arg0: minimumAmount,
                    arg1: FromCurrencyViewModel.Currency.Name);

                App.DialogService.Show(
                    MessageViewModel.Message(
                        title: Resources.CvWarning,
                        text: message,
                        backAction: () => App.DialogService.Close()));

                return;
            }

            var viewModel = new ConversionConfirmationViewModel(_app)
            {
                FromCurrencyViewModel          = FromCurrencyViewModel,
                ToCurrencyViewModel            = ToCurrencyViewModel,
                FromSource                     = FromSource,
                ToAddress                      = ToAddress,
                RedeemFromAddress              = RedeemFromAddress,

                PriceFormat                    = PriceFormat,
                Amount                         = _amount,
                AmountInBase                   = AmountInBase,
                TargetAmount                   = TargetAmount,
                TargetAmountInBase             = TargetAmountInBase,

                EstimatedPrice                 = EstimatedPrice,
                EstimatedOrderPrice            = _estimatedOrderPrice,
                EstimatedPaymentFee            = EstimatedPaymentFee,
                EstimatedRedeemFee             = EstimatedRedeemFee,
                EstimatedMakerNetworkFee       = EstimatedMakerNetworkFee,

                EstimatedPaymentFeeInBase      = EstimatedPaymentFeeInBase,
                EstimatedRedeemFeeInBase       = EstimatedRedeemFeeInBase,
                EstimatedMakerNetworkFeeInBase = EstimatedMakerNetworkFeeInBase,
                EstimatedTotalNetworkFeeInBase = EstimatedTotalNetworkFeeInBase,

                RewardForRedeem                = RewardForRedeem,
                RewardForRedeemInBase          = RewardForRedeemInBase,
                HasRewardForRedeem             = HasRewardForRedeem
            };

            viewModel.OnSuccess += OnSuccessConvertion;

            App.DialogService.Show(viewModel);
        }

        private void OnSuccessConvertion(object? sender, EventArgs e)
        {
            _amount = Math.Min(_amount, EstimatedMaxAmount); // recalculate amount
            _ = EstimateSwapParamsAsync(_amount);
        }

        private void DesignerMode()
        {
            this.WhenAnyValue(vm => vm.DGSelectedIndex)
                .Select(i => i != -1)
                .ToPropertyEx(this, vm => vm.DetailsVisible);

            this.WhenAnyValue(vm => vm.DetailsVisible)
                .Select(dv => dv ? 1 : 2)
                .ToPropertyEx(this, vm => vm.ColumnSpan);

            this.WhenAnyValue(vm => vm.DetailsVisible)
                .Select(dv => dv ? Swaps?[DGSelectedIndex]?.Details : null)
                .ToPropertyEx(this, vm => vm.SwapDetailsViewModel);

            var btc = DesignTime.Currencies.Get<BitcoinConfig>("BTC");
            var ltc = DesignTime.Currencies.Get<LitecoinConfig>("LTC");

            var currencyViewModels = new List<CurrencyViewModel>
            {
                CurrencyViewModelCreator.CreateViewModel(btc, subscribeToUpdates: false),
                CurrencyViewModelCreator.CreateViewModel(ltc, subscribeToUpdates: false)
            };

            FromCurrencies = currencyViewModels;
            FromCurrencyViewModel = currencyViewModels.First(c => c.Currency.Name == btc.Name);
            ToCurrencies = currencyViewModels;
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
                CultureInfo.CurrentCulture,
                Resources.CvInsufficientChainFunds,
                FromCurrencyViewModel.Currency.FeeCurrencyName);

            IsAmountValid = true;
            CanConvert = true;
            DGSelectedIndex = 1;
        }
    }
}