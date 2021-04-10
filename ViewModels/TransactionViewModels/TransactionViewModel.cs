using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.ViewModels;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;
using ReactiveUI;
using Serilog;
using Avalonia;
using Avalonia.Input.Platform;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class TransactionViewModel : ViewModelBase, IExpandable
    {
        public event EventHandler<TransactionEventArgs> UpdateClicked;
        public event EventHandler<TransactionEventArgs> RemoveClicked;

        public IBlockchainTransaction Transaction { get; }
        public string Id { get; set; }
        public Currency Currency { get; set; }
        public BlockchainTransactionState State { get; set; }
        public BlockchainTransactionType Type { get; set; }

        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string AmountFormat { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Fee { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();
        public string TxExplorerUri => $"{Currency.TxExplorerUri}{Id}";
        public bool CanBeRemoved { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; this.RaisePropertyChanged(nameof(IsExpanded)); }
        }

        public TransactionViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TransactionViewModel(IBlockchainTransaction tx, decimal amount, decimal fee)
        {
            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            Id = Transaction.Id;
            Currency = Transaction.Currency;
            State = Transaction.State;
            Type = Transaction.Type;
            Amount = amount;

            var netAmount = amount + fee;

            var currencyViewModel = CurrencyViewModelCreator.CreateViewModel(tx.Currency, false);
            AmountFormat = currencyViewModel.CurrencyFormat;
            CurrencyCode = currencyViewModel.CurrencyCode;
            Time = tx.CreationTime ?? DateTime.UtcNow;
            CanBeRemoved = tx.State == BlockchainTransactionState.Unknown ||
                           tx.State == BlockchainTransactionState.Failed ||
                           tx.State == BlockchainTransactionState.Pending ||
                           tx.State == BlockchainTransactionState.Unconfirmed;

            if (tx.Type.HasFlag(BlockchainTransactionType.SwapPayment))
            {
                Description = $"Swap payment {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.SwapRefund))
            {
                Description = $"Swap refund {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.SwapRedeem))
            {
                Description = $"Swap redeem {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.TokenApprove))
            {
                Description = $"Token approve";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.TokenCall))
            {
                Description = $"Token call";
            }
            else if (tx.Type.HasFlag(BlockchainTransactionType.SwapCall))
            {
                Description = $"Token swap call";
            }
            else if (Amount <= 0) //tx.Type.HasFlag(BlockchainTransactionType.Output))
            {
                Description = $"Sent {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else if (Amount > 0) //tx.Type.HasFlag(BlockchainTransactionType.Input)) // has outputs
            {
                Description = $"Received {Math.Abs(netAmount).ToString("0." + new String('#', tx.Currency.Digits))} {tx.Currency.Name}";
            }
            else
            {
                Description = "Unknown transaction";
            }
        }

        private ICommand _openTxInExplorerCommand;
        public ICommand OpenTxInExplorerCommand => _openTxInExplorerCommand ??= (_openTxInExplorerCommand = ReactiveCommand.Create<string>((id) =>
        {
            if (Uri.TryCreate($"{Currency.TxExplorerUri}{id}", UriKind.Absolute, out var uri))      
                App.OpenBrowser(uri.ToString());
            else
                Log.Error("Invalid uri for transaction explorer");
        }));

        private ICommand _openAddressInExplorerCommand;
        public ICommand OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??= (_openAddressInExplorerCommand = ReactiveCommand.Create<string>((address) =>
        {
            if (Uri.TryCreate($"{Currency.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                App.OpenBrowser(uri.ToString());
            else
                Log.Error("Invalid uri for address explorer");
        }));

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= (_copyCommand = ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                var clipboard = AvaloniaLocator.Current.GetService<IClipboard>();
                clipboard?.SetTextAsync(s);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        }));

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ??= (_updateCommand = ReactiveCommand.Create(() =>
        {
            UpdateClicked?.Invoke(this, new TransactionEventArgs(Transaction));
        }));

        private ICommand _removeCommand;
        public ICommand RemoveCommand => _removeCommand ??= (_removeCommand = ReactiveCommand.Create(() =>
        {
            RemoveClicked?.Invoke(this, new TransactionEventArgs(Transaction));
        }));

        private void DesignerMode()
        {
            Id = "1234567890abcdefgh1234567890abcdefgh";
            Time = DateTime.UtcNow;
        }
    }
}