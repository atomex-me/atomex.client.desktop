using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Desktop.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet.BitcoinBased;
using Avalonia.Controls;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Network = NBitcoin.Network;


namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class SelectOutputsViewModel : ViewModelBase
    {
        private BitcoinBasedAccount Account { get; }
        public Action BackAction { get; set; }
        public Action<IEnumerable<BitcoinBasedTxOutput>> ConfirmAction { get; set; }
        public BitcoinBasedConfig Config { get; set; }

        private ObservableCollection<OutputViewModel> InitialOutputs { get; set; }

        public SelectOutputsViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public SelectOutputsViewModel(
            IEnumerable<BitcoinBasedTxOutput> outputs,
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
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(selectAll =>
                {
                    Outputs = new ObservableCollection<OutputViewModel>(
                        Outputs.Select(output =>
                        {
                            output.IsSelected = selectAll;
                            return output;
                        }));

                    RecalculateTotalStats();
                });

            this.WhenAnyValue(vm => vm.SortIsAscending)
                .Where(_ => Outputs != null)
                .Subscribe(sortIsAscending =>
                {
                    Outputs = sortIsAscending
                        ? new ObservableCollection<OutputViewModel>(
                            Outputs.OrderBy(output => output.BalanceString))
                        : new ObservableCollection<OutputViewModel>(
                            Outputs.OrderByDescending(output => output.BalanceString));
                });

            this.WhenAnyValue(vm => vm.Outputs)
                .WhereNotNull()
                .Take(1)
                .Subscribe(async outputs =>
                {
                    var addresses = (await Account
                            .GetAddressesAsync())
                        .Where(address => outputs.FirstOrDefault(o => o.Address == address.Address) != null)
                        .ToList();

                    var outputsWithAddresses = outputs.Select(output =>
                    {
                        var address = addresses.FirstOrDefault(a => a.Address == output.Address);
                        output.WalletAddress = address ?? null;
                        return output;
                    });

                    Outputs = new ObservableCollection<OutputViewModel>(
                        outputsWithAddresses.OrderByDescending(output => output.BalanceString));

                    // addresses.Sort((a1, a2) =>
                    // {
                    //     var typeResult = a1.KeyType.CompareTo(a2.KeyType);
                    //
                    //     if (typeResult != 0)
                    //         return typeResult;
                    //
                    //     var accountResult = a1.KeyIndex.Account.CompareTo(a2.KeyIndex.Account);
                    //
                    //     if (accountResult != 0)
                    //         return accountResult;
                    //
                    //     var chainResult = a1.KeyIndex.Chain.CompareTo(a2.KeyIndex.Chain);
                    //
                    //     return chainResult != 0
                    //         ? chainResult
                    //         : a1.KeyIndex.Index.CompareTo(a2.KeyIndex.Index);
                    // }

                    InitialOutputs = new ObservableCollection<OutputViewModel>(Outputs);
                    RecalculateTotalStats();
                });

            this.WhenAnyValue(vm => vm.SearchPattern)
                .Where(_ => Outputs != null)
                .Subscribe(searchPattern =>
                {
                    Outputs = SortIsAscending
                        ? new ObservableCollection<OutputViewModel>(
                            InitialOutputs
                                .OrderBy(output => output.BalanceString)
                                .Where(output => output.Address.ToLower().Contains(searchPattern.ToLower()))
                        )
                        : new ObservableCollection<OutputViewModel>(
                            InitialOutputs
                                .OrderByDescending(output => output.BalanceString)
                                .Where(output => output.Address.ToLower().Contains(searchPattern.ToLower()))
                        );
                });

            Account = account;
            Config = config;
            Outputs = new ObservableCollection<OutputViewModel>(
                outputs.Select(output => new OutputViewModel
                {
                    Output = output,
                    Config = config
                }));

            SelectAll = true;
        }

        [Reactive] public ObservableCollection<OutputViewModel> Outputs { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public bool SelectAll { get; set; }
        [Reactive] public bool SortIsAscending { get; set; }
        [Reactive] public string TotalSelected { get; set; }
        [Reactive] public string TotalCoinAmountSelected { get; set; }

        private void RecalculateTotalStats()
        {
            Task.Run(() =>
            {
                TotalSelected =
                    $"{Outputs.Aggregate(0, (result, output) => output.IsSelected ? result + 1 : result)} selected";

                var totalCoinAmount = Outputs.Aggregate(0m,
                    (result, output) => output.IsSelected ? result + output.Balance : result);

                TotalCoinAmountSelected =
                    $"Total {totalCoinAmount.ToString(Config.Format, CultureInfo.InvariantCulture)} {Config.Name}";
            });
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
            (_closeCommand = ReactiveCommand.Create(() => { Desktop.App.DialogService.Close(); }));

        private ReactiveCommand<Unit, Unit> _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??=
            (_backCommand = ReactiveCommand.Create(() => { BackAction?.Invoke(); }));

        private ReactiveCommand<Unit, Unit> _changeSortTypeCommand;

        public ReactiveCommand<Unit, Unit> ChangeSortTypeCommand => _changeSortTypeCommand ??=
            (_changeSortTypeCommand = ReactiveCommand.Create(() => { SortIsAscending = !SortIsAscending; }));

        private ReactiveCommand<Unit, Unit> _confirmCommand;

        public ReactiveCommand<Unit, Unit> ConfirmCommand => _confirmCommand ??=
            (_confirmCommand = ReactiveCommand.Create(() =>
            {
                ConfirmAction?.Invoke(Outputs.Where(o => o.IsSelected).Select(o => o.Output));
            }));


        private ICommand _copyAddressCommand;

        public ICommand CopyAddressCommand =>
            _copyAddressCommand ??= (_copyAddressCommand = ReactiveCommand.Create((string address) =>
            {
                _ = Desktop.App.Clipboard.SetTextAsync(address);

                Outputs.ForEachDo(output => output.CopyButtonToolTip = OutputViewModel.DefaultCopyButtonToolTip);
                Outputs.First(output => output.Address == address).CopyButtonToolTip =
                    OutputViewModel.CopiedButtonToolTip;
            }));

        private void DesignerMode()
        {
            var amount = new Money((decimal)0.9999, MoneyUnit.Satoshi);
            var script = BitcoinAddress.Create("muRDku2ZwNTz2msCZCHSUhDD5o6NxGsoXM", Network.TestNet).ScriptPubKey;

            var outputs = new List<BitcoinBasedTxOutput>
            {
                new BitcoinBasedTxOutput(
                    coin: new Coin(
                        fromTxHash: new uint256("19aa2187cda7610590d09dfab41ed4720f8570d7414b71b3dc677e237f72d4a1"),
                        fromOutputIndex: 123u,
                        amount: amount,
                        scriptPubKey: script),
                    spentTxPoint: null),

                new BitcoinBasedTxOutput(
                    coin: new Coin(
                        fromTxHash: new uint256("19aa2187cda7610590d09dfab41ed4720f8570d7414b71b3dc677e237f72d4a1"),
                        fromOutputIndex: 0u,
                        amount: amount,
                        scriptPubKey: script),
                    spentTxPoint: null)
            };

            var btcCurrencyConfig = DesignTime.Currencies
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

    public class OutputViewModel : ViewModelBase
    {
        public const string DefaultCopyButtonToolTip = "Copy address to clipboard";
        public const string CopiedButtonToolTip = "Successfully copied!";

        public OutputViewModel()
        {
            CopyButtonToolTip = DefaultCopyButtonToolTip;
        }

        [Reactive] public bool IsSelected { get; set; }
        [Reactive] public string CopyButtonToolTip { get; set; }
        public BitcoinBasedTxOutput Output { get; set; }
        public BitcoinBasedConfig Config { get; set; }
        public WalletAddress WalletAddress { get; set; }
        public decimal Balance => Config.SatoshiToCoin(Output.Value);
        public string BalanceString => $"{Balance.ToString(Config.Format, CultureInfo.InvariantCulture)} {Config.Name}";
        public string Address => Output.DestinationAddress(Config.Network);
    }
}