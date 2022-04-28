using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        [Reactive] public bool HasTokens { get; set; }
        [Reactive] public bool HasActiveSwaps { get; set; }

        private ReactiveCommand<MaxAmountEstimation, MaxAmountEstimation> CheckAmountCommand;

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
            CheckAmountCommand = ReactiveCommand.Create<MaxAmountEstimation, MaxAmountEstimation>(estimation => estimation);

            CheckAmountCommand.Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(estimation => CheckAmount(estimation));

            SelectFromViewModel = new SelectAddressViewModel(_app.Account, Currency, SelectAddressMode.SendFrom)
            {
                BackAction = () => { App.DialogService.Show(this); },
                ConfirmAction = walletAddressViewModel =>
                {
                    From = walletAddressViewModel.Address;
                    SelectedFromBalance = walletAddressViewModel.AvailableBalance;
                    App.DialogService.Show(SelectToViewModel);
                }
            };

            SelectToViewModel = new SelectAddressViewModel(_app.Account, Currency)
            {
                BackAction = () => { App.DialogService.Show(SelectFromViewModel); },
                ConfirmAction = walletAddressViewModel =>
                {
                    To = walletAddressViewModel.Address;
                    App.DialogService.Show(this);
                }
            };

            if (Currency.Name == "XTZ")
            {
                CheckTokensAsync();
                CheckActiveSwapsAsync();
            }
        }

        private async void CheckTokensAsync()
        {
            var account = _app.Account
                .GetCurrencyAccount<TezosAccount>(Currency.Name);

            var unpsentTokens = await account
                .GetUnspentTokenAddressesAsync()
                .ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                HasTokens = unpsentTokens.Any(); // todo: use tokens count to calculate reserved fee more accurately

            }).ConfigureAwait(false);
        }

        private async void CheckActiveSwapsAsync()
        {
            var activeSwaps = (await _app.Account
                .GetSwapsAsync()
                .ConfigureAwait(false))
                .Where(s => s.IsActive && (s.SoldCurrency == Currency.Name || s.PurchasedCurrency == Currency.Name));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                HasActiveSwaps = activeSwaps.Any(); // todo: use swaps count to calculate reserved fee more accurately

            }).ConfigureAwait(false);
        }

        protected override void FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;

            selectFromViewModel!.ConfirmAction = walletAddressViewModel =>
            {
                From = walletAddressViewModel.Address;
                SelectedFromBalance = walletAddressViewModel.AvailableBalance;
                App.DialogService.Show(this);
            };
            
            selectFromViewModel.BackAction = () => App.DialogService.Show(this);
            App.DialogService.Show(selectFromViewModel);
        }

        protected override void ToClick()
        {
            SelectToViewModel.BackAction = () => App.DialogService.Show(this);
            App.DialogService.Show(SelectToViewModel);
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount<TezosAccount>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        to: To,
                        type: BlockchainTransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    Fee = maxAmountEstimation.Fee;

                CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
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
                    var account = _app.Account
                        .GetCurrencyAccount<TezosAccount>(Currency.Name);

                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: From,
                            to: To,
                            type: BlockchainTransactionType.Output,
                            reserve: false);

                    CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
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
                var account = _app.Account
                    .GetCurrencyAccount<TezosAccount>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        to: To,
                        type: BlockchainTransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    Fee = maxAmountEstimation.Fee;

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Description;
                    WarningToolTip = maxAmountEstimation.Error.Details;
                    WarningType = MessageType.Error;
                    Amount = 0;
                    return;
                }

                var (fa12TransferFee, _) = await _app.Account
                    .GetCurrencyAccount<Fa12Account>("TZBTC")
                    .EstimateTransferFeeAsync(From);

                var maxAmount = UseDefaultFee
                    ? maxAmountEstimation.Amount
                    : maxAmountEstimation.Amount + maxAmountEstimation.Fee - Fee;

                RecommendedMaxAmount = HasActiveSwaps
                    ? Math.Max(maxAmount - maxAmountEstimation.Reserved, 0)
                    : HasTokens
                        ? Math.Max(maxAmount - fa12TransferFee, 0)
                        : maxAmount;

                Amount = maxAmount > 0
                    ? HasActiveSwaps
                        ? RecommendedMaxAmount
                        : maxAmount
                    : 0;

                CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        private async void CheckAmount(MaxAmountEstimation maxAmountEstimation)
        {
            if (maxAmountEstimation.Error != null)
            {
                Warning = maxAmountEstimation.Error.Description;
                WarningToolTip = maxAmountEstimation.Error.Details;
                WarningType = MessageType.Error;
                return;
            }

            if (Amount + Fee > maxAmountEstimation.Amount + maxAmountEstimation.Fee)
            {
                Warning = Resources.CvInsufficientFunds;
                WarningToolTip = "";
                WarningType = MessageType.Error;
                return;
            }

            if (Fee < maxAmountEstimation.Fee)
            {
                Warning = Resources.CvLowFees;
                WarningToolTip = "";
                WarningType = MessageType.Error;
                return;
            }

            var (fa12TransferFee, _) = await _app.Account
                .GetCurrencyAccount<Fa12Account>("TZBTC")
                .EstimateTransferFeeAsync(From);

            var maxAmount = UseDefaultFee
                ? maxAmountEstimation.Amount
                : maxAmountEstimation.Amount + maxAmountEstimation.Fee - Fee;

            RecommendedMaxAmount = HasActiveSwaps
                ? Math.Max(maxAmount - maxAmountEstimation.Reserved, 0)
                : HasTokens
                    ? Math.Max(maxAmount - fa12TransferFee, 0)
                    : maxAmount;

            if (HasActiveSwaps && Amount > RecommendedMaxAmount)
            {
                RecommendedMaxAmountWarning = string.Format(Resources.MaxAmountToSendWithActiveSwaps,
                    RecommendedMaxAmount, // amount
                    Currency.Name);       // currency code

                RecommendedMaxAmountWarningToolTip = string.Format(Resources.MaxAmountToSendWithActiveSwapsDetails,
                    RecommendedMaxAmount, // amount
                    Currency.Name);       // currency code

                RecommendedMaxAmountWarningType = MessageType.Error;
                ShowAdditionalConfirmation = false;
            }
            else if (HasActiveSwaps && Amount == RecommendedMaxAmount)
            {
                RecommendedMaxAmountWarning = string.Format(Resources.MaxAmountToSendWithActiveSwaps,
                    RecommendedMaxAmount, // amount
                    Currency.Name);       // currency code

                RecommendedMaxAmountWarningToolTip = string.Format(Resources.MaxAmountToSendWithActiveSwapsDetails,
                    RecommendedMaxAmount, // amount
                    Currency.Name);       // currency code

                RecommendedMaxAmountWarningType = MessageType.Warning;
                ShowAdditionalConfirmation = false;
            }
            else if (!HasActiveSwaps && HasTokens && Amount >= RecommendedMaxAmount)
            {
                RecommendedMaxAmountWarning = string.Format(Resources.MaxAmountToSendRecommendation,
                    RecommendedMaxAmount, // amount
                    Currency.Name);       // currency code

                RecommendedMaxAmountWarningToolTip = string.Format(Resources.MaxAmountToSendRecommendationDetails,
                    RecommendedMaxAmount, // amount
                    Currency.Name);       // currency code

                RecommendedMaxAmountWarningType = MessageType.Regular;
                ShowAdditionalConfirmation = true;
            }
            else if (!HasActiveSwaps)
            {
                RecommendedMaxAmountWarning = null;
                RecommendedMaxAmountWarningToolTip = null;
                RecommendedMaxAmountWarningType = MessageType.Regular;
                ShowAdditionalConfirmation = false;
            }
        }

        protected override async Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = _app.Account
                .GetCurrencyAccount<TezosAccount>(Currency.Name);

            var (_, error) = await account
                .SendAsync(
                    from: From,
                    to: To,
                    amount: AmountToSend,
                    fee: Fee,
                    useDefaultFee: UseDefaultFee,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return error;
        }
    }
}