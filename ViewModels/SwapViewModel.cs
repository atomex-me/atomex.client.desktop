﻿using System;
using System.Reactive.Linq;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;

namespace Atomex.Client.Desktop.ViewModels
{
    public enum SwapCompactState
    {
        Canceled,
        InProgress,
        Completed,
        Refunded,
        Unsettled
    }

    public enum SwapMode
    {
        Initiator,
        CounterParty
    }

    public class SwapViewModel : ViewModelBase
    {
        public string Id { get; set; }

        [Reactive] public SwapCompactState CompactState { get; set; }
        [Reactive] public SwapMode Mode { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();

        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public decimal FromAmount { get; set; }
        public string FromAmountFormat { get; set; }
        public string FromCurrencyCode { get; set; }

        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public decimal ToAmount { get; set; }
        public string ToAmountFormat { get; set; }
        public string ToCurrencyCode { get; set; }

        public decimal Price { get; set; }
        public string PriceFormat { get; set; }

        [Reactive] public SwapDetailsViewModel Details { get; set; }

        public SwapViewModel()
        {
            this.WhenAnyValue(vm => vm.CompactState)
                .Select(s => s switch
                {
                    SwapCompactState.Canceled => "Canceled",
                    SwapCompactState.InProgress => "In Progress",
                    SwapCompactState.Completed => "Completed",
                    SwapCompactState.Refunded => "Refunded",
                    SwapCompactState.Unsettled => "Unsettled",
                    _ => throw new ArgumentOutOfRangeException(),
                })
                .ToPropertyExInMainThread(this, vm => vm.State);
        }

        [ObservableAsProperty] public string State { get; }
    }
}