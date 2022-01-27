using System;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        public TezosSendViewModel()
            : base()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
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
                var tezosAccount = App.Account.GetCurrencyAccount<TezosAccount>(Currency.Name);

                var maxAmountEstimation = await tezosAccount.EstimateMaxAmountToSendAsync(
                    from: From,
                    to: To,
                    type: BlockchainTransactionType.Output,
                    reserve: UseDefaultFee);

                if (UseDefaultFee)
                {
                    if (Amount > maxAmountEstimation.Amount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    Fee = maxAmountEstimation.Fee;
                }
                else
                {
                    var availableAmount = maxAmountEstimation.Amount + maxAmountEstimation.Fee;

                    if (Amount > maxAmountEstimation.Amount || Amount + Fee > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                    }
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
                    var tezosAccount = App.Account.GetCurrencyAccount<TezosAccount>(Currency.Name);

                    var maxAmountEstimation = await tezosAccount
                        .EstimateMaxAmountToSendAsync(
                            from: From,
                            to: To,
                            type: BlockchainTransactionType.Output,
                            reserve: false);

                    var availableAmount = maxAmountEstimation.Amount + maxAmountEstimation.Fee;

                    if (Amount + Fee > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    if (Fee < maxAmountEstimation.Fee)
                    {
                        Warning = Resources.CvLowFees;
                    }
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
                var tezosAccount = App.Account.GetCurrencyAccount<TezosAccount>(Currency.Name);

                var maxAmountEstimation = await tezosAccount.EstimateMaxAmountToSendAsync(
                    from: From,
                    to: To,
                    type: BlockchainTransactionType.Output,
                    reserve: UseDefaultFee);

                //if (maxAmountEstimation.Error != null)
                //{
                //    Warning = "";
                //    return;
                //}

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

                        if (Fee < maxAmountEstimation.Fee)
                        {
                            Warning = Resources.CvLowFees;
                        }
                    }
                    else
                    {
                        Amount = 0;
                        Warning = Resources.CvInsufficientFunds;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
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