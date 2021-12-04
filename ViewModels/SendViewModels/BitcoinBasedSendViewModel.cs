using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        // TODO: select outputs from UI
        public IEnumerable<BitcoinBasedTxOutput> Outputs { get; set; }

        protected decimal _feeRate;
        public decimal FeeRate
        {
            get => _feeRate;
            set { _feeRate = value; OnPropertyChanged(nameof(FeeRate)); }
        }

        private BitcoinBasedConfig Config => (BitcoinBasedConfig)Currency;

        public BitcoinBasedSendViewModel()
            : base()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public BitcoinBasedSendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
        }

        protected override async void UpdateAmount(decimal amount)
        {
            IsAmountUpdating = true;

            _amount = amount;
            Warning = string.Empty;

            try
            {
                var account = App.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    FeeRate = await Config.GetFeeRateAsync();

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeRateAsync(
                        availableOutputs: Outputs,
                        to: _to,
                        amount: _amount,
                        feeRate: _feeRate,
                        account: account);

                    if (transactionParams == null)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    _fee = Config.SatoshiToCoin((long)transactionParams.FeeInSatoshi);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: _to,
                        amount: _amount,
                        fee: _fee,
                        account: account);

                    if (transactionParams == null)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    FeeRate = transactionParams.FeeRate;
                    //Fee = _fee; // recalculate fee
                }

                OnPropertyChanged(nameof(AmountString));

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
                var account = App.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

                var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                    availableOutputs: Outputs,
                    to: _to,
                    amount: _amount,
                    fee: _fee,
                    account: account);

                if (transactionParams == null)
                {
                    Warning = Resources.CvInsufficientFunds;
                    IsFeeUpdating = false;
                    return;
                }

                var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                if (_fee < minimumFee)
                    Warning = Resources.CvLowFees;

                FeeRate = transactionParams.FeeRate;

                //OnPropertyChanged(nameof(AmountString));
                OnPropertyChanged(nameof(FeeString));

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
                if (UseDefaultFee) // auto fee
                {
                    if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?

                    FeeRate = await Config.GetFeeRateAsync();

                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromOutputs(Outputs),
                        to: _to,
                        type: BlockchainTransactionType.Output,
                        fee: 0,
                        feePrice: _feeRate,
                        reserve: false);

                    if (maxAmountEstimation.Amount > 0)
                        _amount = maxAmountEstimation.Amount;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = maxAmountEstimation.Fee;
                    OnPropertyChanged(nameof(FeeString));
                }
                else // manual fee
                {
                    var availableInSatoshi = Outputs.Sum(o => o.Value);
                    var feeInSatoshi = Config.CoinToSatoshi(_fee);
                    var maxAmountInSatoshi = availableInSatoshi - feeInSatoshi;
                    var maxAmount = Config.SatoshiToCoin(maxAmountInSatoshi);

                    var account = App.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: _to,
                        amount: maxAmount,
                        fee: _fee,
                        account: account);

                    if (transactionParams == null)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;

                        _amount = 0;
                        OnPropertyChanged(nameof(AmountString));

                        return;
                    }

                    _amount = maxAmount;
                    OnPropertyChanged(nameof(AmountString));

                    FeeRate = transactionParams.FeeRate;
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
            var account = App.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

            return account.SendAsync(
                from: Outputs.ToList(),
                to: confirmationViewModel.To,
                amount: confirmationViewModel.Amount,
                fee: confirmationViewModel.Fee,
                dustUsagePolicy: DustUsagePolicy.AddToFee,
                cancellationToken: cancellationToken);
        }

        private void DesignerMode()
        {
            _feeRate = 200;
        }
    }
}