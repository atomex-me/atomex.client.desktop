using System;
using Avalonia.Media;

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

        public SwapCompactState CompactState { get; set; }
        public SwapMode Mode { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();

        public IBrush FromBrush { get; set; }
        public decimal FromAmount { get; set; }
        public string FromAmountFormat { get; set; }
        public string FromCurrencyCode { get; set; }

        public IBrush ToBrush { get; set; }
        public decimal ToAmount { get; set; }
        public string ToAmountFormat { get; set; }
        public string ToCurrencyCode { get; set; }

        public decimal Price { get; set; }
        public string PriceFormat { get; set; }

        public string State
        {
            get
            {
                switch (CompactState)
                {
                    case SwapCompactState.Canceled:
                        return "Canceled";
                    case SwapCompactState.InProgress:
                        return "In Progress";
                    case SwapCompactState.Completed:
                        return "Completed";
                    case SwapCompactState.Refunded:
                        return "Refunded";
                    case SwapCompactState.Unsettled:
                        return "Unsettled";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}