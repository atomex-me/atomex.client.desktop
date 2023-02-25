using System;
using System.Reactive;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public abstract class TransactionViewModelBase : ViewModelBase
    {
        public event EventHandler<TransactionEventArgs>? UpdateClicked;
        public event EventHandler<TransactionEventArgs>? RemoveClicked;
        public string TxExplorerUri => $"{Currency.TxExplorerUri}{Id}";
        public ITransaction Transaction { get; set; }
        public CurrencyConfig Currency { get; set; }
        public string Id => Transaction.Id;
        public TransactionStatus State => Transaction.Status;
        public DateTime Time => Transaction.CreationTime?.UtcDateTime ?? DateTime.UtcNow;
        public DateTime LocalTime => Time.ToLocalTime();

        [Reactive] public ITransactionMetadata? TransactionMetadata { get; set; }
        [Reactive] public decimal Amount { get; set; }
        [Reactive] public string AmountFormat { get; set; }
        [Reactive] public string Description { get; set; }
        [Reactive] public TransactionType Type { get; set; }   
        [Reactive] public bool CanBeRemoved { get; set; }
        [Reactive] public bool IsReady { get; set; }

        public Action? OnClose { get; set; }

        private ReactiveCommand<Unit, Unit>? _openTxInExplorerCommand;
        public ReactiveCommand<Unit, Unit> OpenTxInExplorerCommand => _openTxInExplorerCommand ??=
            ReactiveCommand.Create(() => App.OpenBrowser(TxExplorerUri));

        private ReactiveCommand<string, Unit>? _openAddressInExplorerCommand;
        public ReactiveCommand<string, Unit> OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??=
            ReactiveCommand.Create<string>((address) =>
            {
                if (Uri.TryCreate($"{Currency.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
                else
                    Log.Error("Invalid uri for address explorer");
            });

        private ReactiveCommand<string, Unit>? _copyCommand;
        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                App.Clipboard.SetTextAsync(s);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

        private ReactiveCommand<Unit, Unit>? _updateCommand;
        public ReactiveCommand<Unit, Unit> UpdateCommand => _updateCommand ??= ReactiveCommand.Create(
            () => UpdateClicked?.Invoke(this, new TransactionEventArgs(Transaction)));

        private ReactiveCommand<Unit, Unit>? _removeCommand;
        public ReactiveCommand<Unit, Unit> RemoveCommand => _removeCommand ??= ReactiveCommand.Create(
            () => RemoveClicked?.Invoke(this, new TransactionEventArgs(Transaction)));

        private ReactiveCommand<Unit, Unit>? _onCloseCommand;
        public ReactiveCommand<Unit, Unit> OnCloseCommand => _onCloseCommand ??= ReactiveCommand.Create(
            () => OnClose?.Invoke());

        public abstract void UpdateMetadata(ITransactionMetadata metadata, CurrencyConfig config);
    }

    public abstract class TransactionViewModel : TransactionViewModelBase
    {
        [Reactive] public string CurrencyCode { get; set; }
        [Reactive] public string FeeCode { get; set; }
        [Reactive] public decimal Fee { get; set; }
        [Reactive] public string Direction { get; set; }

        public TransactionViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public TransactionViewModel(
            ITransaction tx,
            ITransactionMetadata? metadata,
            CurrencyConfig config,
            decimal amount,
            decimal fee,
            TransactionType type)
        {
            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            Currency = config;
            AmountFormat = config.Format;
            FeeCode = config.FeeCode;
            CurrencyCode = config.Name;
            CanBeRemoved = tx.Status is TransactionStatus.Pending or TransactionStatus.Failed;

            TransactionMetadata = metadata;
            Amount = amount;
            Fee = fee;
            Type = type;
            Description = GetDescription(
                type: Type,
                amount: Amount,
                fee: fee,
                decimals: config.Decimals,
                currencyCode: config.Name);
            Direction = Amount <= 0 ? "to " : "from ";

            IsReady = metadata != null;
        }

        public static string GetDescription(
            TransactionType type,
            decimal amount,
            decimal fee,
            int decimals,
            string currencyCode)
        {
            if (type.HasFlag(TransactionType.SwapPayment))
                return $"Swap payment {Math.Abs(amount).ToString("0." + new string('#', decimals))} {currencyCode}";

            if (type.HasFlag(TransactionType.SwapRefund))
                return $"Swap refund {Math.Abs(amount + fee).ToString("0." + new string('#', decimals))} {currencyCode}";

            if (type.HasFlag(TransactionType.SwapRedeem))
                return $"Swap redeem {Math.Abs(amount + fee).ToString("0." + new string('#', decimals))} {currencyCode}";

            if (type.HasFlag(TransactionType.TokenApprove))
                return "Token approve";

            if (type.HasFlag(TransactionType.TokenTransfer))
                return "Token transfer";

            if (type.HasFlag(TransactionType.ContractCall))
                return "Contract call";

            return amount switch
            {
                <= 0 => $"Sent {Math.Abs(amount + fee).ToString("0." + new string('#', decimals))} {currencyCode}",
                > 0 => $"Received {Math.Abs(amount).ToString("0." + new string('#', decimals))} {currencyCode}"
            };
        }

        protected virtual void DesignerMode()
        {
            Transaction = new EthereumTransaction
            {
                Id = "0x014749a05f3b9cee4ca637601a04511468025b762f8340d14b9e96c06728312f"
            };
            Amount = new Random().Next(-1000, 1000);
        }
    }
}