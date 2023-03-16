using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
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
        [Reactive] public SwapCompactState CompactState { get; set; }
        public string SwapId { get; set; }
        public decimal Price { get; set; }
        public DateTime TimeStamp { get; set; }
        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public string FromAmountFormat => FromCurrencyViewModel.CurrencyFormat;
        public string ToAmountFormat => ToCurrencyViewModel.CurrencyFormat;
        public string FromCurrencyCode => FromCurrencyViewModel.CurrencyName;
        public string ToCurrencyCode => ToCurrencyViewModel.CurrencyName;
        [Reactive] public IEnumerable<Atomex.ViewModels.Helpers.SwapDetailingInfo> DetailingInfo { get; set; }

        public SwapDetailsViewModel()
        {
            this.WhenAnyValue(vm => vm.CompactState)
                .Select(s => s switch
                {
                    SwapCompactState.InProgress => "Swap in Progress",
                    SwapCompactState.Completed => "Swap Completed",
                    SwapCompactState.Canceled => "Swap Cancelled",
                    SwapCompactState.Refunded => "Funds Refunded",
                    SwapCompactState.Unsettled => "Funds Unsettled",
                    _ => string.Empty
                })
                .ToPropertyExInMainThread(this, vm => vm.SwapCompactStateTitle);

            this.WhenAnyValue(vm => vm.CompactState)
                .Select(s => s switch
                {
                    SwapCompactState.InProgress => "Do not close the Atomex app until the swap is completed",
                    SwapCompactState.Completed => "You can close Atomex app now",
                    SwapCompactState.Canceled => "Swap was cancelled",
                    SwapCompactState.Refunded => "Lock time has passed",
                    SwapCompactState.Unsettled => "You did not have time to redeem the funds, please contact Atomex support",
                    _ => string.Empty
                })
                .ToPropertyExInMainThread(this, vm => vm.SwapCompactStateDescription);

            this.WhenAnyValue(
                    vm => vm.CompactState,
                    vm => vm.DetailingInfo)
                .Where(v => v.Item2 != null)
                .Select(_ => GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus.Initialization))
                .ToPropertyExInMainThread(this, vm => vm.InitializationStepStatus);

            this.WhenAnyValue(
                    vm => vm.CompactState,
                    vm => vm.DetailingInfo)
                .Where(v => v.Item2 != null)
                .Select(_ => GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging))
                .ToPropertyExInMainThread(this, vm => vm.ExchangingStepStatus);

            this.WhenAnyValue(
                    vm => vm.CompactState,
                    vm => vm.DetailingInfo)
                .Where(v => v.Item2 != null)
                .Select(_ => GetSwapDetailedStepState(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion))
                .ToPropertyExInMainThread(this, vm => vm.CompletionStepStatus);

            this.WhenAnyValue(vm => vm.InitializationStepStatus)
                .Select(s => s == SwapDetailedStepState.InProgress)
                .ToPropertyExInMainThread(this, vm => vm.InitializationStepInProgress);

            this.WhenAnyValue(vm => vm.ExchangingStepStatus)
                .Select(s => s == SwapDetailedStepState.InProgress)
                .ToPropertyExInMainThread(this, vm => vm.ExchangingStepInProgress);

            this.WhenAnyValue(vm => vm.CompletionStepStatus)
                .Select(s => s == SwapDetailedStepState.InProgress)
                .ToPropertyExInMainThread(this, vm => vm.CompletionStepInProgress);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Initialization, 0)?.Description)
                .ToPropertyExInMainThread(this, vm => vm.InitializationFirstStepDescription);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Initialization, 1)?.Description)
                .ToPropertyExInMainThread(this, vm => vm.InitializationSecondStepDescription);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 0)?.Description)
                .ToPropertyExInMainThread(this, vm => vm.ExchangingFirstStepDescription);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 1)?.Description)
                .ToPropertyExInMainThread(this, vm => vm.ExchangingSecondStepDescription);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 0)?.Description)
                .ToPropertyExInMainThread(this, vm => vm.CompletionFirstStepDescription);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 1)?.Description)
                .ToPropertyExInMainThread(this, vm => vm.CompletionSecondStepDescription);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 0)?.ExplorerLink?.Text)
                .ToPropertyExInMainThread(this, vm => vm.ExchangingFirstStepLinkText);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 1)?.ExplorerLink?.Text)
                .ToPropertyExInMainThread(this, vm => vm.ExchangingSecondStepLinkText);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 0)?.ExplorerLink?.Text)
                .ToPropertyExInMainThread(this, vm => vm.CompletionFirstStepLinkText);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 1)?.ExplorerLink?.Text)
                .ToPropertyExInMainThread(this, vm => vm.CompletionSecondStepLinkText);


            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 0)?.ExplorerLink?.Url)
                .ToPropertyExInMainThread(this, vm => vm.ExchangingFirstStepLinkUrl);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Exchanging, 1)?.ExplorerLink?.Url)
                .ToPropertyExInMainThread(this, vm => vm.ExchangingSecondStepLinkUrl);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 0)?.ExplorerLink?.Url)
                .ToPropertyExInMainThread(this, vm => vm.CompletionFirstStepLinkUrl);

            this.WhenAnyValue(vm => vm.DetailingInfo)
                .WhereNotNull()
                .Select(s => GetStatusDescription(Atomex.ViewModels.Helpers.SwapDetailingStatus.Completion, 1)?.ExplorerLink?.Url)
                .ToPropertyExInMainThread(this, vm => vm.CompletionSecondStepLinkUrl);

