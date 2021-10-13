using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Core;
using DynamicData;
using ReactiveUI;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public enum SwapDetailedStepState
    {
        ToBeDone,
        InProgress,
        Completed,
        Failed
    }

    public class SwapDetailsViewModel : ViewModelBase
    {
        public Action? OnClose { get; set; }
        public SwapCompactState CompactState { get; set; }
        public string SwapId { get; set; }
        public decimal Price { get; set; }
        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public string FromAmountFormat => FromCurrencyViewModel.CurrencyFormat;
        public string ToAmountFormat => ToCurrencyViewModel.CurrencyFormat;
        public string FromCurrencyCode => FromCurrencyViewModel.CurrencyCode;
        public string ToCurrencyCode => ToCurrencyViewModel.CurrencyCode;
        public IEnumerable<Atomex.ViewModels.Helpers.SwapDetailingInfo> DetailingInfo { get; set; }


        private ICommand? _closeCommand;

        public ICommand CloseCommand => _closeCommand ??= _closeCommand = ReactiveCommand.Create(() =>
        {
            OnClose?.Invoke();
        });


        public SwapDetailedStepState InitializationStepStatus =>
            GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus.Initialization);

        public SwapDetailedStepState ExchangingStepStatus =>
            GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging);

        public SwapDetailedStepState CompletionStepStatus =>
            GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion);

        public static SwapDetailedStepState ToBeDoneState => SwapDetailedStepState.ToBeDone;
        public static SwapDetailedStepState InProgressState => SwapDetailedStepState.InProgress;
        public static SwapDetailedStepState CompletedState => SwapDetailedStepState.Completed;
        public static SwapDetailedStepState FailedState => SwapDetailedStepState.Failed;

        private SwapDetailedStepState GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus status)
        {
            switch (CompactState)
            {
                case SwapCompactState.Completed:
                    return SwapDetailedStepState.Completed;
                case SwapCompactState.Canceled or SwapCompactState.Unsettled:
                    return SwapDetailedStepState.Failed;
            }

            if (DetailingInfo
                    .Where(info => info.Status == status)
                    .ToList()
                    .Find(info => info.IsCompleted) != null
            ) return SwapDetailedStepState.Completed;

            return DetailingInfo.Any(info => info.Status == status)
                ? SwapDetailedStepState.InProgress
                : SwapDetailedStepState.ToBeDone;
        }
    }
}