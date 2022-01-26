using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class Fa12SendViewModel : SendViewModel
    {
        public Fa12SendViewModel()
            : base()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Fa12SendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
            SelectFromViewModel = new SelectAddressViewModel(App.Account, Currency, true)
            {
                BackAction = () => { Desktop.App.DialogService.Show(this); },
                ConfirmAction = (address, balance) =>
                {
                    From = address;
                    SelectedFromBalance = balance;
                    Desktop.App.DialogService.Show(SelectToViewModel);
                }
            };

            SelectToViewModel = new SelectAddressViewModel(App.Account, Currency)
            {
                BackAction = () => { Desktop.App.DialogService.Show(SelectFromViewModel); },
                ConfirmAction = (address, _) =>
                {
                    To = address;
                    Desktop.App.DialogService.Show(this);
                }
            };
        }

        protected override void FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;

            selectFromViewModel!.ConfirmAction = (address, balance) =>
            {
                From = address;
                SelectedFromBalance = balance;

                Desktop.App.DialogService.Show(this);
            };

            Desktop.App.DialogService.Show(selectFromViewModel);
        }

        protected override void ToClick()
        {
            SelectToViewModel.BackAction = () => Desktop.App.DialogService.Show(this);

            Desktop.App.DialogService.Show(SelectToViewModel);
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
                    fee: 0,
                    feePrice: 0,
                    reserve: false);

                if (Amount > maxAmountEstimation.Amount)
                {
                    if (maxAmountEstimation.Error != null)
                    {
                        Warning = maxAmountEstimation.Error.Description;
                        return;
                    }

                    if (Amount <= CurrencyViewModel.AvailableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }
                }

                if (UseDefaultFee)
                {
                    var estimatedFeeAmount = Amount != 0
                        ? await account.EstimateFeeAsync(
                            from: new FromAddress(From),
                            to: To,
                            amount: Amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    Fee = estimatedFeeAmount ?? Currency.GetDefaultFee();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update amount error", Currency?.Description);
            }
        }

        protected override async Task UpdateFee()
        {
            try
            {
                if (Amount == 0)
                {
                    if (Fee > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;

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
                        fee: 0,
                        feePrice: 0,
                        reserve: false);

                    var estimatedFeeAmount = Amount != 0
                        ? await account.EstimateFeeAsync(
                            from: new FromAddress(From),
                            to: To,
                            amount: Amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    if (Amount > maxAmountEstimation.Amount)
                    {
                        if (maxAmountEstimation.Error != null)
                        {
                            Warning = maxAmountEstimation.Error.Description;
                            return;
                        }

                        if (Amount <= CurrencyViewModel.AvailableAmount)
                        {
                            Warning = Resources.CvInsufficientFunds;
                            return;
                        }
                    }
                    else if (estimatedFeeAmount == null || Fee < estimatedFeeAmount.Value)
                    {
                        Warning = Resources.CvLowFees;
                    }

                    if (Fee > maxAmountEstimation.Fee)
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds,
                            Currency.FeeCurrencyName);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update fee error", Currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?

                var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                    from: new FromAddress(From),
                    to: To,
                    type: BlockchainTransactionType.Output,
                    fee: 0,
                    feePrice: 0,
                    reserve: UseDefaultFee);

                if (UseDefaultFee)
                {
                    if (maxAmountEstimation.Amount > 0)
                        Amount = maxAmountEstimation.Amount;
                    else
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds,
                            Currency.FeeCurrencyName);

                    Fee = maxAmountEstimation.Fee;
                }
                else
                {
                    if (Fee < maxAmountEstimation.Fee)
                    {
                        Warning = Resources.CvLowFees;
                        if (Fee == 0)
                        {
                            Amount = 0;
                            return;
                        }
                    }

                    Amount = maxAmountEstimation.Amount;

                    if (maxAmountEstimation.Amount < CurrencyViewModel.AvailableAmount || Fee > maxAmountEstimation.Fee)
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds,
                            Currency.FeeCurrencyName);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (xtzQuote?.Bid ?? 0m);
        }

        protected override async Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var tokenConfig = (Fa12Config)Currency;
            var tokenContract = tokenConfig.TokenContractAddress;
            const int tokenId = 0;
            const string? tokenType = "FA12";

            var tokenAddress = await TezosTokensSendViewModel.GetTokenAddressAsync(
                account: App.Account,
                address: From,
                tokenContract: tokenContract,
                tokenId: tokenId,
                tokenType: tokenType);

            var currencyName = App.Account.Currencies
                .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == tokenContract)
                ?.Name ?? "FA12";

            var tokenAccount = App.Account.GetTezosTokenAccount<Fa12Account>(
                currency: currencyName,
                tokenContract: tokenContract,
                tokenId: tokenId);

            return await tokenAccount.SendAsync(
                from: tokenAddress.Address,
                to: To,
                amount: Amount,
                fee: Fee,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}