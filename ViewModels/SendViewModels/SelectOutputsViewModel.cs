using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Avalonia.Controls;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class SelectOutputsViewModel : ViewModelBase
    {
        public Action BackAction { get; set; }
        [Reactive] public ObservableCollection<OutputViewModel> Outputs { get; set; }

        public SelectOutputsViewModel()
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
                .Subscribe(outputs =>
                    Outputs = new ObservableCollection<OutputViewModel>(
                        outputs.OrderByDescending(output => output.BalanceString)));
        }

        [Reactive] public bool SelectAll { get; set; }
        [Reactive] public bool SortIsAscending { get; set; }

        private bool _checkedFromList;

        private ICommand _outputCheckCommand;

        public ICommand OutputCheckCommand => _outputCheckCommand ??=
            (_outputCheckCommand = ReactiveCommand.Create(() =>
            {
                _checkedFromList = true;
                var selectionResult = Outputs.Aggregate(true, (result, output) => result && output.IsSelected);
                if (SelectAll != selectionResult)
                    SelectAll = selectionResult;
            }));

        private ICommand _selectAllCommand;

        public ICommand SelectAllCommand => _selectAllCommand ??=
            (_selectAllCommand = ReactiveCommand.Create(() => { _checkedFromList = false; }));

        private ICommand _closeCommand;

        public ICommand CloseCommand => _closeCommand ??=
            (_closeCommand = ReactiveCommand.Create(() => { Desktop.App.DialogService.Close(); }));

        private ICommand _backCommand;

        public ICommand BackCommand => _backCommand ??=
            (_backCommand = ReactiveCommand.Create(() => { BackAction?.Invoke(); }));

        private ReactiveCommand<Unit, Unit> _changeSortTypeCommand;

        public ReactiveCommand<Unit, Unit> ChangeSortTypeCommand => _changeSortTypeCommand ??=
            (_changeSortTypeCommand = ReactiveCommand.Create(() => { SortIsAscending = !SortIsAscending; }));

        private void DesignerMode()
        {
            var amount = new Money((decimal) 0.9999, MoneyUnit.Satoshi);
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
                Config = (BitcoinBasedConfig) btcCurrencyConfig
            }));
        }
    }

    public class OutputViewModel : ViewModelBase
    {
        [Reactive] public bool IsSelected { get; set; }
        public BitcoinBasedTxOutput Output { get; set; }
        public BitcoinBasedConfig Config { get; set; }

        private decimal Balance => Config.SatoshiToCoin(Output.Value);
        public string BalanceString => Balance.ToString(Config.Format, CultureInfo.InvariantCulture);

        public string Address => Output.DestinationAddress(Config.Network);


        private ReactiveCommand<Unit, Unit> _copyCommand;

        public ReactiveCommand<Unit, Unit> CopyCommand =>
            _copyCommand ??= (_copyCommand = ReactiveCommand.CreateFromTask(() => App.Clipboard.SetTextAsync(Address)));
    }
}