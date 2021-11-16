using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

using ReactiveUI;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public abstract class SendViewModel : ViewModelBase
    {
        protected IAtomexApp App { get; set; }

        private List<CurrencyViewModel> _fromCurrencies;

        public virtual List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            set
            {
                _fromCurrencies = value;
                OnPropertyChanged(nameof(FromCurrencies));
            }
        }

        private int _currencyIndex;
        public int CurrencyIndex
        {
            get => _currencyIndex;
            set
            {
                _currencyIndex = value;
                OnPropertyChanged(nameof(CurrencyIndex));

                Currency = FromCurrencies.ElementAt(_currencyIndex).Currency;
            }
        }

        protected CurrencyConfig _currency;

        public virtual CurrencyConfig Currency
        {
            get => _currency;
            set
            {
                if (_currency != null && _currency != value)
                {
                    var sendViewModel = SendViewModelCreator.CreateViewModel(App, value);

                    Desktop.App.DialogService.Show(sendViewModel);
                    return;
                }

                _currency = value;
                OnPropertyChanged(nameof(Currency));

                CurrencyViewModel = FromCurrencies.FirstOrDefault(c => c.Currency.Name == Currency.Name);

                _amount = 0;
                OnPropertyChanged(nameof(AmountString));

                _fee = 0;
                OnPropertyChanged(nameof(FeeString));

                Warning = string.Empty;
            }
        }

        protected CurrencyViewModel _currencyViewModel;

        public virtual CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set
            {
                _currencyViewModel = value;

                CurrencyCode = _currencyViewModel?.CurrencyCode;
                FeeCurrencyCode = _currencyViewModel?.FeeCurrencyCode;
                BaseCurrencyCode = _currencyViewModel?.BaseCurrencyCode;

                CurrencyFormat = _currencyViewModel?.CurrencyFormat;
                FeeCurrencyFormat = CurrencyViewModel?.FeeCurrencyFormat;
                BaseCurrencyFormat = _currencyViewModel?.BaseCurrencyFormat;
            }
        }

        protected string _to;

        public virtual string To
        {
            get => _to;
            set
            {
                _to = value;
                OnPropertyChanged(nameof(To));

                Warning = string.Empty;
            }
        }

        protected string CurrencyFormat { get; set; }
        protected string FeeCurrencyFormat { get; set; }

        private string _baseCurrencyFormat;

        public virtual string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set
            {
                _baseCurrencyFormat = value;
                OnPropertyChanged(nameof(BaseCurrencyFormat));
            }
        }

        protected decimal _amount;

        public decimal Amount
        {
            get => _amount;
            set { UpdateAmount(value); }
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

        public string AmountString
        {
            get => Amount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var amount))
                {
                    if (amount == 0)
                        Amount = amount;

                    OnPropertyChanged(nameof(AmountString));
                    return;
                }

                Amount = amount.TruncateByFormat(CurrencyFormat);
                OnPropertyChanged(nameof(AmountString));
            }
        }

        private bool _isFeeUpdating;

        public bool IsFeeUpdating
        {
            get => _isFeeUpdating;
            set
            {
                _isFeeUpdating = value;
                OnPropertyChanged(nameof(IsFeeUpdating));
            }
        }

        protected decimal _fee;

        public decimal Fee
        {
            get => _fee;
            set { UpdateFee(value); }
        }

        public virtual string FeeString
        {
            get => Fee.ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                {
                    if (fee == 0)
                        Fee = fee;

                    OnPropertyChanged(nameof(FeeString));
                    return;
                }

                Fee = fee.TruncateByFormat(FeeCurrencyFormat);
                OnPropertyChanged(nameof(FeeString));
            }
        }

        protected bool _useDefaultFee;

        public virtual bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                    Amount = _amount; // recalculate amount and fee using default fee
            }
        }

        protected decimal _amountInBase;

        public decimal AmountInBase
        {
            get => _amountInBase;
            set
            {
                _amountInBase = value;
                OnPropertyChanged(nameof(AmountInBase));
            }
        }

        protected decimal _feeInBase;

        public decimal FeeInBase
        {
            get => _feeInBase;
            set
            {
                _feeInBase = value;
                OnPropertyChanged(nameof(FeeInBase));
            }
        }

        protected string _currencyCode;

        public string CurrencyCode
        {
            get => _currencyCode;
            set
            {
                _currencyCode = value;
                OnPropertyChanged(nameof(CurrencyCode));
            }
        }

        protected string _feeCurrencyCode;

        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set
            {
                _feeCurrencyCode = value;
                OnPropertyChanged(nameof(FeeCurrencyCode));
            }
        }

        protected string _baseCurrencyCode;

        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set
            {
                _baseCurrencyCode = value;
                OnPropertyChanged(nameof(BaseCurrencyCode));
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

        private ICommand _backCommand;

        public ICommand BackCommand => _backCommand ??= (_backCommand = ReactiveCommand.Create(() =>
        {
            Desktop.App.DialogService.CloseDialog();
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= (_nextCommand = ReactiveCommand.Create(OnNextCommand));

        protected virtual void OnNextCommand()
        {
            if (string.IsNullOrEmpty(To))
            {
                Warning = Resources.SvEmptyAddressError;
                return;
            }

            if (!Currency.IsValidAddress(To))
            {
                Warning = Resources.SvInvalidAddressError;
                return;
            }

            if (Amount <= 0)
            {
                Warning = Resources.SvAmountLessThanZeroError;
                return;
            }

            if (Fee <= 0)
            {
                Warning = Resources.SvCommissionLessThanZeroError;
                return;
            }

            var isToken = Currency.FeeCurrencyName != Currency.Name;

            var feeAmount = !isToken ? Fee : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                Warning = Resources.SvAvailableFundsError;
                return;
            }

            var confirmationViewModel = new SendConfirmationViewModel
            {
                Currency           = Currency,
                To                 = To,
                Amount             = Amount,
                AmountInBase       = AmountInBase,
                BaseCurrencyCode   = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,
                Fee                = Fee,
                UseDeafultFee      = UseDefaultFee,
                FeeInBase          = FeeInBase,
                CurrencyCode       = CurrencyCode,
                CurrencyFormat     = CurrencyFormat,
            
                FeeCurrencyCode    = FeeCurrencyCode,
                FeeCurrencyFormat  = FeeCurrencyFormat,
                BackView           = this
            };
            
            Desktop.App.DialogService.Show(confirmationViewModel);
        }

        public SendViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public SendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            FromCurrencies = App.Account.Currencies
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            var CurrencyVM = FromCurrencies
                .FirstOrDefault(c => c.Currency.Name == currency.Name);

            CurrencyIndex = FromCurrencies.IndexOf(CurrencyVM);
            
            UseDefaultFee = true; // use default fee by default

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        protected abstract void UpdateAmount(decimal amount);
        protected abstract void UpdateFee(decimal fee);

        protected ICommand _maxCommand;
        public ICommand MaxCommand => _maxCommand ??= (_maxCommand = ReactiveCommand.Create(OnMaxClick));

        protected abstract void OnMaxClick();

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase    = Fee * (quote?.Bid ?? 0m);
        }

        private void DesignerMode()
        {
            FromCurrencies = DesignTime.Currencies
                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                .ToList();

            _currency     = FromCurrencies[0].Currency;
            _to           = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            _amount       = 0.00001234m;
            _amountInBase = 10.23m;
            _fee          = 0.0001m;
            _feeInBase    = 8.43m;
        }
    }
}