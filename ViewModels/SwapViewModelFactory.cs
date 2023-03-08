using System;

using Serilog;

using Atomex.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels
{
    public static class SwapViewModelFactory
    {
        public static SwapViewModel CreateSwapViewModel(
            Swap swap,
            ICurrencies currencies,
            Action? onCloseSwap = null)
        {
            var swapViewModel = new SwapViewModel
            {
                Details = new SwapDetailsViewModel
                {
                    OnClose = onCloseSwap
                }
            };

            Update(
                swapViewModel: swapViewModel,
                swap: swap,
                currencies: currencies);

            return swapViewModel;
        }

        public static void Update(SwapViewModel swapViewModel, Swap swap, ICurrencies currencies)
        {
            try
            {
                var soldCurrency = currencies.GetByName(swap.SoldCurrency);
                var purchasedCurrency = currencies.GetByName(swap.PurchasedCurrency);

                var fromCurrencyViewModel = CurrencyViewModelCreator.CreateOrGet(
                    currencyConfig: soldCurrency,
                    subscribeToUpdates: false);

                var toCurrencyViewModel = CurrencyViewModelCreator.CreateOrGet(
                    currencyConfig: purchasedCurrency,
                    subscribeToUpdates: false);

                var fromAmount = AmountHelper.QtyToSellAmount(swap.Side, swap.Qty, swap.Price, soldCurrency.Precision);
                var toAmount = AmountHelper.QtyToSellAmount(swap.Side.Opposite(), swap.Qty, swap.Price,
                    purchasedCurrency.Precision);

                var quoteCurrency = swap.Symbol.QuoteCurrency() == swap.SoldCurrency
                    ? soldCurrency
                    : purchasedCurrency;

                var compactState = CompactStateBySwap(swap);

                swapViewModel.Details.DetailingInfo = Atomex.ViewModels.Helpers.GetSwapDetailingInfo(swap, currencies);
                swapViewModel.Details.CompactState = compactState;
                swapViewModel.Details.SwapId = swap.Id.ToString();
                swapViewModel.Details.Price = swap.Price;
                swapViewModel.Details.TimeStamp = swap.TimeStamp.ToLocalTime();
                swapViewModel.Details.FromCurrencyViewModel = fromCurrencyViewModel;
                swapViewModel.Details.ToCurrencyViewModel = toCurrencyViewModel;
                swapViewModel.Details.FromAmount = fromAmount;
                swapViewModel.Details.ToAmount = toAmount;

                swapViewModel.Id = swap.Id.ToString();
                swapViewModel.CompactState = compactState;
                swapViewModel.Mode = ModeBySwap(swap);
                swapViewModel.Time = swap.TimeStamp;
                swapViewModel.FromCurrencyViewModel = fromCurrencyViewModel;
                swapViewModel.FromAmount = fromAmount;
                swapViewModel.FromAmountFormat = fromCurrencyViewModel.CurrencyFormat;
                swapViewModel.ToCurrencyViewModel = toCurrencyViewModel;
                swapViewModel.ToAmount = toAmount;
                swapViewModel.ToAmountFormat = toCurrencyViewModel.CurrencyFormat;
                swapViewModel.Price = swap.Price;
                swapViewModel.PriceFormat = $"F{quoteCurrency.Decimals}";
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error while update SwapViewModel for {swap.Symbol} swap with id {swap.Id}");
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