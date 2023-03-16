using System;
using System.Globalization;
using System.Numerics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Ethereum;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        private string TotalFeeCurrencyFormat => CurrencyViewModel.FeeCurrencyFormat;
        public virtual string TotalFeeCurrencyCode => CurrencyCode;
        public string GasPriceCode => "GWEI";
        public string GasLimitCode => "GAS";

        public virtual long GasLimit => EthConfig.GasLimit;
        [Reactive] public decimal MaxFeePerGas { get; set; }
        [Reactive] public decimal MaxPriorityFeePerGas { get; set; }
        [Reactive] public decimal BaseFeePerGas { get; set; }
        [Reactive] public decimal TotalFee { get; set; }
        [ObservableAsProperty] public string TotalFeeString { get; set; }
        [ObservableAsProperty] public decimal EstimatedFee { get; }
        [Reactive] public decimal EstimatedFeeInBase { get; set; }
        [ObservableAsProperty] public string EstimatedFeeString { get; set; }
        protected override decimal FeeAmount => EthConfig.GetFeeInEth(GasLimit, MaxFeePerGas);
        [Reactive] public bool HasTokens { get; set; }
        [Reactive] public bool HasActiveSwaps { get; set; }
        public EthereumConfig EthConfig => (EthereumConfig)Currency;

        private ReactiveCommand<EthereumMaxAmountEstimation, EthereumMaxAmountEstimation> CheckAmountCommand;

        public EthereumSendViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public EthereumSendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
            var updateGasPriceCommand = ReactiveCommand.CreateFromTask(UpdateGasPrice);

            this.WhenAnyValue(vm => vm.MaxFeePerGas)
                .SubscribeInMainThread(_ => Warning = string.Empty);

            this.WhenAnyValue(
                    vm => vm.MaxFeePerGas)
                .Where(_ => !string.IsNullOrEmpty(From))
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(updateGasPriceCommand);

            this.WhenAnyValue(
                    vm => vm.MaxFeePerGas)
                .Select(_ => Unit.Default)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.MaxFeePerGas)
                .Where(_ => !string.IsNullOrEmpty(From))
                .SubscribeInMainThread(_ => TotalFee = FeeAmount);

            this.WhenAnyValue(vm => vm.TotalFee)
                .Select(totalFee => totalFee.ToString(TotalFeeCurrencyFormat, CultureInfo.CurrentCulture))
                .ToPropertyExInMainThread(this, vm => vm.TotalFeeString);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.TotalFee,
                    (amount, fee) => Currency.IsToken ? amount : amount + fee)
                .Select(totalAmount => totalAmount.ToString(CurrencyFormat, CultureInfo.CurrentCulture))
                .ToPropertyExInMainThread(this, vm => vm.TotalAmountString);

            this.WhenAnyValue(
                    vm => vm.BaseFeePerGas,
                    vm => vm.MaxPriorityFeePerGas,
                    (baseFeePerGas, maxPriorityFeePerGas) => EthConfig.GetFeeInEth(GasLimit, baseFeePerGas + maxPriorityFeePerGas))
                .Select(estimatedFee => estimatedFee)
                .ToPropertyExInMainThread(this, vm => vm.EstimatedFee);

            this.WhenAnyValue(vm => vm.EstimatedFee)
                .Select(estimatedFee => estimatedFee.ToString(TotalFeeCurrencyFormat, CultureInfo.CurrentCulture))
                .ToPropertyExInMainThread(this, vm => vm.EstimatedFeeString);

            this.WhenAnyValue(vm => vm.EstimatedFee)
                .Select(_ => Unit.Default)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            CheckAmountCommand = ReactiveCommand.Create<EthereumMaxAmountEstimation, EthereumMaxAmountEstimation>(estimation => estimation);

            CheckAmountCommand.Throttle(TimeSpan.FromMilliseconds(1))
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

            if (Currency.Name == "ETH")
            {
                CheckTokensAsync();
                CheckActiveSwapsAsync();
            }
        }

        private async void CheckTokensAsync()
        {
            var account = _app.Account
                .GetCurrencyAccount<EthereumAccount>(Currency.Name);

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
                    .GetCurrencyAccount<EthereumAccount>(Currency.Name);

                var maxAmountEstimation = (EthereumMaxAmountEstimation)await account.EstimateMaxAmountToSendAsync(
                    from: From,
                    type: TransactionType.Output,
                    gasLimit: UseDefaultFee ? null : GasLimit,
                    maxFeePerGas: UseDefaultFee ? null : MaxFeePerGas,
                    reserve: false);

                var ethConfig = (EthereumConfig)Currency;

                var gasPrice = maxAmountEstimation.GasPrice;

                if (gasPrice == null)
                    (gasPrice, _) = await ethConfig.GetGasPriceAsync();

                if (gasPrice != null)
                {
                    if (UseDefaultFee)
                    {
                        MaxFeePerGas = gasPrice.MaxFeePerGas;
                        MaxPriorityFeePerGas = gasPrice.MaxPriorityFeePerGas;
                    }

                    BaseFeePerGas = gasPrice.SuggestBaseFee;
                }

                CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update amount error", Currency?.Description);
            }
        }

        protected virtual async Task UpdateGasPrice()
        {
            try
            {
                if (!UseDefaultFee)
                {
                    var account = _app.Account
                        .GetCurrencyAccount<EthereumAccount>(Currency.Name);

                    // estimate max amount with new GasPrice
                    var maxAmountEstimation = (EthereumMaxAmountEstimation)await account.EstimateMaxAmountToSendAsync(
                        from: From,
                        type: TransactionType.Output,
                        gasLimit: GasLimit,
                        maxFeePerGas: MaxFeePerGas,
                        reserve: false);

                    CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update gas price error", Currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount<EthereumAccount>(Currency.Name);

                var maxAmountEstimation = (EthereumMaxAmountEstimation)await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: TransactionType.Output,
                        gasLimit: UseDefaultFee ? null : GasLimit,
                        maxFeePerGas: UseDefaultFee ? null : MaxFeePerGas,
                        reserve: false);

                var ethConfig = (EthereumConfig)Currency;

                var gasPrice = maxAmountEstimation.GasPrice;

                //if (gasPrice == null)
                //    (gasPrice, _) = await ethConfig.GetGasPriceAsync();

                if (gasPrice != null)
                {
                    if (UseDefaultFee)
                    {
                        MaxFeePerGas = gasPrice.MaxFeePerGas;
                        MaxPriorityFeePerGas = gasPrice.MaxPriorityFeePerGas;
                    }

                    BaseFeePerGas = gasPrice.SuggestBaseFee;
                }

                if (maxAmountEstimation.Error != null)
                {
                    Warning        = maxAmountEstimation.Error.Value.Message;
                    WarningToolTip = maxAmountEstimation.ErrorHint;
                    WarningType    = MessageType.Error;
                    Amount         = 0;
                    return;
                }

                var erc20Config = _app.Account.Currencies.Get<Erc20Config>("USDT");
                var erc20TransferFee = erc20Config
                    .GetFeeInEth(erc20Config.TransferGasLimit, MaxFeePerGas)
                    .EthToWei();

                var recommendedMaxAmountInWei = HasActiveSwaps
                    ? BigInteger.Max(maxAmountEstimation.Amount - maxAmountEstimation.Reserved, 0)
                    : HasTokens
                        ? BigInteger.Max(maxAmountEstimation.Amount - erc20TransferFee, 0)
                        : maxAmountEstimation.Amount;

                RecommendedMaxAmount = recommendedMaxAmountInWei.WeiToEth();

                // force to use RecommendedMaxAmount in case when there are active swaps
                Amount = maxAmountEstimation.Amount > 0
                    ? HasActiveSwaps
                        ? RecommendedMaxAmount
                        : maxAmountEstimation.Amount.WeiToEth()
                    : 0;

                CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        private void CheckAmount(EthereumMaxAmountEstimation maxAmountEstimation)
        {
            if (maxAmountEstimation.Error != null)
            {
                Warning = maxAmountEstimation.Error.Value.Message;
                WarningToolTip = maxAmountEstimation.ErrorHint;
                WarningType = MessageType.Error;
                return;
            }

            if (Amount > maxAmountEstimation.Amount.WeiToEth())
            {
                Warning = Resources.CvInsufficientFunds;
                WarningToolTip = "";
                WarningType = MessageType.Error;
                return;
            }

            var erc20Config = _app.Account.Currencies.Get<Erc20Config>("USDT");
            var erc20TransferFee = erc20Config
                .GetFeeInEth(erc20Config.TransferGasLimit, MaxFeePerGas)
                .EthToWei();

            var recommendedMaxAmountInWei = HasActiveSwaps
                ? BigInteger.Max(maxAmountEstimation.Amount - maxAmountEstimation.Reserved, 0)
                : HasTokens
                    ? BigInteger.Max(maxAmountEstimation.Amount - erc20TransferFee, 0)
                    : maxAmountEstimation.Amount;

            RecommendedMaxAmount = recommendedMaxAmountInWei.WeiToEth();

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

        protected override void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AmountInBase = Amount.SafeMultiply(quote?.Bid ?? 0m);
                FeeInBase = FeeAmount.SafeMultiply(quote?.Bid ?? 0m);
                EstimatedFeeInBase = EstimatedFee.SafeMultiply(quote?.Bid ?? 0m);
            });
        }

        protected override async Task<Error?> Send(CancellationToken cancellationToken = default)
        {
            var account = _app.Account
                .GetCurrencyAccount<EthereumAccount>(Currency.Name);

            var (_, error) = await account
                .SendAsync(
                    from: From,
                    to: To,
                    amount: AmountToSend.EthToWei(),
                    gasLimit: GasLimit,
                    maxFeePerGas: MaxFeePerGas,
                    maxPriorityFeePerGas: MaxPriorityFeePerGas,
                    useDefaultFee: UseDefaultFee,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return error;
        }

#if DEBUG
        protected override void DesignerMode()
        {
            var fromCurrencies = DesignTime.TestNetCurrencies
                .Select(c => CurrencyViewModelCreator.CreateOrGet(c, subscribeToUpdates: false))
                .ToList();

            Currency = fromCurrencies.FirstOrDefault(c => c.CurrencyName == "ETH")!.Currency;
            CurrencyViewModel = fromCurrencies.FirstOrDefault(c => c.CurrencyName == "ETH")!;
            To = "0xE9C251cbB4881f9e056e40135E7d3EA9A7d037df ";
            Amount = 0.00001234m;
            AmountInBase = 10.23m;
            Fee = 0.0001m;
            FeeInBase = 8.43m;

            Warning = "Insufficient funds";
            WarningToolTip = "";
            WarningType = MessageType.Error;

            RecommendedMaxAmountWarning = "We recommend to send no more than 0.073 ETH";
            RecommendedMaxAmountWarningToolTip = "You have tokens that require ETH. Sending this will not leave enough ETH to send or exchange your tokens. We recommend to send no more than 0.073 ETH";
            RecommendedMaxAmountWarningType = MessageType.Warning;

            Stage = SendStage.Confirmation;
            SendRecommendedAmountMenu = string.Format(Resources.SendRecommendedAmountMenu, 0.073, "ETH");
            SendEnteredAmountMenu = string.Format(Resources.SendEnteredAmountMenu, 0.073, "ETH");

            UseDefaultFee = false;

            this.WhenAnyValue(vm => vm.TotalFee)
                .Select(totalFee => totalFee.ToString(TotalFeeCurrencyFormat, CultureInfo.CurrentCulture))
                .ToPropertyExInMainThread(this, vm => vm.TotalFeeString);

            TotalFee = 0.0001m;

            this.WhenAnyValue(
                    vm => vm.BaseFeePerGas,
                    vm => vm.MaxPriorityFeePerGas,
                    (baseFeePerGas, maxPriorityFeePerGas) => EthConfig.GetFeeInEth(GasLimit, baseFeePerGas + maxPriorityFeePerGas))
                .Select(estimatedFee => estimatedFee)
                .ToPropertyExInMainThread(this, vm => vm.EstimatedFee);

            this.WhenAnyValue(vm => vm.EstimatedFee)
                .Select(totalFee => totalFee.ToString(TotalFeeCurrencyFormat, CultureInfo.CurrentCulture))
                .ToPropertyExInMainThread(this, vm => vm.EstimatedFeeString);

            BaseFeePerGas = 9.2m;
            MaxFeePerGas = 10;
            MaxPriorityFeePerGas = 1;
            EstimatedFeeInBase = 7;
        }
#endif
    }
}