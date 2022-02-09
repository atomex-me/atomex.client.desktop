using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Serilog;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;

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
                ConfirmAction = walletAddressViewModel =>
                {
                    From = walletAddressViewModel.Address;
                    SelectedFromBalance = walletAddressViewModel.AvailableBalance;

                    Desktop.App.DialogService.Show(SelectToViewModel);
                }
            };

            SelectToViewModel = new SelectAddressViewModel(
                App.Account,
                App.Account.Currencies.Get<TezosConfig>(TezosConfig.Xtz))
            {
                BackAction = () => { Desktop.App.DialogService.Show(SelectFromViewModel); },
                ConfirmAction = walletAddressViewModel =>
                {
                    To = walletAddressViewModel.Address;
                    Desktop.App.DialogService.Show(this);
                }
            };
        }

        protected override void FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;

            selectFromViewModel!.ConfirmAction = walletAddressViewModel =>
            {
                From = walletAddressViewModel.Address;
                SelectedFromBalance = walletAddressViewModel.AvailableBalance;
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
                var account = App.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    Fee = maxAmountEstimation.Fee;

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Description;
                    return;
                }

                if (Amount > maxAmountEstimation.Amount)
                {
                    Warning = Resources.CvInsufficientFunds;
                    return;
                }

                if (Fee < maxAmountEstimation.Fee)
                    Warning = Resources.CvLowFees;
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
                if (!UseDefaultFee)
                {
                    var account = App.Account
                        .GetCurrencyAccount<Fa12Account>(Currency.Name);

                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: From,
                            type: BlockchainTransactionType.Output,
                            reserve: false);

                    if (maxAmountEstimation.Error != null)
                    {
                        Warning = maxAmountEstimation.Error.Description;
                        return;
                    }

                    if (Amount > maxAmountEstimation.Amount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    if (Fee < maxAmountEstimation.Fee)
                        Warning = Resources.CvLowFees;
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
                var account = App.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    Fee = maxAmountEstimation.Fee;

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Description;
                    Amount = 0;
                    return;
                }

                Amount = maxAmountEstimation.Amount > 0
                    ? maxAmountEstimation.Amount
                    : 0;

                if (Fee < maxAmountEstimation.Fee)
                    Warning = Resources.CvLowFees;
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