using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        // TODO: select outputs from UI
        [Reactive] public ObservableCollection<BitcoinBasedTxOutput>? Outputs { get; set; }

        [Reactive] public decimal FeeRate { get; set; }

        private BitcoinBasedConfig Config => (BitcoinBasedConfig)Currency;

        private BitcoinBasedAccount Account => App.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

        private static SelectOutputsViewModel SelectOutputsViewModel { get; set; }

        public BitcoinBasedSendViewModel()
            : base()
        {
#if DEBUG
            if (Design.IsDesignMode)
                BitcoinBasedDesignerMode();
#endif
        }

        public BitcoinBasedSendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
            _ = Task.Run(async () =>
            {
                var outputs = (await Account.GetAvailableOutputsAsync())
                    .Select(o => (BitcoinBasedTxOutput)o);
                Outputs = new ObservableCollection<BitcoinBasedTxOutput>(outputs);

                SelectOutputsViewModel = new SelectOutputsViewModel()
                {
                    BackAction = () => { Desktop.App.DialogService.Show(this); }
                };
            });
        }

        protected override async void UpdateAmount(decimal amount)
        {
            if (IsAmountUpdating || Outputs == null || string.IsNullOrEmpty(_to)) return;

            IsAmountUpdating = true;

            _amount = amount;
            Warning = string.Empty;

            try
            {
                if (UseDefaultFee)
                {
                    FeeRate = await Config.GetFeeRateAsync();

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeRateAsync(
                        availableOutputs: Outputs,
                        to: _to,
                        amount: _amount,
                        feeRate: FeeRate,
                        account: Account);

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
                        account: Account);

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
            if (IsFeeUpdating || Outputs == null || string.IsNullOrEmpty(_to)) return;

            IsFeeUpdating = true;

            _fee = Math.Min(fee, Currency.GetMaximumFee());
            Warning = string.Empty;

            try
            {
                var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                    availableOutputs: Outputs,
                    to: _to,
                    amount: _amount,
                    fee: _fee,
                    account: Account);

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
                        feePrice: FeeRate,
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

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: _to,
                        amount: maxAmount,
                        fee: _fee,
                        account: Account);

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
            return Account.SendAsync(
                from: Outputs.ToList(),
                to: confirmationViewModel.To,
                amount: confirmationViewModel.Amount,
                fee: confirmationViewModel.Fee,
                dustUsagePolicy: DustUsagePolicy.AddToFee,
                cancellationToken: cancellationToken);
        }

        private ICommand _showOutputsWindowCommand;

        public ICommand ShowOutputsWindowCommand => _showOutputsWindowCommand ??=
            (_showOutputsWindowCommand = ReactiveCommand.Create(ShowOutputsWindow));

        private static void ShowOutputsWindow()
        {
            Desktop.App.DialogService.Show(SelectOutputsViewModel);
        }

        private void BitcoinBasedDesignerMode()
        {
            FeeRate = 98765;
            _warning = "Insufficient BTC balance";
        }
    }
}