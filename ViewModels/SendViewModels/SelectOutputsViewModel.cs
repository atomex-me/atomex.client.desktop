using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Atomex.Blockchain.BitcoinBased;
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
        public IEnumerable<OutputViewModel> Outputs { get; set; }


        public SelectOutputsViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
            this.WhenAnyValue(vm => vm.DeselectAll)
                .Subscribe(deselectAll =>
                {
                    foreach (var outputViewModel in Outputs)
                    {
                        outputViewModel.IsSelected = deselectAll;
                    }
                });
        }

        [Reactive] public bool DeselectAll { get; set; }
        [Reactive] public bool SortIsAscending { get; set; }

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

            // Outputs = new ObservableCollection<BitcoinBasedTxOutput>(outputs);
        }
    }

    public class OutputViewModel : ViewModelBase
    {
        [Reactive] public bool IsSelected { get; set; }
        public BitcoinBasedTxOutput Output { get; set; }
        public BitcoinBasedConfig Config { get; set; }

        public string Balance =>
            Config.SatoshiToCoin(Output.Value).ToString(Config.Format, CultureInfo.InvariantCulture);

        public string Address => Output.DestinationAddress(Config.Network);


        private ReactiveCommand<Unit, Unit> _copyCommand;

        public ReactiveCommand<Unit, Unit> CopyCommand =>
            _copyCommand ??= (_copyCommand = ReactiveCommand.CreateFromTask(() => App.Clipboard.SetTextAsync(Address)));
    }
}