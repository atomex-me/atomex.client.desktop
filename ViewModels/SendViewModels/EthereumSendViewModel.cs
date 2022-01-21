using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Ethereum;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        private string FeePriceFormat { get; set; }
        public virtual string TotalFeeCurrencyCode => CurrencyCode;
        public virtual string GasCode => "GAS";
        protected virtual string GasFormat => "F0";
    
        public virtual string GasString
        {
            get => Fee.ToString(GasFormat, CultureInfo.InvariantCulture);
            set 
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                {
                    if (fee == 0)
                        Fee = fee;
    
                    OnPropertyChanged(nameof(GasString));
                    return;
                }
    
                Fee = fee.TruncateByFormat(GasFormat);
                OnPropertyChanged(nameof(GasString));
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
            set { _isFeePriceUpdating = value; OnPropertyChanged(nameof(IsFeePriceUpdating)); }
        }
    
        public virtual string FeePriceString
        {
            get => FeePrice.ToString(FeePriceFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var gasPrice))
                {
                    if (gasPrice == 0)
                        FeePrice = gasPrice;
    
                    OnPropertyChanged(nameof(FeePriceString));
                    return;
                }
    
                FeePrice = gasPrice.TruncateByFormat(FeePriceFormat);
                OnPropertyChanged(nameof(FeePriceString));
            }
        }
    
        protected decimal _totalFee;
    
        private bool _isTotalFeeUpdating;
        public bool IsTotalFeeUpdating
        {
            get => _isTotalFeeUpdating;
            set { _isTotalFeeUpdating = value; OnPropertyChanged(nameof(IsTotalFeeUpdating)); }
        }
    
        public string TotalFeeString
        {
            get => _totalFee
                .ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set { UpdateTotalFeeString(); }
        }
    
        protected string _feePriceCode;
        public string FeePriceCode
        {
            get => _feePriceCode;
            set { _feePriceCode = value; OnPropertyChanged(nameof(FeePriceCode)); }
        }
    
        public EthereumSendViewModel()
            : base()
        {
        }
    
        public EthereumSendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
        }
    
        protected override async Task UpdateAmount()
        {
            try
            {
                if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?
    
                var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                    from: new FromAddress(From),
                    to: To,
                    type: BlockchainTransactionType.Output,
                    fee: UseDefaultFee ? 0 : Fee,
                    feePrice: UseDefaultFee ? 0 : _feePrice,
                    reserve: false);
    
                if (UseDefaultFee)
                {
                    Fee = Currency.GetDefaultFee();
                    // OnPropertyChanged(nameof(GasString));
    
                    FeePrice = await Currency.GetDefaultFeePriceAsync();
                    // OnPropertyChanged(nameof(FeePriceString));
    
                    if (Amount > maxAmountEstimation.Amount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }
    
                    // OnPropertyChanged(nameof(AmountString));
    
                    UpdateTotalFeeString();
                    // OnPropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    if (Amount > maxAmountEstimation.Amount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }
    
                    // OnPropertyChanged(nameof(AmountString));
    
                    if (Fee < Currency.GetDefaultFee() || _feePrice == 0) 
                        Warning = Resources.CvLowFees;
                }
    
                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch
            {
                // ignored
            }
        }
    
        private async void UpdateFeePrice(decimal value)
        {
            // if (IsFeeUpdating)
            //     return;
            //
            // IsFeeUpdating = true;
            //
            // _feePrice = value;
            //
            // Warning = string.Empty;
    
            try
            {
                if (Amount == 0)
                {
                    if (Currency.GetFeeAmount(Fee, FeePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;
                    return;
                }
    
                if (value == 0)
                {
                    Warning = Resources.CvLowFees;
                    UpdateTotalFeeString();
                    // OnPropertyChanged(nameof(TotalFeeString));
                    return;
                }
    
                if (!UseDefaultFee)
                {
                    if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?
    
                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromAddress(From),
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee: Fee,
                        feePrice: FeePrice,
                        reserve: false);
    
                    if (Amount > maxAmountEstimation.Amount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }
    
                    // OnPropertyChanged(nameof(FeePriceString));
    
                    UpdateTotalFeeString();
                    // OnPropertyChanged(nameof(TotalFeeString));
                }
    
                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch
            {
                // ignored
            }
        }
    
        protected override async Task UpdateFee()
        {
            try
            {
                if (Amount == 0)
                {
                    if (Currency.GetFeeAmount(Fee, FeePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;
                    return;
                }
    
                if (Fee < Currency.GetDefaultFee())
                {
                    Warning = Resources.CvLowFees;
                    if (Fee == 0)
                    {
                        UpdateTotalFeeString();
                        // OnPropertyChanged(nameof(TotalFeeString));
                        return;
                    }
                }
    
                if (!UseDefaultFee)
                {
                    if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?
    
                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromAddress(From),
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee: Fee,
                        feePrice: FeePrice,
                        reserve: false);
    
                    if (Amount > maxAmountEstimation.Amount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }
    
                    UpdateTotalFeeString();
                    // OnPropertyChanged(nameof(TotalFeeString));
                    //
                    // OnPropertyChanged(nameof(GasString));
                }
    
                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch
            {
                // ignored
            }
        }
    
        protected async void UpdateTotalFeeString(decimal totalFeeAmount = 0)
        {
            IsTotalFeeUpdating = true;
    
            try
            {
                if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?
    
                var feeAmount = totalFeeAmount > 0
                    ? totalFeeAmount
                    : Currency.GetFeeAmount(Fee, FeePrice) > 0
                        ? await account
                            .EstimateFeeAsync(new FromAddress(From), To, Amount, BlockchainTransactionType.Output)
                        : 0;
    
                if (feeAmount != null)
                    _totalFee = feeAmount.Value;
            }
            finally
            {
                IsTotalFeeUpdating = false;
            }
        }
    
        protected override async Task OnMaxClick()
        {
            try
            {
                var availableAmount = CurrencyViewModel.AvailableAmount;
    
                if (availableAmount == 0)
                    return;
    
                if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?
    
                if (UseDefaultFee)
                {
                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: new FromAddress(From),
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: false);
    
                    if (maxAmountEstimation.Amount > 0)
                        Amount = maxAmountEstimation.Amount;
                    
                    Fee = Currency.GetDefaultFee();
                    // OnPropertyChanged(nameof(GasString));
    
                    FeePrice = await Currency.GetDefaultFeePriceAsync();
                    // OnPropertyChanged(nameof(FeePriceString));
    
                    UpdateTotalFeeString(maxAmountEstimation.Fee);
                    // OnPropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    if (Fee < Currency.GetDefaultFee() || _feePrice == 0)
                    {
                        Warning = Resources.CvLowFees;
                        if (Fee == 0 || FeePrice == 0)
                        {
                            Amount = 0;
                            // OnPropertyChanged(nameof(AmountString));
                            return;
                        }
                    }
    
                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: new FromAddress(From),
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: Fee,
                            feePrice: FeePrice,
                            reserve: false);
    
                    Amount = maxAmountEstimation.Amount;
    
                    if (maxAmountEstimation.Amount == 0 && availableAmount > 0)
                        Warning = Resources.CvInsufficientFunds;
    
                    // OnPropertyChanged(nameof(AmountString));
    
                    UpdateTotalFeeString(maxAmountEstimation.Fee);
                    // OnPropertyChanged(nameof(TotalFeeString));
                }
    
                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch
            {
                // ignored
            }
        }

        protected override void FromClick()
        {
            throw new NotImplementedException();
        }

        protected override void ToClick()
        {
            throw new NotImplementedException();
        }

        protected override async Task OnNextCommand()
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
            
            var error = await Send();
            //
            // var confirmationViewModel = new SendConfirmationViewModel
            // {
            //     Currency           = Currency,
            //     From               = From,
            //     To                 = To,
            //     Amount             = Amount,
            //     AmountInBase       = AmountInBase,
            //     BaseCurrencyCode   = BaseCurrencyCode,
            //     BaseCurrencyFormat = BaseCurrencyFormat,
            //     Fee                = Fee,
            //     UseDeafultFee      = UseDefaultFee,
            //     FeeInBase          = FeeInBase,
            //     FeePrice           = FeePrice,
            //     CurrencyCode       = CurrencyCode,
            //     CurrencyFormat     = CurrencyFormat,
            //
            //     FeeCurrencyCode   = FeeCurrencyCode,
            //     FeeCurrencyFormat = FeeCurrencyFormat,
            //     BackView          = this,
            //     SendCallback      = Send
            // };
            //
            // Desktop.App.DialogService.Show(confirmationViewModel);
        }
    
        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;
    
            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
    
            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
        }
    
        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = App.Account.GetCurrencyAccount<EthereumAccount>(Currency.Name);
    
            return account.SendAsync(
                from: From,
                to: To,
                amount: Amount,
                gasLimit: Fee,
                gasPrice: FeePrice,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}