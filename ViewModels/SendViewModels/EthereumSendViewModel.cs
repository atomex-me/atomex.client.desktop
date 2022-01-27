using System;
using System.Globalization;
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
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
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

        [Reactive] public int GasLimit { get; set; }
        [Reactive] public int GasPrice { get; set; }
        [Reactive] private decimal TotalFee { get; set; }
        [ObservableAsProperty] public string TotalFeeString { get; set; }
        [ObservableAsProperty] public string GasPriceString { get; set; }
        protected override decimal FeeAmount => Currency.GetFeeAmount(GasLimit, GasPrice);

        public void SetGasPriceFromString(string value)
        {
            if (value == GasPriceString)
                return;

            var parsed = int.TryParse(value, out var gasPrice);
            {
                if (!parsed)
                    gasPrice = 0;

                GasPrice = gasPrice;

                Dispatcher.UIThread.InvokeAsync(() => this.RaisePropertyChanged(nameof(GasPriceString)));
            }
        }

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
                .Subscribe(_ => Warning = string.Empty);

            this.WhenAnyValue(vm => vm.GasPrice)
                .Select(gasPrice => gasPrice.ToString(CultureInfo.InvariantCulture))
                .ToPropertyEx(this, vm => vm.GasPriceString);

            this.WhenAnyValue(vm => vm.GasPrice)
                .Where(_ => !string.IsNullOrEmpty(From))
                .Select(_ => Unit.Default)
                .InvokeCommand(updateGasPriceCommand);

            this.WhenAnyValue(vm => vm.GasPrice)
                .Select(_ => Unit.Default)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(
                    vm => vm.GasLimit,
                    vm => vm.GasPrice
                )
                .Where(_ => !string.IsNullOrEmpty(From))
                .Subscribe(_ => TotalFee = FeeAmount);

            this.WhenAnyValue(vm => vm.TotalFee)
                .Select(totalFee => totalFee.ToString(TotalFeeCurrencyFormat, CultureInfo.InvariantCulture))
                .ToPropertyEx(this, vm => vm.TotalFeeString);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.TotalFee,
                    (amount, fee) => Currency.IsToken ? amount : amount + fee
                )
                .Select(totalAmount => totalAmount.ToString(CurrencyFormat, CultureInfo.InvariantCulture))
                .ToPropertyEx(this, vm => vm.TotalAmountString);

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
                var account = App.Account
                    .GetCurrencyAccount<EthereumAccount>(Currency.Name);

                var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                    from: From,
                    type: BlockchainTransactionType.Output,
                    gasLimit: UseDefaultFee ? 0 : GasLimit,
                    gasPrice: UseDefaultFee ? 0 : GasPrice,
                    reserve: false);

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Description;
                    return;
                }

                if (UseDefaultFee)
                {
                    GasLimit = decimal.ToInt32(Currency.GetDefaultFee());
                    GasPrice = decimal.ToInt32(Currency.GetFeePriceFromFeeAmount(maxAmountEstimation.Fee, GasLimit));
                }

                if (Amount > maxAmountEstimation.Amount)
                    Warning = Resources.CvInsufficientFunds;
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
                    var account = App.Account
                        .GetCurrencyAccount<EthereumAccount>(Currency.Name);

                    // estimate max amount with new GasPrice
                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        gasLimit: GasLimit,
                        gasPrice: GasPrice,
                        reserve: false);

                    if (maxAmountEstimation.Error != null)
                    {
                        Warning = maxAmountEstimation.Error.Description;
                        return;
                    }

                    if (Amount > maxAmountEstimation.Amount)
                        Warning = Resources.CvInsufficientFunds;
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
                var account = App.Account
                    .GetCurrencyAccount<EthereumAccount>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: BlockchainTransactionType.Output,
                        gasLimit: UseDefaultFee ? 0 : GasLimit,
                        gasPrice: UseDefaultFee ? 0 : GasPrice,
                        reserve: false);

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Description;
                    Amount = 0;
                    return;
                }

                if (UseDefaultFee)
                {
                    GasLimit = decimal.ToInt32(Currency.GetDefaultFee());
                    GasPrice = decimal.ToInt32(Currency.GetFeePriceFromFeeAmount(maxAmountEstimation.Fee, GasLimit));
                }

                Amount = maxAmountEstimation.Amount > 0
                    ? maxAmountEstimation.Amount
                    : 0;
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase    = FeeAmount * (quote?.Bid ?? 0m);
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = App.Account
                .GetCurrencyAccount<EthereumAccount>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: Amount,
                gasLimit: GasLimit,
                gasPrice: GasPrice,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }
    }
}