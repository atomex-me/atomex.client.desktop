using System;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Controls;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        public override Currency Currency
        {
            get => _currency;
            set
            {
                if (_currency != null && _currency != value)
                {
                    // DialogViewer.HideDialog(Dialogs.Send);

                    var sendViewModel = SendViewModelCreator.CreateViewModel(App, value);
                    var sendPageId = SendViewModelCreator.GetSendPageId(value);

                    // DialogViewer.ShowDialog(Dialogs.Send, sendViewModel, defaultPageId: sendPageId);
                    return;
                }

                _currency = value;
                this.RaisePropertyChanged(nameof(Currency));

                CurrencyViewModel = FromCurrencies.FirstOrDefault(c => c.Currency.Name == Currency.Name);

                _amount = 0;
                this.RaisePropertyChanged(nameof(AmountString));

                _fee = 0;
                this.RaisePropertyChanged(nameof(FeeString));

                _feePrice = 0;
                this.RaisePropertyChanged(nameof(FeePriceString));

                this.RaisePropertyChanged(nameof(TotalFeeString));

                FeePriceFormat = _currency.FeePriceFormat;
                FeePriceCode = _currency.FeePriceCode;

                Warning = string.Empty;
            }
        }

        protected string FeePriceFormat { get; set; }
        public virtual string TotalFeeCurrencyCode => CurrencyCode;

        public virtual string GasCode => "GAS";
        public virtual string GasFormat => "F0";

        public virtual string GasString
        {
            get => Fee.ToString(GasFormat, CultureInfo.InvariantCulture);
            set 
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee.TruncateByFormat(GasFormat);
            }
        }

        protected decimal _feePrice;
        public virtual decimal FeePrice
        {
            get => _feePrice;
            set { UpdateFeePrice(value); }
        }

        private bool _isFeePriceUpdating;
        public bool IsFeePriceUpdating
        {
            get => _isFeePriceUpdating;
            set { _isFeePriceUpdating = value; this.RaisePropertyChanged(nameof(IsFeePriceUpdating)); }
        }

        public virtual string FeePriceString
        {
            get => FeePrice.ToString(FeePriceFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var gasPrice))
                    return;

                FeePrice = gasPrice.TruncateByFormat(FeePriceFormat);
            }
        }

        protected decimal _totalFee;

        private bool _isTotalFeeUpdating;
        public bool IsTotalFeeUpdating
        {
            get => _isTotalFeeUpdating;
            set { _isTotalFeeUpdating = value; this.RaisePropertyChanged(nameof(IsTotalFeeUpdating)); }
        }

        public virtual string TotalFeeString
        {
            get => _totalFee
                .ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set { UpdateTotalFeeString(); }
        }

        public override bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                Warning = string.Empty;

                _useDefaultFee = value;
                this.RaisePropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                    Amount = _amount; // recalculate amount and fee using default fee
            }
        }

        protected string _feePriceCode;
        public string FeePriceCode
        {
            get => _feePriceCode;
            set { _feePriceCode = value; this.RaisePropertyChanged(nameof(FeePriceCode)); }
        }

        public EthereumSendViewModel()
            : base()
        {
        }

        public EthereumSendViewModel(
            IAtomexApp app,
            Currency currency)
            : base(app, currency)
        {
        }

        protected override async void UpdateAmount(decimal amount)
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            _amount = amount;

            Warning = string.Empty;

            try
            {
                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                    _fee = Currency.GetDefaultFee();
                    this.RaisePropertyChanged(nameof(GasString));

                    _feePrice = await Currency.GetDefaultFeePriceAsync();
                    this.RaisePropertyChanged(nameof(FeePriceString));

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    this.RaisePropertyChanged(nameof(AmountString));

                    UpdateTotalFeeString();
                    this.RaisePropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    this.RaisePropertyChanged(nameof(AmountString));

                    if (_fee < Currency.GetDefaultFee() || _feePrice == 0) 
                        Warning = Resources.CvLowFees;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        private async void UpdateFeePrice(decimal value)
        {
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            _feePrice = value;

            Warning = string.Empty;

            try
            {
                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;
                    return;
                }

                if (value == 0)
                {
                    Warning = Resources.CvLowFees;
                    UpdateTotalFeeString();
                    this.RaisePropertyChanged(nameof(TotalFeeString));
                    return;
                }

                if (!UseDefaultFee)
                {
                    var (maxAmount, maxFee, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    this.RaisePropertyChanged(nameof(FeePriceString));

                    UpdateTotalFeeString();
                    this.RaisePropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected override async void UpdateFee(decimal fee)
        {
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            Warning = string.Empty;

            try
            {
                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;
                    return;
                }

                if (_fee < Currency.GetDefaultFee())
                {
                    Warning = Resources.CvLowFees;
                    if (fee == 0)
                    {
                        UpdateTotalFeeString();
                        this.RaisePropertyChanged(nameof(TotalFeeString));
                        return;
                    }
                }

                if (!UseDefaultFee)
                {
                    var (maxAmount, maxFee, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    UpdateTotalFeeString();
                    this.RaisePropertyChanged(nameof(TotalFeeString));

                    this.RaisePropertyChanged(nameof(GasString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected async void UpdateTotalFeeString(decimal totalFeeAmount = 0)
        {
            IsTotalFeeUpdating = true;

            try
            {
                var feeAmount = totalFeeAmount > 0
                    ? totalFeeAmount
                    : Currency.GetFeeAmount(_fee, _feePrice) > 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output, _fee, _feePrice)
                        : 0;

                if (feeAmount != null)
                    _totalFee = feeAmount.Value;
            }
            finally
            {
                IsTotalFeeUpdating = false;
            }
        }

        protected override async void OnMaxClick()
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (availableAmount == 0)
                    return;

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                    if (maxAmount > 0)
                        _amount = maxAmount;

                    this.RaisePropertyChanged(nameof(AmountString));

                    _fee = Currency.GetDefaultFee();
                    this.RaisePropertyChanged(nameof(GasString));

                    _feePrice = await Currency.GetDefaultFeePriceAsync();
                    this.RaisePropertyChanged(nameof(FeePriceString));

                    UpdateTotalFeeString(maxFeeAmount);
                    this.RaisePropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
                    {
                        Warning = Resources.CvLowFees;
                        if (_fee == 0 || _feePrice == 0)
                        {
                            _amount = 0;
                            this.RaisePropertyChanged(nameof(AmountString));
                            return;
                        }
                    }

                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    _amount = maxAmount;

                    if (maxAmount == 0 && availableAmount > 0)
                        Warning = Resources.CvInsufficientFunds;

                    this.RaisePropertyChanged(nameof(AmountString));

                    UpdateTotalFeeString(maxFeeAmount);
                    this.RaisePropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected override void OnNextCommand()
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

            var feeAmount = !isToken ? Currency.GetFeeAmount(Fee, FeePrice) : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                Warning = Resources.SvAvailableFundsError;
                return;
            }

            // var confirmationViewModel = new SendConfirmationViewModel(DialogViewer, Dialogs.Send)
            // {
            //     Currency = Currency,
            //     To = To,
            //     Amount = Amount,
            //     AmountInBase = AmountInBase,
            //     BaseCurrencyCode = BaseCurrencyCode,
            //     BaseCurrencyFormat = BaseCurrencyFormat,
            //     Fee = Fee,
            //     UseDeafultFee = UseDefaultFee,
            //     FeeInBase = FeeInBase,
            //     FeePrice = FeePrice,
            //     CurrencyCode = CurrencyCode,
            //     CurrencyFormat = CurrencyFormat,
            //
            //     FeeCurrencyCode = FeeCurrencyCode,
            //     FeeCurrencyFormat = FeeCurrencyFormat
            // };

            // DialogViewer.PushPage(Dialogs.Send, Pages.SendConfirmation, confirmationViewModel);
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
        }
    }
}