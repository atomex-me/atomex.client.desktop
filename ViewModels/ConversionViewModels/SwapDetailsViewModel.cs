using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using ReactiveUI;

using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Avalonia.Controls;
using Atomex.Client.Desktop.Common;

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
        public DateTime TimeStamp { get; set; }
        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public string FromAmountFormat => FromCurrencyViewModel.CurrencyFormat;
        public string ToAmountFormat => ToCurrencyViewModel.CurrencyFormat;
        public string FromCurrencyCode => FromCurrencyViewModel.CurrencyCode;
        public string ToCurrencyCode => ToCurrencyViewModel.CurrencyCode;
        public IEnumerable<Atomex.ViewModels.Helpers.SwapDetailingInfo> DetailingInfo { get; set; }

#if DEBUG
        public SwapDetailsViewModel()
        {

            if (Design.IsDesignMode)
                DesignerMode();
        }
#endif

        private ICommand? _closeCommand;
        public ICommand CloseCommand => _closeCommand ??= _closeCommand = ReactiveCommand.Create(() =>
        {
            OnClose?.Invoke();
        });

        public string SwapCompactStateTitle => CompactState switch
        {
            SwapCompactState.InProgress => "Swap in Progress",
            SwapCompactState.Completed => "Swap Completed",
            SwapCompactState.Canceled => "Swap Cancelled",
            SwapCompactState.Refunded => "Funds Refunded",
            SwapCompactState.Unsettled => "Funds Unsettled",
            _ => string.Empty
        };

        public string SwapCompactStateDescription => CompactState switch
        {
            SwapCompactState.InProgress => "Do not close the Atomex app until the swap is completed",
            SwapCompactState.Completed => "You can close Atomex app now",
            SwapCompactState.Canceled => "Swap was cancelled",
            SwapCompactState.Refunded => "Lock time has passed",
            SwapCompactState.Unsettled => "You did not have time to redeem the funds, please contact Atomex support",
            _ => string.Empty
        };

        public string? InitializationFirstStepDescription =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Initialization, 0)?.Description;
        public string? InitializationSecondStepDescription =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Initialization, 1)?.Description;
        public string? ExchangingFirstStepDescription =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 0)?.Description;
        public string? ExchangingSecondStepDescription =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 1)?.Description;
        public string? CompletionFirstStepDescription =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 0)?.Description;
        public string? CompletionSecondStepDescription =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 1)?.Description;

        public string? ExchangingFirstStepLinkText =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 0)?.ExplorerLink?.Text;
        public string? ExchangingSecondStepLinkText =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 1)?.ExplorerLink?.Text;
        public string? CompletionFirstStepLinkText =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 0)?.ExplorerLink?.Text;
        public string? CompletionSecondStepLinkText =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 1)?.ExplorerLink?.Text;
        
        public string? ExchangingFirstStepLinkUrl =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 0)?.ExplorerLink?.Url;
        public string? ExchangingSecondStepLinkUrl =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 1)?.ExplorerLink?.Url;
        public string? CompletionFirstStepLinkUrl =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 0)?.ExplorerLink?.Url;
        public string? CompletionSecondStepLinkUrl =>
            GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 1)?.ExplorerLink?.Url;

        private Atomex.ViewModels.Helpers.SwapDetailingInfo? GetStatusDescription(
            Atomex.ViewModels.Helpers.SwapDetailingStatus status, int number)
        {
            return DetailingInfo.Any(info => info.Status == status)
                ? DetailingInfo
                    .Where(info => info.Status == status)
                    .ElementAtOrDefault(number)
                : null;
        }

        public SwapDetailedStepState InitializationStepStatus =>
            GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus.Initialization);
        public SwapDetailedStepState ExchangingStepStatus =>
            GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging);
        public SwapDetailedStepState CompletionStepStatus =>
            GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion);

        public bool InitializationStepInProgress => InitializationStepStatus == SwapDetailedStepState.InProgress;
        public bool ExchangingStepInProgress => ExchangingStepStatus == SwapDetailedStepState.InProgress;
        public bool CompletionStepInProgress => CompletionStepStatus == SwapDetailedStepState.InProgress;

        public static SwapDetailedStepState ToBeDoneState => SwapDetailedStepState.ToBeDone;
        public static SwapDetailedStepState InProgressState => SwapDetailedStepState.InProgress;
        public static SwapDetailedStepState CompletedState => SwapDetailedStepState.Completed;
        public static SwapDetailedStepState FailedState => SwapDetailedStepState.Failed;
        public static SwapCompactState InProgress => SwapCompactState.InProgress;
        public static SwapCompactState Completed => SwapCompactState.Completed;
        public static SwapCompactState Cancelled => SwapCompactState.Canceled;
        public static SwapCompactState Refunded => SwapCompactState.Refunded;
        public static SwapCompactState Unsettled => SwapCompactState.Unsettled;

        private SwapDetailedStepState GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus status)
        {
            if (CompactState == SwapCompactState.Completed) return SwapDetailedStepState.Completed;

            if (DetailingInfo
                    .Where(info => info.Status == status)
                    .ToList()
                    .Find(info => info.IsCompleted) != null
            ) return SwapDetailedStepState.Completed;

            var result = DetailingInfo.Any(info => info.Status == status)
                ? SwapDetailedStepState.InProgress
                : SwapDetailedStepState.ToBeDone;

            if (result is SwapDetailedStepState.InProgress or SwapDetailedStepState.ToBeDone &&
                CompactState is SwapCompactState.Canceled or SwapCompactState.Unsettled or SwapCompactState.Refunded)
                result = SwapDetailedStepState.Failed;

            return result;
        }

        private ICommand? _openTxInExplorerCommand;
        public ICommand OpenTxInExplorerCommand =>
            _openTxInExplorerCommand ??= ReactiveCommand.Create<string>(App.OpenBrowser);

#if DEBUG
        private void DesignerMode()
        {
            var btc = DesignTime.TestNetCurrencies.Get<BitcoinConfig>("BTC");
            var ltc = DesignTime.TestNetCurrencies.Get<LitecoinConfig>("LTC");

            FromCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(btc, subscribeToUpdates: false);
            ToCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(ltc, subscribeToUpdates: false);

            CompactState = SwapCompactState.InProgress;
        }
#endif
    }
}