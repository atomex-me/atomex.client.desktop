using System;
using System.Threading.Tasks;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ReactiveUI;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.Abstract
{
    public abstract class CurrencyViewModel : ViewModelBase
    {
        private const string PathToImages = "avares://Atomex.Client.Desktop/Resources/Images";

        private IAccount Account { get; set; }
        private ICurrencyQuotesProvider QuotesProvider { get; set; }

        public event EventHandler AmountUpdated;

        public Currency Currency { get; set; }
        public Currency ChainCurrency { get; set; }
        public string Header { get; set; }
        public Brush IconBrush { get; set; }
        public IBrush UnselectedIconBrush { get; set; }
        public Brush IconMaskBrush { get; set; }
        public Color AccentColor { get; set; }
        public Color AmountColor { get; set; }
        public IImage IconPath { get; set; }
        public IImage LargeIconPath { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalAmountInBase { get; set; }
        public decimal AvailableAmount { get; set; }
        public decimal AvailableAmountInBase { get; set; }
        public decimal UnconfirmedAmount { get; set; }

        public decimal UnconfirmedAmountInBase { get; set; }

        //public decimal LockedAmount { get; set; }
        //public decimal LockedAmountInBase { get; set; }
        public string CurrencyCode => Currency.Name;
        public string FeeCurrencyCode => Currency.FeeCode;
        public string BaseCurrencyCode => "USD"; // todo: use base currency from settings
        public string CurrencyFormat => Currency.Format;
        public string FeeCurrencyFormat => Currency.FeeFormat;
        public string BaseCurrencyFormat => "$0.00"; // todo: use base currency format from settings
        public string FeeName { get; set; }

        public bool HasUnconfirmedAmount => UnconfirmedAmount != 0;

        private decimal _portfolioPercent;

        public decimal PortfolioPercent
        {
            get => _portfolioPercent;
            set
            {
                _portfolioPercent = value;
                this.RaisePropertyChanged(nameof(PortfolioPercent));
            }
        }

        protected CurrencyViewModel(Currency currency)
        {
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        }

        protected virtual async Task UpdateAsync()
        {
            var balance = await Account
                .GetBalanceAsync(Currency.Name)
                .ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalAmount = balance.Confirmed;
                this.RaisePropertyChanged(nameof(TotalAmount));

                AvailableAmount = balance.Available;
                this.RaisePropertyChanged(nameof(AvailableAmount));

                UnconfirmedAmount = balance.UnconfirmedIncome + balance.UnconfirmedOutcome;
                this.RaisePropertyChanged(nameof(UnconfirmedAmount));
                this.RaisePropertyChanged(nameof(HasUnconfirmedAmount));

                UpdateQuotesInBaseCurrency(QuotesProvider);
            }, DispatcherPriority.Background);
        }

        public Task UpdateInBackgroundAsync()
        {
            return Task.Run(UpdateAsync);
        }

        public void SubscribeToUpdates(IAccount account)
        {
            Account = account;
            Account.BalanceUpdated += OnBalanceChangedEventHandler;
        }

        public void SubscribeToRatesProvider(ICurrencyQuotesProvider quotesProvider)
        {
            QuotesProvider = quotesProvider;
            QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private async void OnBalanceChangedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name.Equals(args.Currency))
                    await UpdateAsync()
                        .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error for currency {args.Currency}");
            }
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        private void UpdateQuotesInBaseCurrency(ICurrencyQuotesProvider quotesProvider)
        {
            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            TotalAmountInBase = TotalAmount * (quote?.Bid ?? 0m);
            this.RaisePropertyChanged(nameof(TotalAmountInBase));

            AvailableAmountInBase = AvailableAmount * (quote?.Bid ?? 0m);
            this.RaisePropertyChanged(nameof(AvailableAmountInBase));

            UnconfirmedAmountInBase = UnconfirmedAmount * (quote?.Bid ?? 0m);
            this.RaisePropertyChanged(nameof(UnconfirmedAmountInBase));

            //LockedAmountInBase = LockedAmount * (quote?.Bid ?? 0m);
            //OnPropertyChanged(nameof(LockedAmountInBase));

            AmountUpdated?.Invoke(this, EventArgs.Empty);
        }

        protected static string PathToImage(string imageName)
        {
            return $"{PathToImages}/{imageName}";
        }
        
        public IBitmap GetBitmap(string uri)
        {
            Console.WriteLine($"Getting bitmap {uri}");
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var bitmap = new Bitmap(assets.Open(new Uri(uri)));
            return bitmap;
        }

        #region IDisposable Support

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (Account != null)
                        Account.BalanceUpdated -= OnBalanceChangedEventHandler;

                    if (QuotesProvider != null)
                        QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
                }

                Account = null;
                Currency = null;

                _disposedValue = true;
            }
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