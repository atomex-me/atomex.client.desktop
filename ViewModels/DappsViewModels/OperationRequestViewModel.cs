using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.MarketData.Abstract;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class OperationRequestViewModel : ViewModelBase, IDisposable
    {
        private IAtomexApp AtomexApp { get; }
        private TezosConfig TezosConfig { get; }
        private string BaseCurrencyCode => "USD";
        public string BaseCurrencyFormat => "$0.##";
        public string DappName { get; set; }
        public string SubTitle => $"{DappName} is asking to confirm the following transactions:";
        public string? DappLogo { get; set; }
        public decimal Amount => TezosConfig.MtzToTz(Transaction.Amount);

        [Reactive] public TezosTransaction Transaction { get; set; }
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }

        public string Operations => Transaction.Operations.ToString();
        // public string Operations { get; set; }

        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        public OperationRequestViewModel(IAtomexApp app, TezosConfig tezosConfig)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            TezosConfig = tezosConfig ?? throw new ArgumentNullException(nameof(tezosConfig));

            OnConfirmCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);

            AtomexApp.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;

            this.WhenAnyValue(vm => vm.Transaction)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(_ => OnQuotesUpdatedEventHandler(AtomexApp.QuotesProvider, EventArgs.Empty));
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        private void OnQuotesUpdatedEventHandler(object? sender, EventArgs e)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
            AmountInBase = Amount.SafeMultiply(quote?.Bid ?? 0);
            FeeInBase = Transaction.Fee.SafeMultiply(quote?.Bid ?? 0);
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
                App.Clipboard.SetTextAsync(data);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

        public void Dispose()
        {
            AtomexApp.QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
        }

#if DEBUG
        private void DesignerMode()
        {
            DappName = "objkt.com";
            DappLogo = "";

            Transaction = new TezosTransaction()
            {
                From = "tz1Mrt2GJcKBCAWdwWK6mRwhpqt9XGGH6tLb",
                To = "KT1EtjRRCBC2exyCRXz8UfV7jz7svnkqi7di",
                Amount = 5000000,
                GasLimit = 6789,
                StorageLimit = 100,
                Fee = 0.001262m,
            };

            // Operations =
            //    "[\r\n  {\r\n    \"kind\": \"transaction\",\r\n    \"source\": \"tz1Mrt2GJcKBCAWdwWK6mRwhpqt9XGGH6tLb\",\r\n    \"fee\": \"1262\",\r\n    \"counter\": \"64204625\",\r\n    \"gas_limit\": \"9798\",\r\n    \"storage_limit\": \"67\",\r\n    \"amount\": \"500000\",\r\n    \"destination\": \"KT1EtjRRCBC2exyCRXz8UfV7jz7svnkqi7di\",\r\n    \"parameters\": {\r\n      \"entrypoint\": \"collect\",\r\n      \"value\": {\r\n        \"int\": \"128129\"\r\n      }\r\n    }\r\n  }\r\n]";
        }
#endif
    }
}