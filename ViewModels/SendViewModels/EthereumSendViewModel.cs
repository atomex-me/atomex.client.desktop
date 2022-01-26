using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Ethereum;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        public string TotalFeeCurrencyFormat => CurrencyViewModel.FeeCurrencyFormat;
        public string TotalFeeCurrencyCode => CurrencyCode;
        public static string GasPriceCode => "GWEI";
        public static string GasLimitCode => "GAS";

        [Reactive] public int GasLimit { get; set; }
        [Reactive] public int GasPrice { get; set; }
        [Reactive] public decimal TotalFee { get; set; }
        [ObservableAsProperty] public string TotalFeeString { get; set; }
        [ObservableAsProperty] public string GasPriceString { get; set; }
        protected override decimal FeeAmount => Currency.GetFeeAmount(GasLimit, GasPrice);

        public void SetGasPriceFromString(string value)
        {
            if (value == GasPriceString) return;
            var parsed = int.TryParse(value, out var gasPrice);
            {
                if (!parsed) gasPrice = 0;
                GasPrice = gasPrice;
                Dispatcher.UIThread.InvokeAsync(() => this.RaisePropertyChanged(nameof(GasPriceString)));
            }
        }

        public EthereumSendViewModel()
        {
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
                    (amount, fee) => amount + fee
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
                if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?

                var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                    from: new FromAddress(From),
                    to: To,
                    type: BlockchainTransactionType.Output,
                    fee: UseDefaultFee ? 0 : GasLimit,
                    feePrice: UseDefaultFee ? 0 : GasPrice,
                    reserve: false);

                if (UseDefaultFee)
                {
                    GasLimit = decimal.ToInt32(Currency.GetDefaultFee());
                    GasPrice = decimal.ToInt32(await Currency.GetDefaultFeePriceAsync());

                    if (Amount > maxAmountEstimation.Amount)
                        Warning = Resources.CvInsufficientFunds;
                }
                else
                {
                    if (Amount > maxAmountEstimation.Amount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    if (GasLimit < Currency.GetDefaultFee() || GasPrice == 0)
                        Warning = Resources.CvLowFees;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update amount error", Currency?.Description);
            }
        }

        private async Task UpdateGasPrice()
        {
            try
            {
                if (Amount == 0)
                {
                    if (FeeAmount > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;

                    return;
                }

                if (GasPrice == 0)
                {
                    Warning = Resources.CvLowFees;
                    return;
                }

                if (!UseDefaultFee)
                {
                    if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                        return; // todo: error?

                    var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
                        from: new FromAddress(From),
                        to: To,
                        type: BlockchainTransactionType.Output,
                        fee: GasLimit,
                        feePrice: GasPrice,
                        reserve: false);

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
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (availableAmount == 0)
                    return;

                if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
                    return; // todo: error?

                if (UseDefaultFee)
                {
                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: new FromAddress(From),
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: false);

                    Amount = maxAmountEstimation.Amount > 0 ? maxAmountEstimation.Amount : 0;

                    GasLimit = decimal.ToInt32(Currency.GetDefaultFee());
                    GasPrice = decimal.ToInt32(await Currency.GetDefaultFeePriceAsync());
                }
                else
                {
                    if (GasLimit < Currency.GetDefaultFee() || GasPrice == 0)
                    {
                        Warning = Resources.CvLowFees;
                        if (GasLimit == 0 || GasPrice == 0)
                        {
                            Amount = 0;
                            return;
                        }
                    }

                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: new FromAddress(From),
                            to: To,
                            type: BlockchainTransactionType.Output,
                            fee: GasLimit,
                            feePrice: GasPrice,
                            reserve: false);

                    if (maxAmountEstimation.Error != null)
                    {
                        Warning = maxAmountEstimation.Error.Description;
                        Amount = 0;
                        return;
                    }

                    Amount = maxAmountEstimation.Amount;

                    if (maxAmountEstimation.Amount == 0 && availableAmount > 0)
                        Warning = Resources.CvInsufficientFunds;
                }
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
            FeeInBase = FeeAmount * (quote?.Bid ?? 0m);
        }

        protected override Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var account = App.Account.GetCurrencyAccount<EthereumAccount>(Currency.Name);

            return account.SendAsync(
                from: From,
                to: To,
                amount: Amount,
                gasLimit: GasLimit,
                gasPrice: GasPrice,
                useDefaultFee: UseDefaultFee,
                cancellationToken: cancellationToken);
        }

        protected override async Task UpdateFee()
        {
            return;
        }
    }
}