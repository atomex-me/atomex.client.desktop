﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using Serilog;

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

                var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                    from: From,
                    type: TransactionType.Output,
                    gasLimit: UseDefaultFee ? null : GasLimit,
                    gasPrice: UseDefaultFee ? null : GasPrice,
                    reserve: false);

                var erc20Config = (Erc20Config)Currency;

                if (UseDefaultFee) {
                    if (maxAmountEstimation.Fee > 0) {
                        GasPrice = decimal.ToInt32(erc20Config.GetGasPriceInGwei(maxAmountEstimation.Fee, GasLimit));
                    } else {
                        GasPrice = decimal.ToInt32(await erc20Config.GetGasPriceAsync());
                    }
                }

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Message;
                    WarningToolTip = maxAmountEstimation.Error.Details;
                    WarningType = MessageType.Error;
                    return;
                }

                if (Amount > maxAmountEstimation.Amount)
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
                        gasPrice: GasPrice,
                        reserve: false);

                    if (maxAmountEstimation.Error != null)
                    {
                        Warning = maxAmountEstimation.Error.Message;
                        WarningToolTip = maxAmountEstimation.Error.Details;
                        WarningType = MessageType.Error;
                        return;
                    }

                    if (Amount > maxAmountEstimation.Amount)
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

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: TransactionType.Output,
                        gasLimit: UseDefaultFee ? null : GasLimit,
                        gasPrice: UseDefaultFee ? null : GasPrice,
                        reserve: false);

                var erc20Config = (Erc20Config)Currency;

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    GasPrice = decimal.ToInt32(erc20Config.GetGasPriceInGwei(maxAmountEstimation.Fee, GasLimit));

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Message;
                    WarningToolTip = maxAmountEstimation.Error.Details;
                    WarningType = MessageType.Error;
                    Amount = 0;
                    return;
                }

                Amount = maxAmountEstimation.Amount > 0
                    ? maxAmountEstimation.Amount
                    : 0;
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var ethQuote = quotesProvider.GetQuote(Currency.FeeCurrencyName, BaseCurrencyCode);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AmountInBase = Amount.SafeMultiply(quote?.Bid ?? 0m);
                FeeInBase = FeeAmount.SafeMultiply(ethQuote?.Bid ?? 0m);
            });
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = _app.Account.GetCurrencyAccount<Erc20Account>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: AmountToSend,
                gasLimit: GasLimit,
                gasPrice: GasPrice,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}