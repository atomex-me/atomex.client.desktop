using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;
using Avalonia.Controls;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Network = NBitcoin.Network;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        [Reactive] private ObservableCollection<BitcoinBasedTxOutput> Outputs { get; set; }

        [Reactive] public decimal FeeRate { get; set; }

        public string FeeRateFormat => "0.#";

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
            this.WhenAnyValue(vm => vm.Outputs)
                .WhereNotNull()
                .Subscribe(outputs =>
                {
                    From = outputs.Count != 1
                        ? $"{outputs.Count} outputs"
                        : GetShortenedAddress(outputs.ElementAt(0).DestinationAddress(Config.Network));

                    var totalOutputsSatoshi = outputs
                        .Aggregate((long)0, (sum, output) => sum + output.Value);

                    SelectedFromAmount = Config.SatoshiToCoin(totalOutputsSatoshi);

                    Warning = string.Empty;
                    _ = UpdateAmount(Amount);
                });

            _ = Task.Run(async () =>
            {
                var outputs = (await Account.GetAvailableOutputsAsync())
                    .Select(output => (BitcoinBasedTxOutput)output);

                Outputs = new ObservableCollection<BitcoinBasedTxOutput>(outputs);

                SelectOutputsViewModel = new SelectOutputsViewModel(outputs, Config, Account)
                {
                    BackAction = () => { Desktop.App.DialogService.Show(this); },
                    ConfirmAction = ots =>
                    {
                        Outputs = new ObservableCollection<BitcoinBasedTxOutput>(ots);
                        Desktop.App.DialogService.Show(this);
                    },
                    Config = Config,
                };

                // todo: remove
                To = "mptaZCY98rZTDh3dovtQUEdESmxTAcyNQr";
            });
        }

        protected override void FromClick()
        {
            Desktop.App.DialogService.Show(SelectOutputsViewModel);
        }

        protected override void ToClick()
        {
            SelectToViewModel ??= new SelectToViewModel(App.Account, Currency)
            {
                BackAction = () => { Desktop.App.DialogService.Show(this); },
                ConfirmAction = address =>
                {
                    Log.Information(address);
                    Desktop.App.DialogService.Show(this);
                }
            };
            Desktop.App.DialogService.Show(SelectToViewModel);
        }

        protected override async Task UpdateAmount(decimal amount)
        {
            try
            {
                if (UseDefaultFee)
                {
                    FeeRate = await Config.GetFeeRateAsync();

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeRateAsync(
                        availableOutputs: Outputs,
                        to: To,
                        amount: Amount,
                        feeRate: FeeRate,
                        account: Account);

                    if (transactionParams == null)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    var feeVal = Config.SatoshiToCoin((long)transactionParams.FeeInSatoshi);
                    Fee = feeVal;
                }
                else
                {
                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: To,
                        amount: Amount,
                        fee: Fee,
                        account: Account);

                    if (transactionParams == null)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                    var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                    if (Fee < minimumFee)
                        Warning = Resources.CvLowFees;

                    FeeRate = transactionParams.FeeRate;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch
            {
                // ignored
            }
        }

        protected override async Task UpdateFee(decimal fee)
        {
            try
            {
                var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                    availableOutputs: Outputs,
                    to: To,
                    amount: Amount,
                    fee: Fee,
                    account: Account);

                if (transactionParams == null)
                {
                    Warning = Resources.CvInsufficientFunds;
                    return;
                }

                var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                if (Fee < minimumFee)
                    Warning = Resources.CvLowFees;

                FeeRate = transactionParams.FeeRate;

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
                if (UseDefaultFee)
                {
                    if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?

                    if (Outputs.Count == 0)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        Amount = 0;
                        return;
                    }

                    FeeRate = await Config.GetFeeRateAsync();

                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromOutputs(Outputs),
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee: 0,
                        feePrice: FeeRate,
                        reserve: false);

                    if (maxAmountEstimation.Amount > 0)
                    {
                        Amount = maxAmountEstimation.Amount;
                        return;
                    }

                    Fee = maxAmountEstimation.Fee;
                }
                else // manual fee
                {
                    var availableInSatoshi = Outputs.Sum(o => o.Value);
                    var feeInSatoshi = Config.CoinToSatoshi(Fee);
                    var maxAmountInSatoshi = Math.Max(availableInSatoshi - feeInSatoshi, 0);
                    var maxAmount = Config.SatoshiToCoin(maxAmountInSatoshi);

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: To,
                        amount: maxAmount,
                        fee: Fee,
                        account: Account);

                    if (transactionParams == null)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        Amount = 0;
                        return;
                    }

                    if (Amount != maxAmount)
                    {
                        Amount = maxAmount;
                    }
                    else
                    {
                        var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                        var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                        if (Fee < minimumFee)
                            Warning = Resources.CvLowFees;
                    }

                    FeeRate = transactionParams.FeeRate;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            catch
            {
                // ignored
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

        private void BitcoinBasedDesignerMode()
        {
            FeeRate = 98765;
            Warning = "Insufficient BTC balance";

            var amount = new Money((decimal)0.0001, MoneyUnit.Satoshi);
            var script = BitcoinAddress.Create("muRDku2ZwNTz2msCZCHSUhDD5o6NxGsoXM", Network.TestNet).ScriptPubKey;

            var outputs = new List<BitcoinBasedTxOutput>
            {
                new BitcoinBasedTxOutput(
                    coin: new Coin(
                        fromTxHash: new uint256("19aa2187cda7610590d09dfab41ed4720f8570d7414b71b3dc677e237f72d4a1"),
                        fromOutputIndex: 0u,
                        amount: amount,
                        scriptPubKey: script),
                    spentTxPoint: null)
            };

            Outputs = new ObservableCollection<BitcoinBasedTxOutput>(outputs);
        }
    }
}