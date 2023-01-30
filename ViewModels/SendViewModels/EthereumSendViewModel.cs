using System;
using System.Globalization;
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
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Ethereum;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        private string TotalFeeCurrencyFormat => CurrencyViewModel.FeeCurrencyFormat;
        public virtual string TotalFeeCurrencyCode => CurrencyCode;
        public string GasPriceCode => "GWEI";
        public string GasLimitCode => "GAS";

        public int GasLimit => decimal.ToInt32(Currency.GetDefaultFee());
        [Reactive] public int GasPrice { get; set; }
        [Reactive] private decimal TotalFee { get; set; }
        [ObservableAsProperty] public string TotalFeeString { get; set; }
        protected override decimal FeeAmount => EthConfig.GetFeeInEth(GasLimit, GasPrice);
        [Reactive] public bool HasTokens { get; set; }
        [Reactive] public bool HasActiveSwaps { get; set; }
        public EthereumConfig EthConfig => (EthereumConfig)Currency;

        private ReactiveCommand<MaxAmountEstimation, MaxAmountEstimation> CheckAmountCommand;

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

            this.WhenAnyValue(vm => vm.GasPrice)
                .SubscribeInMainThread(_ => Warning = string.Empty);

            this.WhenAnyValue(vm => vm.GasPrice)
                .Where(_ => !string.IsNullOrEmpty(From))
                .Select(_ => Unit.Default)
                .InvokeCommandInMainThread(updateGasPriceCommand);

            this.WhenAnyValue(vm => vm.GasPrice)
                .Select(_ => Unit.Default)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.GasPrice)
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

                var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                    from: From,
                    type: TransactionType.Output,
                    gasLimit: UseDefaultFee ? null : GasLimit,
                    gasPrice: UseDefaultFee ? null : GasPrice,
                    reserve: false);

                var ethConfig = (EthereumConfig)Currency;

                if (UseDefaultFee)
                {
                    GasPrice = maxAmountEstimation.Fee > 0
                        ? decimal.ToInt32(ethConfig.GetGasPriceInGwei(maxAmountEstimation.Fee, GasLimit))
                        : decimal.ToInt32(await ethConfig.GetGasPriceAsync());
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
                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: From,
                        type: TransactionType.Output,
                        gasLimit: GasLimit,
                        gasPrice: GasPrice,
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

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: TransactionType.Output,
                        gasLimit: UseDefaultFee ? null : GasLimit,
                        gasPrice: UseDefaultFee ? null : GasPrice,
                        reserve: false);

                var ethConfig = (EthereumConfig)Currency;

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    GasPrice = decimal.ToInt32(ethConfig.GetGasPriceInGwei(maxAmountEstimation.Fee, GasLimit));

                if (maxAmountEstimation.Error != null)
                {
                    Warning        = maxAmountEstimation.Error.Message;
                    WarningToolTip = maxAmountEstimation.Error.Details;
                    WarningType    = MessageType.Error;
                    Amount         = 0;
                    return;
                }

                var erc20Config = _app.Account.Currencies.Get<Erc20Config>("USDT");
                var erc20TransferFee = erc20Config.GetFeeInEth(erc20Config.TransferGasLimit, GasPrice);

                RecommendedMaxAmount = HasActiveSwaps
                    ? Math.Max(maxAmountEstimation.Amount - maxAmountEstimation.Reserved, 0)
                    : HasTokens
                        ? Math.Max(maxAmountEstimation.Amount - erc20TransferFee, 0)
                        : maxAmountEstimation.Amount;

                // force to use RecommendedMaxAmount in case when there are active swaps
                Amount = maxAmountEstimation.Amount > 0
                    ? HasActiveSwaps
                        ? RecommendedMaxAmount
                        : maxAmountEstimation.Amount
                    : 0;

                CheckAmountCommand?.Execute(maxAmountEstimation).Subscribe();
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        private void CheckAmount(MaxAmountEstimation maxAmountEstimation)
        {
            if (maxAmountEstimation.Error != null)
            {
                Warning = maxAmountEstimation.Error.Message;
                WarningToolTip = maxAmountEstimation.Error.Details;
                WarningType = MessageType.Error;
                return;
            }

            if (Amount > maxAmountEstimation.Amount)
            {
                Warning = Resources.CvInsufficientFunds;
                WarningToolTip = "";
                WarningType = MessageType.Error;
                return;
            }

            var erc20Config = _app.Account.Currencies.Get<Erc20Config>("USDT");
            var erc20TransferFee = erc20Config.GetFeeInEth(erc20Config.TransferGasLimit, GasPrice);

            RecommendedMaxAmount = HasActiveSwaps
                ? Math.Max(maxAmountEstimation.Amount - maxAmountEstimation.Reserved, 0)
                : HasTokens
                    ? Math.Max(maxAmountEstimation.Amount - erc20TransferFee, 0)
                    : maxAmountEstimation.Amount;

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

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AmountInBase = Amount.SafeMultiply(quote?.Bid ?? 0m);
                FeeInBase = FeeAmount.SafeMultiply(quote?.Bid ?? 0m);
            });
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = _app.Account
                .GetCurrencyAccount<EthereumAccount>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: AmountToSend,
                gasLimit: GasLimit,
                gasPrice: GasPrice,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}