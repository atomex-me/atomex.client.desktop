using System;
using System.Globalization;
using System.Linq;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Controls;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class Erc20SendViewModel : EthereumSendViewModel
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

        public override string TotalFeeCurrencyCode => Currency.FeeCurrencyName;

        public override decimal FeePrice
        {
            get => _feePrice;
            set { UpdateFeePrice(value); }
        }

        public Erc20SendViewModel()
            : base()
        {
        }

        public Erc20SendViewModel(
            IAtomexApp app,
            Currency currency)
            : base(app, currency)
        {
        }

        public override bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                Warning = string.Empty;

                _useDefaultFee = value;
                this.RaisePropertyChanged(nameof(UseDefaultFee));

                Amount = _amount; // recalculate amount
            }
        }

        protected override async void UpdateAmount(decimal amount)
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            var availableAmount = CurrencyViewModel.AvailableAmount;
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
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
                        else
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
                    var (maxAmount, _, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    if (_amount > maxAmount)
                    {
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
                        else
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
                        var availableAmount = CurrencyViewModel.AvailableAmount;

                        if(_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
                        else
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
                        var availableAmount = CurrencyViewModel.AvailableAmount;

                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
                        else
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
                    else if(CurrencyViewModel.AvailableAmount > 0)
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);

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

                    if (maxAmount < availableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);

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

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var ethQuote = quotesProvider.GetQuote(Currency.FeeCurrencyName, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (ethQuote?.Bid ?? 0m);
        }
    }
}