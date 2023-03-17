using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Controls;
using ReactiveUI;
using Serilog;

using Atomex.Blockchain.Bitcoin;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Entities;
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
        public IFromSource FromSource { get; set; }
        public string? FromAddressDescription
        {
            get
            {
                if (FromSource is FromAddress fromAddress)
                    return fromAddress.Address.TruncateAddress();

                if (FromSource is FromOutputs fromOutputs)
                {
                    if (fromOutputs.Outputs.Count() > 1)
                        return $"{fromOutputs.Outputs.Count()} outputs";

                    var network = (FromCurrencyViewModel?.Currency as BitcoinBasedConfig)?.Network;

                    if (network != null)
                        return fromOutputs.Outputs
                            .First()
                            .DestinationAddress(network)
                            .TruncateAddress();
                }

                return null;
            }
        }

        public string ToAddress { get; set; }
        public string? ToAddressDescription => ToAddress?.TruncateAddress();
        public string? RedeemFromAddress { get; set; }

        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public string? BaseCurrencyCode { get; set; }
        public string? QuoteCurrencyCode { get; set; }
        public string PriceFormat { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetAmountInBase { get; set; }
        public decimal EstimatedPrice { get; set; }
        public decimal EstimatedOrderPrice { get; init; }
        public decimal EstimatedMakerNetworkFee { get; init; }
        public decimal EstimatedTotalNetworkFeeInBase { get; set; }

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
            if (Design.IsDesignMode)
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
                App.DialogService.Show(
                    MessageViewModel.Message(title: "Sending, please wait", withProgressBar: true));

                var error = await ConvertAsync();

                if (error != null)
                {
                    if (error.Value.Code == Errors.PriceHasChanged)
                    {
                        App.DialogService.Show(MessageViewModel.Message(
                            title: Resources.SvFailed,
                            text: error.Value.Message,
                            backAction: () => App.DialogService.Show(this)));
                    }
                    else
                    {
                        App.DialogService.Show(MessageViewModel.Error(
                            text: error.Value.Message,
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

                var baseCurrency = account.Currencies.GetByName(symbol.Base);
                var side         = symbol.OrderSideForBuyCurrency(ToCurrencyViewModel.Currency);
                var atomexClient = _app.AtomexClient;
                var price        = EstimatedPrice;
                var orderPrice   = EstimatedOrderPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, Resources.CvNoLiquidity);

                var qty = AmountHelper.AmountToSellQty(side, Amount, price, baseCurrency.Precision);

                if (qty < symbol.MinimumQty)
                {
                    var minimumAmount = AmountHelper.QtyToSellAmount(
                        side: side,
                        qty: symbol.MinimumQty,
                        price: price,
                        precision: FromCurrencyViewModel.Currency.Precision);

                    var message = string.Format(
                        provider: CultureInfo.InvariantCulture,
                        format: Resources.CvMinimumAllowedQtyWarning,
                        arg0: minimumAmount,
                        arg1: FromCurrencyViewModel.Currency.Name);

                    return new Error(Errors.SwapError, message);
                }

                var isToBitcoinBased = Currencies.IsBitcoinBased(ToCurrencyViewModel.Currency.Name);

                var order = new Order
                {
                    ClientOrderId   = Guid.NewGuid().ToByteArray().ToHexString(0, 16),
                    Status          = OrderStatus.Pending,
                    Symbol          = symbol.Name,
                    TimeStamp       = DateTime.UtcNow,
                    Price           = orderPrice,
                    Qty             = qty,
                    LeaveQty        = qty,
                    Side            = side,
                    Type            = OrderType.FillOrKill,
                    MakerNetworkFee = EstimatedMakerNetworkFee,

                    FromAddress = FromSource is FromAddress fromAddress ? fromAddress.Address : null,
                    FromOutputs = FromSource is FromOutputs fromOutputs
                        ? fromOutputs.Outputs
                            .Select(o => new BitcoinTxPoint { Hash = o.TxId, Index = o.Index })
                            .ToList()
                        : null,

                    // for Bitcoin based currencies ToAddress must be Atomex wallet's address!
                    ToAddress = isToBitcoinBased
                        ? RedeemFromAddress
                        : ToAddress,
                    RedeemFromAddress = isToBitcoinBased
                        ? ToAddress
                        : RedeemFromAddress
                };

                await _app
                    .LocalStorage
                    .UpsertOrderAsync(order);

                var fromWalletsWithProofs = await fromWallets
                    .CreateProofOfPossessionAsync(
                        timeStamp: order.TimeStamp,
                        account: account);

                if (fromWalletsWithProofs == null)
                    return new Error(Errors.SwapError, "Can't create proofs for used wallets");

                foreach (var fromWalletWithProof in fromWalletsWithProofs)
                    if (fromWalletWithProof.Currency != FromCurrencyViewModel.Currency.Name)
                        fromWalletWithProof.Currency = FromCurrencyViewModel.Currency.Name;

                atomexClient.OrderSendAsync(new V1.Entities.Order
                {
                    ClientOrderId         = order.ClientOrderId,
                    Symbol                = order.Symbol,
                    TimeStamp             = order.TimeStamp,
                    Price                 = order.Price,
                    Qty                   = order.Qty,
                    Side                  = order.Side,
                    Type                  = order.Type,
                    FromWallets           = fromWalletsWithProofs.ToList(),
                    BaseCurrencyContract  = GetSwapContract(order.Symbol.BaseCurrency()),
                    QuoteCurrencyContract = GetSwapContract(order.Symbol.QuoteCurrency())
                });

                // wait for swap confirmation
                var timeStamp = DateTime.UtcNow;

                while (DateTime.UtcNow < timeStamp + SwapTimeout)
                {
                    await Task.Delay(SwapCheckInterval);

                    var currentOrder = _app
                        .LocalStorage
                        .GetOrderById(order.ClientOrderId);

                    if (currentOrder == null)
                        continue;

                    if (currentOrder.Status == OrderStatus.Pending)
                        continue;

                    if (currentOrder.Status == OrderStatus.PartiallyFilled || currentOrder.Status == OrderStatus.Filled)
                    {
                        var swap = (await _app
                            .LocalStorage
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

        private string? GetSwapContract(string currency)
        {
            if (currency == "ETH" || Currencies.IsPresetEthereumToken(currency))
                return _app.Account.Currencies.Get<EthereumConfig>(currency).SwapContractAddress;

            if (currency == "XTZ" || Currencies.IsPresetTezosToken(currency))
                return _app.Account.Currencies.Get<TezosConfig>(currency).SwapContractAddress;

            return null;
        }

        private async Task<IEnumerable<WalletAddress>> GetFromAddressesAsync()
        {
            if (FromSource is FromAddress fromAddress)
            {
                var walletAddress = await _app
                    .Account
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

#if DEBUG
        private void DesignerMode()
        {
            var btc = DesignTime.TestNetCurrencies.Get<BitcoinConfig>("BTC");
            var ltc = DesignTime.TestNetCurrencies.Get<LitecoinConfig>("LTC");

            FromCurrencyViewModel = CurrencyViewModelCreator.CreateOrGet(btc, subscribeToUpdates: false);
            ToCurrencyViewModel   = CurrencyViewModelCreator.CreateOrGet(ltc, subscribeToUpdates: false);

            FromSource            = new FromAddress("13V2gzjUL9DiHZLy1WFk9q6pZ3yBsb4TzP");
            ToAddress             = "13V2gzjUL9DiHZLy1WFk9q6pZ3yBsb4TzP";

            BaseCurrencyCode      = "LTC";
            QuoteCurrencyCode     = "BTC";
            PriceFormat           = $"F{FromCurrencyViewModel.Currency.Decimals}";
            EstimatedPrice        = 0.003m;

            Amount                = 0.00001234m;
            AmountInBase          = 10.23m;
            TargetAmount          = Amount / EstimatedPrice;
            TargetAmountInBase    = AmountInBase;

            EstimatedTotalNetworkFeeInBase = 23.43m;
        }
#endif
    }
}