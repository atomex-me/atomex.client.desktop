using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Abstract;
using Atomex.Blockchain;
using Atomex.Client.Desktop.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public abstract class CurrencyViewModel : ViewModelBase, IAssetViewModel, IDisposable
    {
        protected const string PathToIcons = "/Resources/Icons";

        public event EventHandler AmountUpdated;

        protected IAccount _account;
        protected ILocalStorage _localStorage;
        private IQuotesProvider _quotesProvider;
        public CurrencyConfig Currency { get; set; }
        public CurrencyConfig ChainCurrency { get; set; }
        public string Header { get; set; }
        public Color AccentColor { get; set; }
        public string IconPath { get; set; }
        public string? PreviewUrl => null;
        public string DisabledIconPath { get; set; }
        [Reactive] public decimal CurrentQuote { get; set; }
        [Reactive] public decimal? DailyChangePercent { get; set; }
        [Reactive] public decimal TotalAmount { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }
        [Reactive] public decimal AvailableAmount { get; set; }
        [Reactive] public decimal AvailableAmountInBase { get; set; }
        [Reactive] public decimal UnconfirmedAmount { get; set; }
        [Reactive] public decimal UnconfirmedAmountInBase { get; set; }

        public string CurrencyName => Currency.DisplayedName;
        public string CurrencyCode => Currency.Name;
        public string CurrencyDescription => Currency.Description;
        public string FeeCurrencyCode => Currency.FeeCode;
        public string CurrencyFormat => Currency.Format;
        public string FeeCurrencyFormat => Currency.FeeFormat;
        public string BaseCurrencyCode => "USD"; // todo: use base currency from settings
        public string BaseCurrencyFormat => "$0.##"; // todo: use base currency format from settings
        public string FeeName { get; set; }

        [ObservableAsProperty] public bool HasUnconfirmedAmount { get; }
        [Reactive] public decimal PortfolioPercent { get; set; }

        protected CurrencyViewModel(CurrencyConfig currency)
        {
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));

            this.WhenAnyValue(vm => vm.UnconfirmedAmount)
                .Select(ua => ua != 0)
                .ToPropertyExInMainThread(this, vm => vm.HasUnconfirmedAmount);
        }

        protected virtual async Task UpdateAsync()
        {
            var balance = await _account
                .GetBalanceAsync(Currency.Name)
                .ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalAmount = balance.Confirmed.FromTokens(Currency.Decimals);
                AvailableAmount = balance.Confirmed.FromTokens(Currency.Decimals); // todo: use unconfirmed income for Bitcoin based currencies?
                UnconfirmedAmount = (balance.UnconfirmedIncome + balance.UnconfirmedOutcome).FromTokens(Currency.Decimals);

                UpdateQuotesInBaseCurrency(_quotesProvider);

            }, DispatcherPriority.Background);
        }

        public Task UpdateInBackgroundAsync()
        {
            return Task.Run(UpdateAsync);
        }

        public void SubscribeToUpdates(IAccount account, ILocalStorage localStorage)
        {
            _account = account;
            _localStorage = localStorage;
            _localStorage.BalanceChanged += OnBalanceChangedEventHandler;
        }

        public void SubscribeToRatesProvider(IQuotesProvider quotesProvider)
        {
            _quotesProvider = quotesProvider;
            _quotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private async void OnBalanceChangedEventHandler(object? sender, BalanceChangedEventArgs args)
        {
            try
            {
                var needReload = args is TokenBalanceChangedEventArgs eventArgs
                    ? _account.Currencies.FirstOrDefault(c =>
                        c is ITokenConfig tc &&
                        eventArgs.Tokens.Contains((tc.TokenContractAddress, tc.TokenId)) &&
                        c.Name == Currency.Name) != null
                    : args.Currency == Currency.Name;

                if (needReload)
                {
                    await UpdateAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error for currency {Currency.Name}");
            }
        }

        private void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        private void UpdateQuotesInBaseCurrency(IQuotesProvider quotesProvider)
        {
            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            TotalAmountInBase = TotalAmount.SafeMultiply(quote?.Bid ?? 0);
            AvailableAmountInBase = AvailableAmount.SafeMultiply(quote?.Bid ?? 0);
            UnconfirmedAmountInBase = UnconfirmedAmount.SafeMultiply(quote?.Bid ?? 0);
            CurrentQuote = quote?.Bid ?? 0;
            DailyChangePercent = quote?.DailyChangePercent ?? 0;

            AmountUpdated?.Invoke(this, EventArgs.Empty);
        }

        #region IDisposable Support

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;

            if (disposing)
            {
                if (_localStorage != null)
                    _localStorage.BalanceChanged -= OnBalanceChangedEventHandler;

                if (_quotesProvider != null)
                    _quotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
            }

            _disposedValue = true;
        }

        ~CurrencyViewModel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}