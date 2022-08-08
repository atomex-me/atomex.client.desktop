using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class CollectiblesWalletViewModel : ViewModelBase, IWalletViewModel
    {
        private Action<TezosTokenViewModel> ShowTezosCollectible { get; }
        [ObservableAsProperty] public string Header { get; }
        [Reactive] public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public TransactionViewModelBase? SelectedTransaction { get; set; }

        public CollectiblesWalletViewModel(Action<TezosTokenViewModel> showTezosCollectible)
        {
            ShowTezosCollectible =
                showTezosCollectible ?? throw new ArgumentNullException(nameof(showTezosCollectible));

            this.WhenAnyValue(vm => vm.Tokens)
                .WhereNotNull()
                .Select(tokens =>
                    $"{tokens.First().Contract.Name ?? tokens.First().Contract.Address.TruncateAddress()} collection")
                .ToPropertyExInMainThread(this, vm => vm.Header);
        }

        private ReactiveCommand<TezosTokenViewModel, Unit>? _onCollectibleClickCommand;

        public ReactiveCommand<TezosTokenViewModel, Unit> OnCollectibleClickCommand => _onCollectibleClickCommand ??=
            ReactiveCommand.Create<TezosTokenViewModel>(tezosTokenViewModel =>
                ShowTezosCollectible?.Invoke(tezosTokenViewModel));
    }
}