#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }


        private ICommand? _closeCommand;
        public ICommand CloseCommand => _closeCommand ??= _closeCommand = ReactiveCommand.Create(() =>
        {
            OnClose?.Invoke();
        });

        [ObservableAsProperty] public string SwapCompactStateTitle { get; }
        [ObservableAsProperty] public string SwapCompactStateDescription { get; }

        [ObservableAsProperty] public string? InitializationFirstStepDescription { get; }
        [ObservableAsProperty] public string? InitializationSecondStepDescription { get; }
        [ObservableAsProperty] public string? ExchangingFirstStepDescription { get; }
        [ObservableAsProperty] public string? ExchangingSecondStepDescription { get; }
        [ObservableAsProperty] public string? CompletionFirstStepDescription { get; }
        [ObservableAsProperty] public string? CompletionSecondStepDescription { get; }

        [ObservableAsProperty] public string? ExchangingFirstStepLinkText { get; }
        [ObservableAsProperty] public string? ExchangingSecondStepLinkText { get; }
        [ObservableAsProperty] public string? CompletionFirstStepLinkText { get; }
        [ObservableAsProperty] public string? CompletionSecondStepLinkText { get; }

        [ObservableAsProperty] public string? ExchangingFirstStepLinkUrl { get; }
        [ObservableAsProperty] public string? ExchangingSecondStepLinkUrl { get; }
        [ObservableAsProperty] public string? CompletionFirstStepLinkUrl { get; }
        [ObservableAsProperty] public string? CompletionSecondStepLinkUrl { get; }

        private Atomex.ViewModels.Helpers.SwapDetailingInfo? GetStatusDescription(
            Atomex.ViewModels.Helpers.SwapDetailingStatus status, int number)
        {
            return DetailingInfo.Any(info => info.Status == status)
                ? DetailingInfo
                    .Where(info => info.Status == status)
                    .ElementAtOrDefault(number)
                : null;
        }

        [ObservableAsProperty] public SwapDetailedStepState InitializationStepStatus { get; }
        [ObservableAsProperty] public SwapDetailedStepState ExchangingStepStatus { get; }
        [ObservableAsProperty] public SwapDetailedStepState CompletionStepStatus { get; }

        [ObservableAsProperty] public bool InitializationStepInProgress => InitializationStepStatus == SwapDetailedStepState.InProgress;
        [ObservableAsProperty] public bool ExchangingStepInProgress => ExchangingStepStatus == SwapDetailedStepState.InProgress;
        [ObservableAsProperty] public bool CompletionStepInProgress => CompletionStepStatus == SwapDetailedStepState.InProgress;

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
            if (CompactState == SwapCompactState.Completed)
                return SwapDetailedStepState.Completed;

            if (DetailingInfo
                .Where(info => info.Status == status)
                .ToList()
                .Find(info => info.IsCompleted) != null)
            {
                return SwapDetailedStepState.Completed;
            }

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

            FromCurrencyViewModel = CurrencyViewModelCreator.CreateOrGet(btc, subscribeToUpdates: false);
            ToCurrencyViewModel = CurrencyViewModelCreator.CreateOrGet(ltc, subscribeToUpdates: false);

            CompactState = SwapCompactState.InProgress;
        }
#endif
    }
}