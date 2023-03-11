using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Ethereum;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class Erc20SendViewModel : EthereumSendViewModel
    {
        public override string TotalFeeCurrencyCode => Currency.FeeCurrencyName;

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
                var account = _app.Account
                    .GetCurrencyAccount<Erc20Account>(Currency.Name);

                var maxAmountEstimation = (EthereumMaxAmountEstimation)await account.EstimateMaxAmountToSendAsync(
                    from: From,
                    type: TransactionType.Output,
                    gasLimit: UseDefaultFee ? null : GasLimit,
                    maxFeePerGas: UseDefaultFee ? null : MaxFeePerGas,
                    reserve: false);

                var erc20Config = (Erc20Config)Currency;
                var gasPrice = maxAmountEstimation.GasPrice;

                if (gasPrice == null)
                    (gasPrice, _) = await erc20Config.GetGasPriceAsync();

                if (gasPrice != null)
                {
                    if (UseDefaultFee)
                    {
                        MaxFeePerGas = gasPrice.MaxFeePerGas;
                        MaxPriorityFeePerGas = gasPrice.MaxPriorityFeePerGas;
                    }

                    BaseFeePerGas = gasPrice.SuggestBaseFee;
                }

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Value.Message;
                    WarningToolTip = maxAmountEstimation.ErrorHint;
                    WarningType = MessageType.Error;
                    return;
                }

                if (Amount > maxAmountEstimation.Amount.FromTokens(erc20Config.Decimals))
                {
                    Warning = Resources.CvInsufficientFunds;
                    WarningToolTip = "";
                    WarningType = MessageType.Error;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update amount error", Currency?.Description);
            }
        }

        protected override async Task UpdateGasPrice()
        {
            try
            {
                if (!UseDefaultFee)
                {
                    var account = _app.Account
                        .GetCurrencyAccount<Erc20Account>(Currency.Name);

                    // estimate max amount with new GasPrice
                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: From,
                        type: TransactionType.Output,
                        gasLimit: GasLimit,
                        maxFeePerGas: MaxFeePerGas,
                        reserve: false);

                    if (maxAmountEstimation.Error != null)
                    {
                        Warning = maxAmountEstimation.Error.Value.Message;
                        WarningToolTip = maxAmountEstimation.ErrorHint;
                        WarningType = MessageType.Error;
                        return;
                    }

                    var erc20Config = (Erc20Config)Currency;

                    if (Amount > maxAmountEstimation.Amount.FromTokens(erc20Config.Decimals))
                    {
                        Warning = Resources.CvInsufficientFunds;
                        WarningToolTip = "";
                        WarningType = MessageType.Error;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update gas price error", Currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount<Erc20Account>(Currency.Name);

                var maxAmountEstimation = (EthereumMaxAmountEstimation)await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: TransactionType.Output,
                        gasLimit: UseDefaultFee ? null : GasLimit,
                        maxFeePerGas: UseDefaultFee ? null : MaxFeePerGas,
                        reserve: false);

                var erc20Config = (Erc20Config)Currency;

                var gasPrice = maxAmountEstimation.GasPrice;

                //if (gasPrice == null)
                //    (gasPrice, _) = await ethConfig.GetGasPriceAsync();

                if (gasPrice != null)
                {
                    if (UseDefaultFee)
                    {
                        MaxFeePerGas = gasPrice.MaxFeePerGas;
                        MaxPriorityFeePerGas = gasPrice.MaxPriorityFeePerGas;
                    }

                    BaseFeePerGas = gasPrice.SuggestBaseFee;
                }

                //if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                //    MaxFeePerGas = erc20Config.GetGasPriceInGwei(maxAmountEstimation.Fee, GasLimit);

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Value.Message;
                    WarningToolTip = maxAmountEstimation.ErrorHint;
                    WarningType = MessageType.Error;
                    Amount = 0;
                    return;
                }

                Amount = maxAmountEstimation.Amount > 0
                    ? maxAmountEstimation.Amount.FromTokens(erc20Config.Decimals)
                    : 0;
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var ethQuote = quotesProvider.GetQuote(Currency.FeeCurrencyName, BaseCurrencyCode);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AmountInBase = Amount.SafeMultiply(quote?.Bid ?? 0m);
                FeeInBase = FeeAmount.SafeMultiply(ethQuote?.Bid ?? 0m);
                EstimatedFeeInBase = EstimatedFee.SafeMultiply(quote?.Bid ?? 0m);
            });
        }

        protected override async Task<Error?> Send(CancellationToken cancellationToken = default)
        {
            var account = _app.Account.GetCurrencyAccount<Erc20Account>(Currency.Name);

            var erc20Config = (Erc20Config)Currency;

            var (_, error) = await account
                .SendAsync(
                    from: From,
                    to: To,
                    amount: AmountToSend.ToTokens(erc20Config.Decimals),
                    gasLimit: GasLimit,
                    maxFeePerGas: MaxFeePerGas,
                    maxPriorityFeePerGas: MaxPriorityFeePerGas,
                    useDefaultFee: UseDefaultFee,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return error;
        }
    }
}