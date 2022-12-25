using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Dialogs;
using Atomex.Client.Desktop.Helpers;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.ViewModels;
using Atomex.Wallet.Tezos;
using Atomex.Wallets.Tezos;
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

        [Reactive] public IEnumerable<BaseBeaconOperationViewModel>? Operations { get; set; }
        private IEnumerable<ManagerOperationContent> InitialOperations { get; set; }
        [Reactive] private byte[]? ForgedOperations { get; set; }
        [Reactive] private string? RawOperations { get; set; }
        [ObservableAsProperty] public string OperationsBytes { get; set; }
        [Reactive] public decimal TotalFees { get; set; }
        [Reactive] public decimal TotalFeesInBase { get; set; }
        [Reactive] public int TotalGasLimit { get; set; }
        [Reactive] public int TotalStorageLimit { get; set; }
        [Reactive] public decimal TotalGasFee { get; set; }
        [Reactive] public decimal TotalGasFeeInBase { get; set; }
        [Reactive] public decimal TotalStorageFee { get; set; }
        [Reactive] public decimal TotalStorageFeeInBase { get; set; }
        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public bool AutofillError { get; set; }
        [Reactive] public bool DetailsOpened { get; set; }
        [Reactive] public IQuotesProvider? QuotesProvider { get; init; }
        private static string BaseCurrencyCode => "USD";
        public static string BaseCurrencyFormat => "$0.##";
        private int? ByteCost { get; set; }
        private TezosConfig Tezos { get; }
        private int DefaultOperationGasLimit { get; }
        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        public OperationRequestViewModel(
            IEnumerable<ManagerOperationContent> operations,
            WalletAddress connectedAddress,
            int operationGasLimit,
            TezosConfig tezosConfig)
        {
            Tezos = tezosConfig;
            ConnectedAddress = connectedAddress;
            InitialOperations = operations;
            DefaultOperationGasLimit = operationGasLimit;

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
                        if (!int.TryParse(responseJObj?["constants"]?["byteCost"]?.ToString(), out var byteCost))
                            return;

                        ByteCost = byteCost;
                        TotalStorageFee = TezosConfig.MtzToTz(Convert.ToDecimal(byteCost)) * TotalStorageLimit;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during sending request to {Url}", url);
                    }
                });

            this.WhenAnyValue(vm => vm.TotalGasFee, vm => vm.TotalStorageFee)
                .WhereAllNotNull()
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Select(values => values.Item1 + values.Item2)
                .SubscribeInMainThread(totalFees =>
                {
                    TotalFees = totalFees;
                    OnQuotesUpdatedEventHandler(QuotesProvider, EventArgs.Empty);
                });

            this.WhenAnyValue(vm => vm.TotalStorageLimit)
                .WhereNotNull()
                .Skip(1)
                .SubscribeInMainThread(totalStorageLimit =>
                {
                    if (ByteCost != null)
                        TotalStorageFee = TezosConfig.MtzToTz(Convert.ToDecimal(ByteCost)) * totalStorageLimit;
                });

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .WhereNotNull()
                .Where(useDefaultFee => useDefaultFee)
                .SubscribeInMainThread(_ => { AutofillOperations(); });

            this.WhenAnyValue(vm => vm.TotalGasFee, vm => vm.TotalStorageLimit)
                .WhereAllNotNull()
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Where(_ => !UseDefaultFee)
                .SubscribeInMainThread(_ => { AutofillOperations(); });

            this.WhenAnyValue(vm => vm.ForgedOperations)
                .WhereNotNull()
                .Select(forgedOperations => forgedOperations.ToHexString())
                .ToPropertyExInMainThread(this, vm => vm.OperationsBytes);

            UseDefaultFee = true;

