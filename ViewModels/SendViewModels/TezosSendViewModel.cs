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
        public string From { get; set; }

        public TezosSendViewModel()
            : base()
        {
        }

        public TezosSendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
        }

        protected override async void UpdateAmount(decimal amount)
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;

            _amount = amount;

            try
            {
                var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?

                var (maxAmount, maxFeeAmount, _) = await account.EstimateMaxAmountToSendAsync(
                    from: new FromAddress(From),
                    to: _to,
                    type: BlockchainTransactionType.Output,
                    fee: UseDefaultFee ? 0 : _fee,
                    feePrice: 0,
                    reserve: false);

                if (UseDefaultFee)
                {
                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    var estimatedFeeAmount = _amount != 0
                        ? await account.EstimateFeeAsync(
                            from: new FromAddress(From),
                            to: To,
                            amount: _amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount ?? Currency.GetDefaultFee(), defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var availableAmount = maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    if (_amount > maxAmount || _amount + feeAmount > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    OnPropertyChanged(nameof(AmountString));

                    Fee = _fee;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
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
                var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, defaultFeePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;

                    return;
                }

                if (!UseDefaultFee)
                {
                    if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?

                    var estimatedFeeAmount = _amount != 0
                        ? await account.EstimateFeeAsync(
                            from: new FromAddress(From),
                            to: To,
                            amount: _amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            from: new FromAddress(From),
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: false);

                    var availableAmount = Currency is BitcoinBasedConfig
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    if (_amount + feeAmount > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }
                    else if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                    {
                        Warning = Resources.CvLowFees;
                    }

                    OnPropertyChanged(nameof(FeeString));
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
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                var defaultFeePrice = await Currency
                    .GetDefaultFeePriceAsync();

                if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?

                var (maxAmount, maxFeeAmount, _) = await account.EstimateMaxAmountToSendAsync(
                    from: new FromAddress(From),
                    to: _to,
                    type: BlockchainTransactionType.Output,
                    fee: 0,
                    feePrice: 0,
                    reserve: UseDefaultFee);

                if (UseDefaultFee)
                {
                    if (maxAmount > 0)
                        _amount = maxAmount;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var availableAmount = maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    if (availableAmount - feeAmount > 0)
                    {
                        _amount = availableAmount - feeAmount;

                        var estimatedFeeAmount = _amount != 0
                            ? await account.EstimateFeeAsync(
                                from: new FromAddress(From),
                                to: To,
                                amount: _amount,
                                type: BlockchainTransactionType.Output)
                            : 0;

                        if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                        {
                            Warning = Resources.CvLowFees;
                            if (_fee == 0)
                            {
                                _amount = 0;
                                OnPropertyChanged(nameof(AmountString));
                                return;
                            }
                        }
                    }
                    else
                    {
                        _amount = 0;

                        Warning = Resources.CvInsufficientFunds;
                    }

                    OnPropertyChanged(nameof(AmountString));
                    OnPropertyChanged(nameof(FeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected override Task<Error> Send(
            SendConfirmationViewModel confirmationViewModel,
            CancellationToken cancellationToken = default)
        {
            var account = App.Account.GetCurrencyAccount<TezosAccount>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: confirmationViewModel.To,
                amount: confirmationViewModel.Amount,
                fee: confirmationViewModel.Fee,
                useDefaultFee: confirmationViewModel.UseDeafultFee,
                cancellationToken: cancellationToken);
        }
    }
}