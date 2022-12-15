using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.ViewModels;
using Avalonia.Controls;
using Netezos.Forging.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using DecimalExtensions = Atomex.Common.DecimalExtensions;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public abstract class BaseBeaconOperationViewModel : ViewModelBase, IDisposable
    {
        public int Id { get; set; }
        protected static string BaseCurrencyCode => "USD";
        public string BaseCurrencyFormat => "$0.##";
        public abstract string JsonStringOperation { get; }
        [Reactive] public IQuotesProvider? QuotesProvider { get; init; }
        [Reactive] public bool IsDetailsOpened { get; set; }

        protected BaseBeaconOperationViewModel()
        {
            this.WhenAnyValue(vm => vm.QuotesProvider)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(quotesProvider =>
                {
                    quotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
                    OnQuotesUpdatedEventHandler(quotesProvider, EventArgs.Empty);
                });
        }

        protected abstract void OnQuotesUpdatedEventHandler(object? sender, EventArgs args);

        private ReactiveCommand<Unit, Unit>? _onOpenDetailsCommand;

        public ReactiveCommand<Unit, Unit> OnOpenDetailsCommand =>
            _onOpenDetailsCommand ??= ReactiveCommand.Create(() => { IsDetailsOpened = !IsDetailsOpened; });

        public void Dispose()
        {
            if (QuotesProvider != null)
                QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
        }
    }

    public class TransactionContentViewModel : BaseBeaconOperationViewModel
    {
        [Reactive] public TransactionContent Operation { get; set; }
        public override string JsonStringOperation => JsonConvert.SerializeObject(Operation, Formatting.Indented);
        public decimal AmountInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Amount));
        public decimal FeeInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Fee));
        public string DestinationIcon => $"https://services.tzkt.io/v1/avatars/{Operation.Destination}";
        [Reactive] public string? DestinationAlias { get; set; }
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [ObservableAsProperty] public string Entrypoint { get; }
        public string ExplorerUri { get; set; }

        public TransactionContentViewModel()
        {
            this.WhenAnyValue(vm => vm.Operation)
                .WhereNotNull()
                .SubscribeInMainThread(async operation =>
                {
                    DestinationAlias = operation.Destination.TruncateAddress();

                    using var response = await HttpHelper.GetAsync(
                        baseUri: "https://api.tzkt.io/",
                        relativeUri: $"v1/accounts/{operation.Destination}?metadata=true");

                    if (response.StatusCode != HttpStatusCode.OK) return;

                    var responseContent = await response
                        .Content
                        .ReadAsStringAsync();

                    var responseJObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                    var alias = responseJObj?["metadata"]?["alias"]?.ToString();
                    if (alias != null)
                        DestinationAlias = alias;
                });

            this.WhenAnyValue(vm => vm.Operation)
                .WhereNotNull()
                .Select(operation =>
                    operation.Parameters == null ? "Transfer to " : $"Call {operation.Parameters.Entrypoint} in ")
                .ToPropertyExInMainThread(this, vm => vm.Entrypoint);

#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        protected override void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
            AmountInBase = AmountInTez.SafeMultiply(xtzQuote?.Bid ?? 0);
            FeeInBase = FeeInTez.SafeMultiply(xtzQuote?.Bid ?? 0);
            Log.Debug("Quotes updated for beacon TransactionContent operation {Id}", Id);
        }

        private ReactiveCommand<Unit, Unit>? _openDestinationInExplorer;

        public ReactiveCommand<Unit, Unit> OpenDestinationInExplorer => _openDestinationInExplorer ??=
            ReactiveCommand.Create(() =>
            {
                if (Uri.TryCreate($"{ExplorerUri}{Operation.Destination}", UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
            });

#if DEBUG

        private void DesignerMode()
        {
            Operation = new TransactionContent
            {
                Amount = 0,
                Destination = "KT1SjXiUX63QvdNMcM2m492f7kuf8JxXRLp4",
                Parameters = new Parameters
                {
                    Entrypoint = "approve"
                }
            };
        }
#endif
    }

    public class RevealContentViewModel : BaseBeaconOperationViewModel
    {
        public RevealContent Operation { get; set; }
        public override string JsonStringOperation => JsonConvert.SerializeObject(Operation, Formatting.Indented);
        public decimal FeeInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Fee));
        [Reactive] public decimal FeeInBase { get; set; }

        protected override void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
            FeeInBase = FeeInTez.SafeMultiply(xtzQuote?.Bid ?? 0);
            Log.Debug("Quotes updated for beacon RevealContent operation {Id}", Id);
        }
    }

    public class OperationRequestViewModel : ViewModelBase, IDisposable
    {
        public string DappName { get; set; }
        public string SubTitle => $"{DappName} requests to confirm operations";
        public string? DappLogo { get; set; }
        public WalletAddress ConnectedAddress { get; set; }

        public string TezosFormat =>
            DecimalExtensions.GetFormatWithPrecision((int)Math.Round(Math.Log10(TezosConfig.XtzDigitsMultiplier)));

        [Reactive] public IEnumerable<BaseBeaconOperationViewModel> Operations { get; set; }
        public string OperationsBytes { get; set; }
        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        public OperationRequestViewModel()
        {
            OnConfirmCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);

#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Func<Task> OnConfirm { get; set; }
        public Func<Task> OnReject { get; set; }

        private ReactiveCommand<Unit, Unit>? _onConfirmCommand;

        public ReactiveCommand<Unit, Unit> OnConfirmCommand =>
            _onConfirmCommand ??= ReactiveCommand.CreateFromTask(async () => await OnConfirm());

        private ReactiveCommand<Unit, Unit>? _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());

        private ReactiveCommand<string, Unit>? _copyCommand;

        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>(data =>
        {
            try
            {
                App.Clipboard!.SetTextAsync(data);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

        public void Dispose()
        {
            foreach (var operation in Operations)
            {
                operation.Dispose();
            }
        }

#if DEBUG
        private void DesignerMode()
        {
            DappName = "objkt.com";
            DappLogo = "";
            ConnectedAddress = new WalletAddress
            {
                Address = "tzhSFN6677KThcTkTai3P1acVQtrujkkvVd",
                Balance = (decimal)0.991873633123
            };
            OperationsBytes =
                "tz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcK";
        }
#endif
    }
}