#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        private async void AutofillOperations()
        {
            AutofillError = false;
            
            var rpc = new Rpc(Tezos.RpcNodeUri);
            JObject head;
            try
            {
                head = await rpc
                    .GetHeader()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during querying rpc, {Message}", ex.Message);
                return;
            }

            var operations = InitialOperations.Select(op => op switch
                {
                    TransactionContent txContent => new TransactionContent
                    {
                        Amount = txContent.Amount,
                        Destination = txContent.Destination,
                        Parameters = txContent.Parameters,
                        Source = txContent.Source,
                        Fee = UseDefaultFee ? 0 : txContent.Fee,
                        Counter = txContent.Counter,
                        GasLimit = UseDefaultFee ? DefaultOperationGasLimit : txContent.GasLimit,
                        StorageLimit = UseDefaultFee ? DappsViewModel.StorageLimitPerOperation : txContent.StorageLimit
                    },
                    RevealContent revealContent => new RevealContent
                    {
                        PublicKey = revealContent.PublicKey,
                        Source = revealContent.Source,
                        Fee = UseDefaultFee ? 0 : revealContent.Fee,
                        Counter = revealContent.Counter,
                        GasLimit = UseDefaultFee ? DefaultOperationGasLimit : revealContent.GasLimit,
                        StorageLimit = UseDefaultFee
                            ? DappsViewModel.StorageLimitPerOperation
                            : revealContent.StorageLimit
                    },
                    _ => op
                })
                .ToList();

            // if (!UseDefaultFee)  
            // {
            //     if (TotalStorageLimit != 0)
            //     {
            //         var notZeroStorageLimitIdx = operations.FindIndex(op => op.StorageLimit != 0);
            //
            //         foreach (var op in operations)
            //         {
            //             op.StorageLimit = 0;
            //         }
            //
            //         operations[notZeroStorageLimitIdx].StorageLimit = TotalStorageLimit;
            //     }
            // }

            var avgFee = Convert.ToInt64(TotalGasFee * TezosConfig.XtzDigitsMultiplier / operations.Count);

            if (!UseDefaultFee)
            {
                foreach (var op in operations)
                {
                    op.Fee = avgFee;
                }
            }

            var error = await TezosOperationFiller.AutoFillAsync(
                    operations,
                    head["hash"]!.ToString(),
                    head["chain_id"]!.ToString(),
                    Tezos)
                .ConfigureAwait(false);

            if (!UseDefaultFee)
            {
                foreach (var op in operations)
                {
                    op.Fee = avgFee;
                }
            }

            if (error != null)
                AutofillError = true;

            var branch = head["hash"]!.ToString();

            ForgedOperations = await TezosForge.ForgeAsync(
                operations: operations,
                branch: branch);

            var rawJObj = new JObject
            {
                ["branch"] = branch,
                ["contents"] = JArray.Parse(JsonConvert.SerializeObject(operations))
            };

            RawOperations = JsonConvert.SerializeObject(rawJObj, Formatting.Indented);

            if (!UseDefaultFee && Operations != null) return;

            var operationsViewModel = new ObservableCollection<BaseBeaconOperationViewModel>();
            foreach (var item in operations.Select((value, idx) => new { idx, value }))
            {
                var operation = item.value;
                var index = item.idx;

                switch (operation)
                {
                    case TransactionContent transactionOperation:
                        operationsViewModel.Add(new TransactionContentViewModel
                        {
                            Id = index + 1,
                            Operation = transactionOperation,
                            QuotesProvider = QuotesProvider,
                            ExplorerUri = Tezos.AddressExplorerUri
                        });
                        break;
                    case RevealContent revealOperation:
                        operationsViewModel.Add(new RevealContentViewModel
                        {
                            Id = index + 1,
                            Operation = revealOperation,
                            QuotesProvider = QuotesProvider,
                        });
                        break;
                }
            }

            if (Operations != null)
            {
                foreach (var op in Operations)
                {
                    if (op is IDisposable disposable)
                        disposable.Dispose();
                }
            }

            InitialOperations = operations;
            Operations = operationsViewModel;
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
        public Func<byte[], Task> OnConfirm { get; init; }
        public Func<Task> OnReject { get; init; }

        private ReactiveCommand<Unit, Unit>? _onConfirmCommand;

        public ReactiveCommand<Unit, Unit> OnConfirmCommand =>
            _onConfirmCommand ??= ReactiveCommand.CreateFromTask(async () =>
            {
                if (ForgedOperations != null)
                    await OnConfirm(ForgedOperations);
            });

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
            // TotalFees = (decimal)0.222223121331233;
            TotalFeesInBase = (decimal)0.123123;

            TotalGasLimit = 234344;
            TotalStorageLimit = 16000;
            TotalGasFee = (decimal)0.002331231234;
            TotalStorageFee = (decimal)0.022331231234;

            UseDefaultFee = false;
            AutofillError = true;

            OperationsBytes =
                "tz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWhpqt9XGGH6tCAWtz1dwWLbMrt2tz1dwWLbMrt2GJcKBK6mRwhpqt9XGGH6tCAWtz1dwWLbMrt2GJcK";
        }
#endif
    }
}