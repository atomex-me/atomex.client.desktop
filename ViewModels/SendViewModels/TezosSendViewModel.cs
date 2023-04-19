using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using Netezos.Forging.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using Atomex.Wallets.Abstract;
using Netezos.Forging;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        private const int MinimumAmount = 1;
        private const int MaxGasLimit = 1_000_000;
        private const int MaxStorageLimit = 5000;

        [Reactive] public bool HasTokens { get; set; }
        [Reactive] public bool HasActiveSwaps { get; set; }
        [Reactive] public long GasLimit { get; set; }
        [Reactive] public long StorageLimit { get; set; }       
        [Reactive] public string Entrypoint { get; set; }
        [Reactive] public string Parameters { get; set; }

        private class EstimatedFees
        {
            public decimal Fee { get; set; }
            public long GasLimit { get; set; }
            public long StorageLimit { get; set; }
        }

        private EstimatedFees? _estimatedFees;
        private TezosConfig Config => (TezosConfig)Currency;
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

            CheckAmountCommand
                .Throttle(TimeSpan.FromMilliseconds(1))
                .SubscribeInMainThread(estimation => CheckAmount(estimation));

            SelectFromViewModel = new SelectAddressViewModel(
                _app.Account,
                _app.LocalStorage,
                Currency,
                SelectAddressMode.SendFrom)
            {
                BackAction = () => { App.DialogService.Show(this); },
                ConfirmAction = walletAddressViewModel =>
                {
                    From = walletAddressViewModel.Address;
                    SelectedFromBalance = walletAddressViewModel.AvailableBalance;
                    App.DialogService.Show(SelectToViewModel);
                }
            };

            SelectToViewModel = new SelectAddressViewModel(
                _app.Account,
                _app.LocalStorage,
                Currency)
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
                .Where(s => s.IsActive && (s.SoldCurrency == Currency.Name || s.PurchasedCurrency == Currency.Name))
                .ToList();

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

        private async Task<EstimatedFees?> EstimateFeesAsync(decimal amount, CancellationToken cancellationToken = default)
        {
            try
            {
                Log.Debug("EstimateFeesAsync call");

                if (From == null || To == null)
                    return null;

                var account = _app.Account
                    .GetCurrencyAccount<TezosAccount>(Currency.Name);

                var (request, error) = await account
                    .FillOperationsAsync(new List<TezosOperationParameters>
                    {
                        new TezosOperationParameters
                        {
                            Content = new TransactionContent
                            {
                                Amount = amount != 0
                                    ? amount.ToMicroTez()
                                    : Parameters == null
                                        ? MinimumAmount // required non zero amount for transfers
                                        : 0,            // allow zero amount for contracts calls
                                GasLimit = MaxGasLimit,
                                StorageLimit = MaxStorageLimit,
                                Destination = To,
                                Source = From,
                                Fee = 0
                            },
                            UseFeeFromNetwork = true,
                            UseGasLimitFromNetwork = true,
                            UseStorageLimitFromNetwork = true,
                        }
                    }, cancellationToken)
                    .ConfigureAwait(false);

                if (error != null || request == null || !request.IsAutoFilled)
                    return null;

                return new EstimatedFees
                {
                    Fee = request.TotalFee().ToTez() +
                        (request.TotalStorageLimit() * Config.StorageFeeMultiplier).ToTez(),
                    GasLimit = request.TotalGasLimit(),
                    StorageLimit = request.TotalStorageLimit(),
                };
            }
            catch
            {
                return null;
            }
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                _estimatedFees = await EstimateFeesAsync(Amount);

                if (UseDefaultFee)
                {
                    if (_estimatedFees == null)
                    {
                        Warning        = Resources.SvCantEstimateFees;
                        WarningToolTip = "";
                        WarningType    = MessageType.Warning;

                        var isAllocated = await _app.Account
                            .GetCurrencyAccount<TezosAccount>(TezosHelper.Xtz)
                            .IsAllocatedDestinationAsync(To);

                        StorageLimit = !isAllocated ? Config.ActivationStorage : 0;
                        Fee = Config.Fee.ToTez() +
                            (StorageLimit * Config.StorageFeeMultiplier).ToTez();
                        GasLimit = Config.GasLimit;
                    }
                    else
                    {
                        Fee          = _estimatedFees.Fee;
                        GasLimit     = _estimatedFees.GasLimit;
                        StorageLimit = _estimatedFees.StorageLimit;
                    }
                }

                var maxAmountEstimation = await _app
                    .Account
                    .GetCurrencyAccount<TezosAccount>(Currency.Name)
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        fee: Fee.ToMicroTez(),
                        reserve: false);

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
                if (!UseDefaultFee) // manual fee
                {
                    var maxAmountEstimation = await _app
                        .Account
                        .GetCurrencyAccount<TezosAccount>(Currency.Name)
                        .EstimateMaxAmountToSendAsync(
                            from: From,
                            fee: Fee.ToMicroTez(),
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
                _estimatedFees = await EstimateFeesAsync(amount: 0);

                if (_estimatedFees != null)
                {
                    var fromAddress = await _app.Account
                        .GetAddressAsync(TezosHelper.Xtz, From);

                    var sizeDiff = LocalForge.ForgeMicheNat(fromAddress.Balance).Length - LocalForge.ForgeMicheNat(1).Length;

                    var feeDiff = Math.Max((long)(sizeDiff * Config.GetFillOperationSettings().MinimalNanotezPerByte), 0L);

                    _estimatedFees.Fee += feeDiff.ToTez();

                    if (UseDefaultFee)
                    {
                        Warning      = "";
                        Fee          = _estimatedFees.Fee;
                        GasLimit     = _estimatedFees.GasLimit;
                        StorageLimit = _estimatedFees.StorageLimit;
                    }
                }
                else if (UseDefaultFee)
                {
                    Warning        = Resources.SvCantEstimateFees;
                    WarningToolTip = "";
                    WarningType    = MessageType.Warning;

                    var isAllocated = await _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosHelper.Xtz)
                        .IsAllocatedDestinationAsync(To);

                    StorageLimit = !isAllocated ? Config.ActivationStorage : 0;
                    Fee = Config.Fee.ToTez() +
                        (StorageLimit * Config.StorageFeeMultiplier).ToTez();
                    GasLimit = Config.GasLimit;
                }

                var maxAmountEstimation = await _app
                    .Account
                    .GetCurrencyAccount<TezosAccount>(Currency.Name)
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        fee: Fee.ToMicroTez(),
                        reserve: false);

                if (maxAmountEstimation.Error != null)
                {
                    Warning        = maxAmountEstimation.Error.Value.Message;
                    WarningToolTip = maxAmountEstimation.ErrorHint;
                    WarningType    = MessageType.Error;
                    Amount = 0;
                    return;
                }

                var (fa12TransferFee, _) = await _app
                    .Account
                    .GetCurrencyAccount<Fa12Account>("TZBTC")
                    .EstimateTransferFeeAsync(From);

                var maxAmount = maxAmountEstimation.Amount.ToTez();

                RecommendedMaxAmount = HasActiveSwaps
                    ? Math.Max(maxAmount - maxAmountEstimation.Reserved.ToTez(), 0)
                    : HasTokens
                        ? Math.Max(maxAmount - fa12TransferFee.ToTez(), 0)
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
                Warning        = maxAmountEstimation.Error.Value.Message;
                WarningToolTip = maxAmountEstimation.ErrorHint;
                WarningType    = MessageType.Error;
                return;
            }

            if (_estimatedFees != null && Fee < _estimatedFees.Fee) // less than minimum estimated fee
            {
                Warning        = Resources.CvLowFees;
                WarningToolTip = "";
                WarningType    = MessageType.Error;
                return;
            }

            var maxAmount = maxAmountEstimation.Amount.ToTez();

            if (Amount > maxAmount)
            {
                Warning        = Resources.CvInsufficientFunds;
                WarningToolTip = "";
                WarningType    = MessageType.Error;
                return;
            }

            var (fa12TransferFee, _) = await _app.Account
                .GetCurrencyAccount<Fa12Account>("TZBTC")
                .EstimateTransferFeeAsync(From);

            RecommendedMaxAmount = HasActiveSwaps
                ? Math.Max(maxAmount - maxAmountEstimation.Reserved.ToTez(), 0)
                : HasTokens
                    ? Math.Max(maxAmount - fa12TransferFee.ToTez(), 0)
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

        protected override async Task<Error?> Send(CancellationToken cancellationToken = default)
        {
            var account = _app.Account
                .GetCurrencyAccount<TezosAccount>(Currency.Name);

            var (_, error) = await account
                .SendTransactionAsync(
                    from: From,
                    to: To,
                    amount: AmountToSend.ToMicroTez(),
                    fee: Blockchain.Tezos.Fee.FromValue(Fee.ToMicroTez()),
                    gasLimit: Blockchain.Tezos.GasLimit.FromValue((int)GasLimit),
                    storageLimit: Blockchain.Tezos.StorageLimit.FromValue((int)StorageLimit),
                    entrypoint: Entrypoint,
                    parameters: Parameters,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return error;
        }
    }
}