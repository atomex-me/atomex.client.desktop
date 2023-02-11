using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;

using Avalonia.Controls;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Network = NBitcoin.Network;

using Atomex.Blockchain.Bitcoin;
using Atomex.Client.Desktop.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.BitcoinBased;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class SelectOutputsViewModel : NavigatableSelectAddress
    {
        private BitcoinBasedAccount Account { get; }
        public BitcoinBasedConfig Config { get; set; }
        public Action<IEnumerable<BitcoinTxOutput>> ConfirmAction { get; set; }
        private ObservableCollection<OutputViewModel> InitialOutputs { get; set; }

        public SelectOutputsViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public SelectOutputsViewModel(
            IEnumerable<OutputViewModel> outputs,
            BitcoinBasedConfig config,
            BitcoinBasedAccount account)
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
            this.WhenAnyValue(vm => vm.SelectAll)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Where(_ => Outputs != null && !_checkedFromList)
                .Skip(1)
                .SubscribeInMainThread(selectAll =>
                {
                    Outputs = new ObservableCollection<OutputViewModel>(
                        Outputs.Select(output =>
                        {
                            output.IsSelected = selectAll;
                            return output;
                        }));

                    RecalculateTotalStats();
                });

            this.WhenAnyValue(vm => vm.Outputs)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(async outputViewModels =>
                {
                    var addresses = (await Account
                            .GetAddressesAsync())
                        .Where(address => outputViewModels.FirstOrDefault(o => o.Address == address.Address) != null)
                        .ToList();

                    var outputsWithAddresses = outputViewModels.Select((output, index) =>
                    {
                        var address = addresses.FirstOrDefault(a => a.Address == output.Address);
                        output.WalletAddress = address ?? null;
                        output.Id = index;
                        return output;
                    });

                    Outputs = new ObservableCollection<OutputViewModel>(
                        outputsWithAddresses.OrderByDescending(output => output.Balance));
                    InitialOutputs = new ObservableCollection<OutputViewModel>(Outputs);

                    RecalculateTotalStats();
                });

            this.WhenAnyValue(
                    vm => vm.SortByDate,
                    vm => vm.SortIsAscending,
                    vm => vm.SearchPattern)
                .SubscribeInMainThread(value =>
                {
                    var (sortByDate, sortIsAscending, searchPattern) = value;

                    if (Outputs == null) return;

                    var outputViewModels = new ObservableCollection<OutputViewModel>(
                        InitialOutputs!
                            .Where(output => output.Address.ToLower().Contains(searchPattern?.ToLower() ?? string.Empty)));

                    if (sortByDate)
                    {
                        var outputsList = outputViewModels.ToList();

                        outputsList.Sort(sortIsAscending
                            ? new KeyPathAscending<OutputViewModel>()
                            : new KeyPathDescending<OutputViewModel>());
 
                        Outputs = new ObservableCollection<OutputViewModel>(outputsList);
                    }
                    else
                    {
                        Outputs = new ObservableCollection<OutputViewModel>(sortIsAscending
                            ? outputViewModels.OrderBy(output => output.Balance)
                            : outputViewModels.OrderByDescending(output => output.Balance));
                    }
                });

            this.WhenAnyValue(vm => vm.TotalSelected)
                .WhereNotNull()
                .Select(totalSelected => $"{totalSelected} selected")
                .ToPropertyExInMainThread(this, vm => vm.TotalSelectedString);

            this.WhenAnyValue(vm => vm.TotalSelected)
                .WhereNotNull()
                .Where(_ => Config != null && Outputs != null)
                .Select(_ =>
                {
                    var totalCoinAmount = Outputs!.Aggregate(0m,
                        (result, output) => output.IsSelected ? result + output.Balance : result);

                    return $"Total {totalCoinAmount.ToString(Config!.Format, CultureInfo.CurrentCulture)} {Config.DisplayedName}";
                })
                .ToPropertyExInMainThread(this, vm => vm.TotalCoinAmountSelected);

            Account = account;
            Config = config;
            Outputs = new ObservableCollection<OutputViewModel>(outputs);

            SelectAll = Outputs.Aggregate(true, (res, flag) => res && flag.IsSelected);
        }

        [Reactive] public ObservableCollection<OutputViewModel> Outputs { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public bool SelectAll { get; set; }
        [Reactive] private bool SortIsAscending { get; set; }
        [Reactive] private bool SortByDate { get; set; }
        [Reactive] public int TotalSelected { get; set; }
        [ObservableAsProperty] public string TotalSelectedString { get; }
        [ObservableAsProperty] public string TotalCoinAmountSelected { get; }

        private void RecalculateTotalStats()
        {
            TotalSelected = Outputs.Aggregate(0, (result, output) => output.IsSelected ? result + 1 : result);
        }

        private bool _checkedFromList;

        private ReactiveCommand<Unit, Unit> _outputCheckCommand;

        public ReactiveCommand<Unit, Unit> OutputCheckCommand => _outputCheckCommand ??=
            (_outputCheckCommand = ReactiveCommand.Create(() =>
            {
                _checkedFromList = true;
                var selectionResult = Outputs.Aggregate(true, (result, output) => result && output.IsSelected);
                if (SelectAll != selectionResult)
                    SelectAll = selectionResult;

                RecalculateTotalStats();
            }));

        private ReactiveCommand<Unit, Unit> _selectAllCommand;
        public ReactiveCommand<Unit, Unit> SelectAllCommand => _selectAllCommand ??=
            (_selectAllCommand = ReactiveCommand.Create(() => { _checkedFromList = false; }));

        private ReactiveCommand<Unit, Unit> _closeCommand;
        public ReactiveCommand<Unit, Unit> CloseCommand => _closeCommand ??=
            (_closeCommand = ReactiveCommand.Create(() => { App.DialogService.Close(); }));

        private ReactiveCommand<Unit, Unit> _backCommand;
        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??=
            (_backCommand = ReactiveCommand.Create(() => { BackAction?.Invoke(); }));

        private ReactiveCommand<Unit, Unit> _changeSortTypeCommand;
        public ReactiveCommand<Unit, Unit> ChangeSortTypeCommand => _changeSortTypeCommand ??=
            (_changeSortTypeCommand = ReactiveCommand.Create(() => { SortByDate = !SortByDate; }));

        private ReactiveCommand<Unit, Unit> _changeSortDirectionCommand;
        public ReactiveCommand<Unit, Unit> ChangeSortDirectionCommand => _changeSortDirectionCommand ??=
            (_changeSortDirectionCommand = ReactiveCommand.Create(() => { SortIsAscending = !SortIsAscending; }));

        private ReactiveCommand<Unit, Unit> _confirmCommand;
        public ReactiveCommand<Unit, Unit> ConfirmCommand => _confirmCommand ??=
            (_confirmCommand =
                ReactiveCommand.Create(() =>
                {
                    ConfirmAction?.Invoke(Outputs
                        .Where(o => o.IsSelected)
                        .Select(o => o.Output)
                    );
                }));

        private ICommand _copyAddressCommand;
        public ICommand CopyAddressCommand =>
            _copyAddressCommand ??= (_copyAddressCommand = ReactiveCommand.Create((OutputViewModel output) =>
            {
                _ = App.Clipboard.SetTextAsync(output.Address);

                Outputs.ForEachDo(o => o.CopyButtonToolTip = OutputViewModel.DefaultCopyButtonToolTip);
                Outputs.First(o => o.Id == output.Id).CopyButtonToolTip =
                    OutputViewModel.CopiedButtonToolTip;
            }));

        private void DesignerMode()
        {
            var amount = new Money((decimal)0.9999, MoneyUnit.Satoshi);
            var script = BitcoinAddress.Create("muRDku2ZwNTz2msCZCHSUhDD5o6NxGsoXM", Network.TestNet).ScriptPubKey;

            var outputs = new List<BitcoinTxOutput>
            {
                new BitcoinTxOutput(
                    coin: new Coin(
                        fromTxHash: new uint256("19aa2187cda7610590d09dfab41ed4720f8570d7414b71b3dc677e237f72d4a1"),
                        fromOutputIndex: 123u,
                        amount: amount,
                        scriptPubKey: script),
                    confirmations: 5,
                    spentTxPoint: null,
                    spentConfirmations: 0),

                new BitcoinTxOutput(
                    coin: new Coin(
                        fromTxHash: new uint256("19aa2187cda7610590d09dfab41ed4720f8570d7414b71b3dc677e237f72d4a1"),
                        fromOutputIndex: 0u,
                        amount: amount,
                        scriptPubKey: script),
                    confirmations: 4,
                    spentTxPoint: null,
                    spentConfirmations: 0)
            };

            var btcCurrencyConfig = DesignTime.TestNetCurrencies
                .Where(currency => currency.Name == "BTC")
                .ToList()
                .First();

            Outputs = new ObservableCollection<OutputViewModel>(outputs.Select(output => new OutputViewModel
            {
                Output = output,
                Config = (BitcoinBasedConfig)btcCurrencyConfig
            }));
        }
    }

    public class OutputViewModel : ViewModelBase, IWalletAddressViewModel
    {
        public const string DefaultCopyButtonToolTip = "Copy address to clipboard";
        public const string CopiedButtonToolTip = "Successfully copied!";

        public OutputViewModel()
        {
            CopyButtonToolTip = DefaultCopyButtonToolTip;
        }

        public int Id { get; set; }
        [Reactive] public bool IsSelected { get; set; }
        [Reactive] public string CopyButtonToolTip { get; set; }
        public BitcoinTxOutput Output { get; set; }
        public BitcoinBasedConfig Config { get; set; }
        public WalletAddress? WalletAddress { get; set; }
        public decimal Balance => Config.SatoshiToCoin(Output.Value);
        public string BalanceString => $"{Balance.ToString(Config.Format, CultureInfo.CurrentCulture)} {Config.DisplayedName}";
        public string Address => Output.DestinationAddress(Config.Network);
    }
}