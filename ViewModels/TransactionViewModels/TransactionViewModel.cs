﻿using System;
using System.Reactive;

using Avalonia.Controls;
using ReactiveUI;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class TransactionViewModelBase : ViewModelBase
    {
        public event EventHandler<TransactionEventArgs> UpdateClicked;
        public event EventHandler<TransactionEventArgs> RemoveClicked;
        public string TxExplorerUri => $"{Currency.TxExplorerUri}{Id}";
        public decimal Amount { get; set; }
        public string AmountFormat { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
        public CurrencyConfig Currency { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();
        public TransactionStatus State { get; set; }
        public DateTime Time { get; set; }
        public ITransaction Transaction { get; set; }
        public TransactionType Type { get; set; }
        public Action? OnClose { get; set; }
        public bool CanBeRemoved { get; set; }

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

        protected void DesignerMode()
        {
            var random = new Random();
            Id = "1234567890abcdefgh1234567890abcdefgh";
            Time = DateTime.UtcNow;
            Amount = random.Next(-1000, 1000);
        }
    }

    public class TransactionViewModel : TransactionViewModelBase
    {
        public string CurrencyCode { get; set; }
        public string FeeCode { get; set; }
        public decimal Fee { get; set; }
        public string Direction { get; set; }

        public TransactionViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public TransactionViewModel(
            ITransaction tx,
            CurrencyConfig currencyConfig,
            decimal amount,
            decimal fee)
        {
            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            Id = Transaction.Id;
            Currency = currencyConfig;
            State = Transaction.Status;
            Type = Transaction.Type;
            Amount = amount;
            FeeCode = currencyConfig.FeeCode;

            var netAmount = amount + fee;

            var currencyViewModel = CurrencyViewModelCreator.CreateOrGet(currencyConfig, false);

            AmountFormat = currencyViewModel.CurrencyFormat;
            CurrencyCode = currencyViewModel.CurrencyCode;
            Time = tx.CreationTime ?? DateTime.UtcNow;
            CanBeRemoved = tx.Status is TransactionStatus.Unknown or
                TransactionStatus.Failed or
                TransactionStatus.Pending or
                TransactionStatus.Unconfirmed;

            Description = GetDescription(
                type: tx.Type,
                amount: Amount,
                netAmount: netAmount,
                amountDigits: currencyConfig.Decimals,
                currencyCode: currencyConfig.Name);

            Direction = Amount switch
            {
                <= 0 => "to ",
                > 0 => "from "
            };
        }

        public static string GetDescription(
            TransactionType type,
            decimal amount,
            decimal netAmount,
            int amountDigits,
            string currencyCode)
        {
            if (type.HasFlag(TransactionType.SwapPayment))
                return $"Swap payment {Math.Abs(amount).ToString("0." + new string('#', amountDigits))} {currencyCode}";

            if (type.HasFlag(TransactionType.SwapRefund))
                return $"Swap refund {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";

            if (type.HasFlag(TransactionType.SwapRedeem))
                return $"Swap redeem {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}";

            if (type.HasFlag(TransactionType.TokenApprove))
                return "Token approve";

            if (type.HasFlag(TransactionType.TokenCall))
                return "Token call";

            if (type.HasFlag(TransactionType.TokenTransfer))
                return "Token transfer";

            if (type.HasFlag(TransactionType.ContractCall))
                return "Contract call";

            return amount switch
            {
                <= 0 => $"Sent {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}",
                > 0 => $"Received {Math.Abs(netAmount).ToString("0." + new string('#', amountDigits))} {currencyCode}"
            };
        }
    }
}