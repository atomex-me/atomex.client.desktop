using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Ethereum;
using Avalonia.Controls;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class Erc20SendViewModel : EthereumSendViewModel
    {
        public override string TotalFeeCurrencyCode => _currency.FeeCurrencyName;

        public Erc20SendViewModel()
            : base()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Erc20SendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                if (_app.Account.GetCurrencyAccount(_currency.Name) is not IEstimatable account)
                    return; // todo: error?

                var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                    from: new FromAddress(From),
                    to: To,
                    type: BlockchainTransactionType.Output,
                    fee: UseDefaultFee ? null : GasLimit,
                    feePrice: UseDefaultFee ? null : GasPrice,
                    reserve: false);

                if (Amount > maxAmountEstimation.Amount)
                {
                    if (maxAmountEstimation.Error != null)
                    {
                        Warning = maxAmountEstimation.Error.Description;
                        return;
                    }
                    
                    if (Amount <= CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;
                }

                if (UseDefaultFee)
                {
                    GasLimit = decimal.ToInt32(_currency.GetDefaultFee());
                    GasPrice = decimal.ToInt32(await _currency.GetDefaultFeePriceAsync());
                }
                else
                {
                    if (GasLimit < _currency.GetDefaultFee() || GasPrice == 0)
                        Warning = Resources.CvLowFees;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update amount error", _currency?.Description);
            }
        }

        protected override async Task UpdateGasPrice()
        {
            try
            {
                if (Amount == 0)
                {
                    if (FeeAmount > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;
                    return;
                }

                if (GasPrice == 0)
                {
                    Warning = Resources.CvLowFees;
                    return;
                }

                if (!UseDefaultFee)
                {
                    if (_app.Account.GetCurrencyAccount(_currency.Name) is not IEstimatable account)
                        return; // todo: error?

                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromAddress(From),
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee: GasLimit,
                        feePrice: GasPrice,
                        reserve: false);
                    
                    if (Amount > maxAmountEstimation.Amount)
                    {
                        if (maxAmountEstimation.Error != null)
                        {
                            Warning = maxAmountEstimation.Error.Description;
                            return;
                        }
                    
                        if (Amount <= CurrencyViewModel.AvailableAmount)
                            Warning = Resources.CvInsufficientFunds;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update gas price error", _currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                if (_app.Account.GetCurrencyAccount(_currency.Name) is not IEstimatable account)
                    return; // todo: error?

                if (UseDefaultFee)
                {
                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromAddress(From),
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee: null,
                        feePrice: null,
                        reserve: false);

                    if (maxAmountEstimation.Amount > 0)
                        Amount = maxAmountEstimation.Amount;
                    else if (CurrencyViewModel.AvailableAmount > 0)
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds,
                            _currency.FeeCurrencyName);

                    GasLimit = decimal.ToInt32(_currency.GetDefaultFee());
                    GasPrice = decimal.ToInt32(await _currency.GetDefaultFeePriceAsync());
                }
                else
                {
                    if (GasLimit < _currency.GetDefaultFee() || GasPrice == 0)
                    {
                        Warning = Resources.CvLowFees;

                        if (GasLimit == 0 || GasPrice == 0)
                            Amount = 0;
                    }

                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromAddress(From),
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee: GasLimit,
                        feePrice: GasPrice,
                        reserve: false);

                    Amount = maxAmountEstimation.Amount;

                    if (maxAmountEstimation.Amount < CurrencyViewModel.AvailableAmount)
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds,
                            _currency.FeeCurrencyName);
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", _currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var ethQuote = quotesProvider.GetQuote(_currency.FeeCurrencyName, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = FeeAmount * (ethQuote?.Bid ?? 0m);
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = _app.Account.GetCurrencyAccount<Erc20Account>(_currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: Amount,
                gasLimit: GasLimit,
                gasPrice: GasPrice,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}