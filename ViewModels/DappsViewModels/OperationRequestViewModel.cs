using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Dialogs;
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

        public string TezosFormat =>
            DecimalExtensions.GetFormatWithPrecision((int)Math.Round(Math.Log10(TezosConfig.XtzDigitsMultiplier)));

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
                    var url = $"v1/accounts/{operation.Destination}?metadata=true";

                    try
                    {
                        using var response = await HttpHelper.GetAsync(
                            baseUri: "https://api.tzkt.io/",
                            relativeUri: url);

                        if (response.StatusCode != HttpStatusCode.OK) return;

                        var responseContent = await response
                            .Content
                            .ReadAsStringAsync();

                        var responseJObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        var alias = responseJObj?["metadata"]?["alias"]?.ToString();
                        if (alias != null)
                            DestinationAlias = alias;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during sending request to {Url}", url);
                    }
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

    public class OperationRequestViewModel : ViewModelBase, IDialogViewModel, IDisposable
    {
        public string DappName { get; set; }
        public string SubTitle => $"{DappName} requests to confirm operations";
        public string? DappLogo { get; set; }
        public WalletAddress ConnectedAddress { get; set; }

        public string TezosFormat =>
            DecimalExtensions.GetFormatWithPrecision((int)Math.Round(Math.Log10(TezosConfig.XtzDigitsMultiplier)));

        [Reactive] public IEnumerable<BaseBeaconOperationViewModel> Operations { get; set; }
        [Reactive] public decimal TotalFees { get; set; }
        [Reactive] public decimal TotalFeesInBase { get; set; }
        [Reactive] public int TotalGasLimit { get; set; }
        [Reactive] public int TotalStorageLimit { get; set; }
        [Reactive] public decimal TotalGasFee { get; set; }
        [Reactive] public decimal TotalGasFeeInBase { get; set; }
        [Reactive] public decimal TotalStorageFee { get; set; }
        [Reactive] public decimal TotalStorageFeeInBase { get; set; }
        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public bool DetailsOpened { get; set; }
        public string OperationsBytes { get; set; }
        [Reactive] public IQuotesProvider? QuotesProvider { get; init; }
        private static string BaseCurrencyCode => "USD";
        private static string BaseCurrencyFormat => "$0.##";
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

            this.WhenAnyValue(vm => vm.QuotesProvider)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(quotesProvider =>
                {
                    quotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
                });

            this.WhenAnyValue(vm => vm.Operations, vm => vm.QuotesProvider)
                .WhereAllNotNull()
                .Select(args => args.Item1.ToList())
                .SubscribeInMainThread(async operations =>
                {
                    TotalGasLimit = operations.Aggregate(0, (res, currentOp) => currentOp switch
                    {
                        TransactionContentViewModel transactionOp => res + transactionOp.Operation.GasLimit,
                        RevealContentViewModel revealOp => res + revealOp.Operation.GasLimit,
                        _ => res
                    });

                    TotalStorageLimit = operations.Aggregate(0, (res, currentOp) => currentOp switch
                    {
                        TransactionContentViewModel transactionOp => res + transactionOp.Operation.StorageLimit,
                        RevealContentViewModel revealOp => res + revealOp.Operation.StorageLimit,
                        _ => res
                    });

                    TotalGasFee = operations.Aggregate(0m, (res, currentOp) => currentOp switch
                    {
                        TransactionContentViewModel transactionOp => res + transactionOp.FeeInTez,
                        RevealContentViewModel revealOp => res + revealOp.FeeInTez,
                        _ => res
                    });

                    const string url = "v1/protocols/current";
                    try
                    {
                        using var response = await HttpHelper.GetAsync(
                            baseUri: "https://api.tzkt.io/",
                            relativeUri: url);

                        if (response.StatusCode != HttpStatusCode.OK) return;

                        var responseContent = await response
                            .Content
                            .ReadAsStringAsync();

                        var responseJObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        if (int.TryParse(responseJObj?["constants"]?["byteCost"]?.ToString(), out var byteCost))
                            TotalStorageFee = TezosConfig.MtzToTz(Convert.ToDecimal(byteCost)) * TotalStorageLimit;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during sending request to {Url}", url);
                    }

                    TotalFees = TotalGasFee + TotalStorageFee;
                    OnQuotesUpdatedEventHandler(QuotesProvider, EventArgs.Empty);
                });

            UseDefaultFee = true;
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        private void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
            TotalGasFeeInBase = TotalGasFee.SafeMultiply(xtzQuote?.Bid ?? 0);
            TotalStorageFeeInBase = TotalStorageFee.SafeMultiply(xtzQuote?.Bid ?? 0);
            TotalFeesInBase = TotalFees.SafeMultiply(xtzQuote?.Bid ?? 0);

            Log.Debug("Quotes updated for Operation Request View");
        }

        public Action? OnClose { get; set; }
        public Func<Task> OnConfirm { get; init; }
        public Func<Task> OnReject { get; init; }

        private ReactiveCommand<Unit, Unit>? _onConfirmCommand;

        public ReactiveCommand<Unit, Unit> OnConfirmCommand =>
            _onConfirmCommand ??= ReactiveCommand.CreateFromTask(async () => await OnConfirm());

        private ReactiveCommand<Unit, Unit>? _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());

        private ReactiveCommand<Unit, Unit>? _openDetailsCommand;

        public ReactiveCommand<Unit, Unit> OpenDetailsCommand =>
            _openDetailsCommand ??= ReactiveCommand.Create(() => { DetailsOpened = !DetailsOpened; });

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
            if (QuotesProvider != null)
                QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;

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

            DetailsOpened = true;
            TotalFees = (decimal)0.222223121331233;
            TotalFeesInBase = (decimal)0.123123;

            TotalGasLimit = 234344;
            TotalStorageLimit = 16000;
            TotalGasFee = (decimal)0.002331231234;
            TotalStorageFee = (decimal)0.022331231234;

            OperationsBytes =
                "tz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcK";
        }
#endif
    }
}