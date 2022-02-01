using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using ReactiveUI;
using Serilog;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ConversionConfirmationViewModel : ViewModelBase
    {
        public event EventHandler OnSuccess;

        private static readonly TimeSpan SwapTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan SwapCheckInterval = TimeSpan.FromSeconds(3);

        private readonly IAtomexApp _app;
        public IFromSource FromSource { get; init; }
        public string ToAddress { get; init; }
        public string RedeemFromAddress { get; init; }

        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public string PriceFormat { get; set; }

        public decimal Amount { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetAmountInBase { get; set; }

        public decimal EstimatedPrice { get; set; }
        public decimal EstimatedOrderPrice { get; init; }
        public decimal EstimatedPaymentFee { get; set; }
        public decimal EstimatedPaymentFeeInBase { get; set; }
        public decimal EstimatedRedeemFee { get; set; }
        public decimal EstimatedRedeemFeeInBase { get; set; }
        public decimal EstimatedMakerNetworkFee { get; init; }
        public decimal EstimatedMakerNetworkFeeInBase { get; init; }
        public decimal EstimatedTotalNetworkFeeInBase { get; init; }

        public decimal RewardForRedeem { get; init; }
        public decimal RewardForRedeemInBase { get; init; }
        public bool HasRewardForRedeem { get; init; }

        private ICommand _backCommand;

        public ICommand BackCommand => _backCommand ??= ReactiveCommand.Create(() =>
        {
            App.DialogService.Close();
        });

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= ReactiveCommand.Create(Send);

#if DEBUG
        public ConversionConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public ConversionConfirmationViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        private async void Send()
        {
            try
            {
                App.DialogService.Show(new SendingViewModel());

                var error = await ConvertAsync();

                if (error != null)
                {
                    if (error.Code == Errors.PriceHasChanged)
                    {
                        App.DialogService.Show(MessageViewModel.Message(
                            title: Resources.SvFailed,
                            text: error.Description,
                            backAction: () => App.DialogService.Show(this)));
                    }
                    else
                    {
                        App.DialogService.Show(MessageViewModel.Error(
                            text: error.Description,
                            backAction: () => App.DialogService.Show(this)));
                    }

                    return;
                }
                
                App.DialogService.Show(MessageViewModel.Success(
                    text: Resources.SvOrderMatched,
                    nextAction: () => App.DialogService.Close()));

                OnSuccess?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                App.DialogService.Show(MessageViewModel.Error(
                    text: "An error has occurred while sending swap.",
                    backAction: () => App.DialogService.Show(this)));

                Log.Error(e, "Swap error.");
            }
        }

        private async Task<Error?> ConvertAsync()
        {
            try
            {
                var account = _app.Account;

                var fromWallets = await GetFromAddressesAsync();

                foreach (var fromWallet in fromWallets)
                    if (fromWallet.Currency != FromCurrencyViewModel.Currency.Name)
                        fromWallet.Currency = FromCurrencyViewModel.Currency.Name;

                // check balances
                var errors = await BalanceChecker.CheckBalancesAsync(_app.Account, fromWallets);

                if (errors.Any())
                    return new Error(Errors.SwapError, GetErrorsDescription(errors));

                if (Amount == 0)
                    return new Error(Errors.SwapError, Resources.CvZeroAmount);

                if (Amount > 0 && !fromWallets.Any())
                    return new Error(Errors.SwapError, Resources.CvInsufficientFunds);

                var symbol = _app.SymbolsProvider
                    .GetSymbols(_app.Account.Network)
                    .SymbolByCurrencies(FromCurrencyViewModel.Currency, ToCurrencyViewModel.Currency);

                var baseCurrency = _app.Account.Currencies.GetByName(symbol.Base);
                var side         = symbol.OrderSideForBuyCurrency(ToCurrencyViewModel.Currency);
                var terminal     = _app.Terminal;
                var price        = EstimatedPrice;
                var orderPrice   = EstimatedOrderPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, Resources.CvNoLiquidity);

                var qty = AmountHelper.AmountToSellQty(side, Amount, price, baseCurrency.DigitsMultiplier);

                if (qty < symbol.MinimumQty)
                {
                    var minimumAmount = AmountHelper.QtyToSellAmount(
                        side: side,
                        qty: symbol.MinimumQty,
                        price: price,
                        digitsMultiplier: FromCurrencyViewModel.Currency.DigitsMultiplier);

                    var message = string.Format(
                        provider: CultureInfo.InvariantCulture,
                        format: Resources.CvMinimumAllowedQtyWarning,
                        arg0: minimumAmount,
                        arg1: FromCurrencyViewModel.Currency.Name);

                    return new Error(Errors.SwapError, message);
                }

                var order = new Order
                {
                    Symbol            = symbol.Name,
                    TimeStamp         = DateTime.UtcNow,
                    Price             = orderPrice,
                    Qty               = qty,
                    Side              = side,
                    Type              = OrderType.FillOrKill,
                    FromWallets       = fromWallets.ToList(),
                    MakerNetworkFee   = EstimatedMakerNetworkFee,

                    FromAddress       = FromSource is FromAddress fromAddress ? fromAddress.Address : null,
                    FromOutputs       = FromSource is FromOutputs fromOutputs ? fromOutputs.Outputs.ToList() : null,
                    ToAddress         = ToAddress,
                    RedeemFromAddress = RedeemFromAddress
                };

                await order.CreateProofOfPossessionAsync(account);

                terminal.OrderSendAsync(order);

                // wait for swap confirmation
                var timeStamp = DateTime.UtcNow;

                while (DateTime.UtcNow < timeStamp + SwapTimeout)
                {
                    await Task.Delay(SwapCheckInterval);

                    var currentOrder = terminal.Account.GetOrderById(order.ClientOrderId);

                    if (currentOrder == null)
                        continue;

                    if (currentOrder.Status == OrderStatus.Pending)
                        continue;

                    if (currentOrder.Status == OrderStatus.PartiallyFilled || currentOrder.Status == OrderStatus.Filled)
                    {
                        var swap = (await terminal.Account
                            .GetSwapsAsync())
                            .FirstOrDefault(s => s.OrderId == currentOrder.Id);

                        if (swap == null)
                            continue;

                        return null;
                    }

                    if (currentOrder.Status == OrderStatus.Canceled)
                        return new Error(Errors.PriceHasChanged, Resources.SvPriceHasChanged);

                    if (currentOrder.Status == OrderStatus.Rejected)
                        return new Error(Errors.OrderRejected, Resources.SvOrderRejected);
                }

                return new Error(Errors.TimeoutReached, Resources.SvTimeoutReached);
            }
            catch (Exception e)
            {
                Log.Error(e, "Conversion error");

                return new Error(Errors.SwapError, Resources.CvConversionError);
            }
        }
        
        private async Task<IEnumerable<WalletAddress>> GetFromAddressesAsync()
        {
            if (FromSource is FromAddress fromAddress)
            {
                var walletAddress = await _app.Account
                    .GetAddressAsync(FromCurrencyViewModel.Currency.Name, fromAddress.Address);

                return new WalletAddress[] { walletAddress };
            }
            else if (FromSource is FromOutputs fromOutputs)
            {
                var config = (BitcoinBasedConfig)FromCurrencyViewModel.Currency;

                return await Task.WhenAll(fromOutputs.Outputs
                    .Select(o => o.DestinationAddress(config.Network))
                    .Distinct()
                    .Select(async a => await _app.Account.GetAddressAsync(FromCurrencyViewModel.Currency.Name, a)));
  
            }

            throw new NotSupportedException("Not supported type of From field");
        }

        private static string GetErrorsDescription(IEnumerable<BalanceError> errors)
        {
            var descriptions = errors.Select(e => e.Type switch
            {
                BalanceErrorType.FailedToGet => $"Balance check for address {e.Address} failed",
                BalanceErrorType.LessThanExpected => $"Balance for address {e.Address} is " +
                    $"{e.ActualBalance.ToString(CultureInfo.InvariantCulture)} and less than" +
                    $" local {e.LocalBalance.ToString(CultureInfo.InvariantCulture)}",
                _ => $"Balance for address {e.Address} has changed and needs to be updated"
            });

            return string.Join(". ", descriptions) + ". Please update your balance and try again!";
        }

        private void DesignerMode()
        {
            var currencies = DesignTime.Currencies.ToList();

            FromCurrencyViewModel     = CurrencyViewModelCreator.CreateViewModel(currencies[0], false);
            ToCurrencyViewModel       = CurrencyViewModelCreator.CreateViewModel(currencies[1], false);

            PriceFormat               = $"F{FromCurrencyViewModel.Currency.Digits}";
            EstimatedPrice            = 0.003m;

            Amount                    = 0.00001234m;
            AmountInBase              = 10.23m;

            TargetAmount              = Amount / EstimatedPrice;
            TargetAmountInBase        = AmountInBase;

            EstimatedPaymentFee       = 0.0001904m;
            EstimatedPaymentFeeInBase = 0.22m;

            EstimatedRedeemFee        = 0.001m;
            EstimatedRedeemFeeInBase  = 0.11m;
        }
    }
}