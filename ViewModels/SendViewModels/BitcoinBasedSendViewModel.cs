using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Network = NBitcoin.Network;

using Avalonia.Controls;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain.Bitcoin;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet.BitcoinBased;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        [Reactive] private ObservableCollection<BitcoinTxOutput> Outputs { get; set; }
        [Reactive] public decimal FeeRate { get; set; }

        public string FeeRateFormat => "0.#";

        private BitcoinBasedConfig Config => (BitcoinBasedConfig)Currency;
        private BitcoinBasedAccount Account => _app.Account.GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

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
                .Select(outputs => outputs.Count != 1
                    ? $"{outputs.Count} outputs"
                    : outputs.ElementAt(0)
                        .DestinationAddress(Config.Network)
                        .TruncateAddress())
                .ToPropertyExInMainThread(this, vm => vm.FromBeautified);

            this.WhenAnyValue(vm => vm.Outputs)
                .WhereNotNull()
                .SubscribeInMainThread(outputs =>
                {
                    From = outputs.Count != 1
                        ? $"{outputs.Count} outputs"
                        : outputs.ElementAt(0)
                            .DestinationAddress(Config.Network)
                            .TruncateAddress();

                    var totalOutputsSatoshi = outputs
                        .Aggregate((long)0, (sum, output) => sum + output.Value);

                    SelectedFromBalance = Config.SatoshiToCoin(totalOutputsSatoshi);
                });

            this.WhenAnyValue(vm => vm.FeeRate)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .Where(useDefaultFee => !useDefaultFee)
                .SubscribeInMainThread((useDefaultFee) => { _ = UpdateFee(); });

            var outputs = Account.GetAvailableOutputsAsync()
                .WaitForResult()
                .Select(output => (BitcoinTxOutput)output);

            Outputs = new ObservableCollection<BitcoinTxOutput>(outputs);

            SelectFromViewModel = new SelectOutputsViewModel(outputs
                .Select(o => new OutputViewModel
                {
                    Output = o,
                    Config = Config,
                    IsSelected = true
                }), Config, Account)
            {
                BackAction = () => { App.DialogService.Show(this); },
                ConfirmAction = ots =>
                {
                    Outputs = new ObservableCollection<BitcoinTxOutput>(ots);
                    App.DialogService.Show(SelectToViewModel);
                },
                Config = Config,
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
        }

        protected override void FromClick()
        {
            var outputs = Account.GetAvailableOutputsAsync()
                .WaitForResult()
                .Select(o => new OutputViewModel()
                {
                    Output = (BitcoinTxOutput)o,
                    Config = Config,
                    IsSelected = Outputs.Any(output => output.TxId == o.TxId && output.Index == o.Index)
                });

            SelectFromViewModel = new SelectOutputsViewModel(outputs, Config, Account)
            {
                BackAction = () => { App.DialogService.Show(this); },
                ConfirmAction = ots =>
                {
                    Outputs = new ObservableCollection<BitcoinTxOutput>(ots);
                    App.DialogService.Show(this);
                },
                Config = Config,
            };
            
            SelectFromViewModel.BackAction = () => App.DialogService.Show(this);
            App.DialogService.Show(SelectFromViewModel);
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
                if (string.IsNullOrEmpty(To) || !Config.IsValidAddress(To))
                {
                    Warning        = Resources.SvInvalidAddressError;
                    WarningToolTip = "";
                    WarningType    = MessageType.Error;
                    return;
                }

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
                        Warning        = Resources.CvInsufficientFunds;
                        WarningToolTip = "";
                        WarningType    = MessageType.Error;
                        return;
                    }

                    Fee = Config.SatoshiToCoin((long)transactionParams.FeeInSatoshi);
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
                        Warning        = Resources.CvInsufficientFunds;
                        WarningToolTip = "";
                        WarningType    = MessageType.Error;
                        return;
                    }

                    var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                    var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                    if (Fee < minimumFee)
                    {
                        Warning        = Resources.CvLowFees;
                        WarningToolTip = "";
                        WarningType    = MessageType.Error;
                    }

                    FeeRate = transactionParams.FeeRate;
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
                if (string.IsNullOrEmpty(To) || !Config.IsValidAddress(To))
                {
                    Warning        = Resources.SvInvalidAddressError;
                    WarningToolTip = "";
                    WarningType    = MessageType.Error;
                    return;
                }

                var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                    availableOutputs: Outputs,
                    to: To,
                    amount: Amount,
                    fee: Fee,
                    account: Account);

                if (transactionParams == null)
                {
                    Warning        = Resources.CvInsufficientFunds;
                    WarningToolTip = "";
                    WarningType    = MessageType.Error;
                    return;
                }

                var minimumFeeInSatoshi = Config.GetMinimumFee((int)transactionParams.Size);
                var minimumFee = Config.SatoshiToCoin(minimumFeeInSatoshi);

                if (Fee < minimumFee)
                {
                    Warning        = Resources.CvLowFees;
                    WarningToolTip = "";
                    WarningType    = MessageType.Error;
                }

                FeeRate = transactionParams.FeeRate;
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
                if (UseDefaultFee)
                {
                    if (Outputs.Count == 0)
                    {
                        Warning        = Resources.CvInsufficientFunds;
                        WarningToolTip = "";
                        WarningType    = MessageType.Error;
                        Amount         = 0;
                        return;
                    }

                    FeeRate = await Config.GetFeeRateAsync();

                    var maxAmountEstimation = await Account
                        .EstimateMaxAmountToSendAsync(
                            outputs: Outputs,
                            to: To,
                            fee: null,
                            feeRate: FeeRate);

                    if (maxAmountEstimation.Error != null)
                    {
                        Warning        = maxAmountEstimation.Error.Value.Message;
                        WarningToolTip = maxAmountEstimation.ErrorHint;
                        WarningType    = MessageType.Error;
                        Amount         = 0;
                        return;
                    }

                    if (maxAmountEstimation.Amount > 0)
                        Amount = Config.SatoshiToCoin(maxAmountEstimation.Amount);

                    Fee = Config.SatoshiToCoin(maxAmountEstimation.Fee);
                }
                else // manual fee
                {
                    var availableInSatoshi = Outputs.SumBigIntegers(o => o.Value);
                    var feeInSatoshi = Config.CoinToSatoshi(Fee);
                    var maxAmountInSatoshi = BigInteger.Max(availableInSatoshi - feeInSatoshi, 0);
                    var maxAmount = Config.SatoshiToCoin(maxAmountInSatoshi);

                    if (string.IsNullOrEmpty(To) || !Config.IsValidAddress(To))
                    {
                        Warning        = Resources.SvInvalidAddressError;
                        WarningToolTip = "";
                        WarningType    = MessageType.Error;
                        Amount         = 0;
                        return;
                    }

                    var transactionParams = await BitcoinTransactionParams.SelectTransactionParamsByFeeAsync(
                        availableOutputs: Outputs,
                        to: To,
                        amount: maxAmount,
                        fee: Fee,
                        account: Account);

                    if (transactionParams == null)
                    {
                        Warning        = Resources.CvInsufficientFunds;
                        WarningToolTip = "";
                        WarningType    = MessageType.Error;
                        Amount         = 0;
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
                        {
                            Warning        = Resources.CvLowFees;
                            WarningToolTip = "";
                            WarningType    = MessageType.Error;
                        }
                    }

                    FeeRate = transactionParams.FeeRate;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        protected override async Task<Error?> Send(CancellationToken cancellationToken = default)
        {
            var (_, error) = await Account.SendAsync(
                    from: Outputs,
                    to: To,
                    amount: AmountToSend,
                    fee: Fee,
                    dustUsagePolicy: DustUsagePolicy.AddToFee,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return error;
        }

#if DEBUG
        private void BitcoinBasedDesignerMode()
        {
            FeeRate = 98765;
            Warning = "Insufficient BTC balance";

            var amount = new Money((decimal)0.0001, MoneyUnit.Satoshi);
            var script = BitcoinAddress.Create("muRDku2ZwNTz2msCZCHSUhDD5o6NxGsoXM", Network.TestNet).ScriptPubKey;

            var outputs = new List<BitcoinTxOutput>
            {
                new BitcoinTxOutput
                {
                    Coin = new Coin(
                        fromTxHash: new uint256("19aa2187cda7610590d09dfab41ed4720f8570d7414b71b3dc677e237f72d4a1"),
                        fromOutputIndex: 0u,
                        amount: amount,
                        scriptPubKey: script),
                    IsConfirmed = true,
                    Currency = "BTC"
                }
            };

            Outputs = new ObservableCollection<BitcoinTxOutput>(outputs);
            Stage = SendStage.Edit;
            AmountInBase = 1233123.34m;
        }
#endif
    }
}