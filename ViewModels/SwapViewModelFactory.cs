using System;

using Avalonia.Media;
using Serilog;

using Atomex.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels
{
    public static class SwapViewModelFactory
    {
        public static SwapViewModel? CreateSwapViewModel(
            Swap swap,
            ICurrencies currencies,
            Action? onCloseSwap = null)
        {
            try
            {
                var soldCurrency = currencies.GetByName(swap.SoldCurrency);
                var purchasedCurrency = currencies.GetByName(swap.PurchasedCurrency);

                var fromCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(
                    currencyConfig: soldCurrency,
                    subscribeToUpdates: false);

                var toCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(
                    currencyConfig: purchasedCurrency,
                    subscribeToUpdates: false);

                var fromAmount = AmountHelper.QtyToSellAmount(swap.Side, swap.Qty, swap.Price, soldCurrency.DigitsMultiplier);
                var toAmount = AmountHelper.QtyToSellAmount(swap.Side.Opposite(), swap.Qty, swap.Price,
                    purchasedCurrency.DigitsMultiplier);

                var quoteCurrency = swap.Symbol.QuoteCurrency() == swap.SoldCurrency
                    ? soldCurrency
                    : purchasedCurrency;

                var compactState = CompactStateBySwap(swap);

                var detailsViewModel = new SwapDetailsViewModel
                {
                    DetailingInfo         = Atomex.ViewModels.Helpers.GetSwapDetailingInfo(swap, currencies),
                    CompactState          = compactState,
                    SwapId                = swap.Id.ToString(),
                    Price                 = swap.Price,
                    TimeStamp             = swap.TimeStamp.ToLocalTime(),
                    FromCurrencyViewModel = fromCurrencyViewModel,
                    ToCurrencyViewModel   = toCurrencyViewModel,
                    FromAmount            = fromAmount,
                    ToAmount              = toAmount,
                    OnClose               = onCloseSwap
                };

                return new SwapViewModel
                {
                    Id                    = swap.Id.ToString(),
                    CompactState          = compactState,
                    Mode                  = ModeBySwap(swap),
                    Time                  = swap.TimeStamp,

                    FromCurrencyViewModel = fromCurrencyViewModel,
                    FromAmount            = fromAmount,
                    FromAmountFormat      = fromCurrencyViewModel.CurrencyFormat,

                    ToCurrencyViewModel   = toCurrencyViewModel,
                    ToAmount              = toAmount,
                    ToAmountFormat        = toCurrencyViewModel.CurrencyFormat,

                    Price                 = swap.Price,
                    PriceFormat           = $"F{quoteCurrency.Digits}",

                    Details               = detailsViewModel
                };
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error while create SwapViewModel for {swap.Symbol} swap with id {swap.Id}");

                return null;
            }
        }

        private static SwapMode ModeBySwap(Swap swap)
        {
            return swap.IsInitiator
                ? SwapMode.Initiator
                : SwapMode.CounterParty;
        }

        private static SwapCompactState CompactStateBySwap(Swap swap)
        {
            if (swap.IsComplete)
                return SwapCompactState.Completed;

            if (swap.IsCanceled)
                return SwapCompactState.Canceled;

            if (swap.IsUnsettled)
                return SwapCompactState.Unsettled;

            if (swap.IsRefunded)
                return SwapCompactState.Refunded;

            return SwapCompactState.InProgress;
        }
    }
}