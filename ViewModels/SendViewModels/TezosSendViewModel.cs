using System;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        public TezosSendViewModel()
            : base()
        {
        }

        public TezosSendViewModel(
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
                    fee: UseDefaultFee ? 0 : Fee,
                    feePrice: 0,
                    reserve: false);

                if (UseDefaultFee)
                {
                    if (Amount > maxAmountEstimation.Amount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    var estimatedFeeAmount = Amount != 0
                        ? await account.EstimateFeeAsync(
                            from: new FromAddress(From),
                            to: To,
                            amount: Amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    Fee = estimatedFeeAmount ?? Currency.GetDefaultFee();
                }
                else
                {
                    var availableAmount = maxAmountEstimation.Amount + maxAmountEstimation.Fee;

                    if (Amount > maxAmountEstimation.Amount || Amount + Fee > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    // Fee = _fee;
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
                    if (Fee > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;

                    return;
                }

                if (!UseDefaultFee)
                {
                    if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?

                    var estimatedFeeAmount = Amount != 0
                        ? await account.EstimateFeeAsync(
                            from: new FromAddress(From),
                            to: To,
                            amount: Amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: new FromAddress(From),
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: false);

                    var availableAmount = Currency is BitcoinBasedConfig
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmountEstimation.Amount + maxAmountEstimation.Fee;

                    if (Amount + Fee > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }
                    else if (estimatedFeeAmount == null || Fee < estimatedFeeAmount.Value)
                    {
                        Warning = Resources.CvLowFees;
                    }
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch
            {
                // ignored
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

                    Fee = maxAmountEstimation.Fee;
                }
                else
                {
                    var availableAmount = maxAmountEstimation.Amount + maxAmountEstimation.Fee;

                    if (availableAmount - Fee > 0)
                    {
                        Amount = availableAmount - Fee;

                        var estimatedFeeAmount = Amount != 0
                            ? await account.EstimateFeeAsync(
                                from: new FromAddress(From),
                                to: To,
                                amount: Amount,
                                type: BlockchainTransactionType.Output)
                            : 0;

                        if (estimatedFeeAmount == null || Fee < estimatedFeeAmount.Value)
                        {
                            Warning = Resources.CvLowFees;
                            if (Fee == 0)
                            {
                                Amount = 0;
                                return;
                            }
                        }
                    }
                    else
                    {
                        Amount = 0;
                        Warning = Resources.CvInsufficientFunds;
                    }
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch
            {
                // ignored
            }
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = App.Account.GetCurrencyAccount<TezosAccount>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: Amount,
                fee: Fee,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}