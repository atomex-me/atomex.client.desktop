using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class CollectiblesWalletViewModel : ViewModelBase, IWalletViewModel
    {
        private Action<TezosTokenViewModel> ShowTezosCollectible { get; }
        [ObservableAsProperty] public string Header { get; }
        [Reactive] public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public ObservableCollection<TezosTokenViewModel> InitialTokens { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        public TransactionViewModelBase? SelectedTransaction { get; set; }

        public CollectiblesWalletViewModel(Action<TezosTokenViewModel> showTezosCollectible)
        {
            ShowTezosCollectible =
                showTezosCollectible ?? throw new ArgumentNullException(nameof(showTezosCollectible));

            this.WhenAnyValue(vm => vm.Tokens)
                .WhereNotNull()
                .Where(tokens => tokens.Count > 0)
                .Select(tokens =>
                    $"{tokens.First().Contract.Name ?? tokens.First().Contract.Address.TruncateAddress()} collection")
                .ToPropertyExInMainThread(this, vm => vm.Header);

            this.WhenAnyValue(vm => vm.SearchPattern)
                .WhereNotNull()
                .SubscribeInMainThread(searchPattern =>
                    Tokens = new ObservableCollection<TezosTokenViewModel>(InitialTokens.Where(token =>
                        {
                            if (token.TokenBalance.Name != null)
                            {
                                return token.TokenBalance.Name.ToLower().Contains(searchPattern.ToLower()) ||
                                       token.TokenBalance.TokenId.ToString(CultureInfo.CurrentCulture)
                                           .Contains(searchPattern.ToLower());
                            }

                            return token.TokenBalance.TokenId.ToString(CultureInfo.CurrentCulture)
                                .Contains(searchPattern.ToLower());
                        })
                        .OrderByDescending(token => token.TotalAmount != 0)
                        .ThenBy(token => token.TokenBalance.Name)));
        }

        private ReactiveCommand<TezosTokenViewModel, Unit>? _onCollectibleClickCommand;
        public ReactiveCommand<TezosTokenViewModel, Unit> OnCollectibleClickCommand => _onCollectibleClickCommand ??=
            ReactiveCommand.Create<TezosTokenViewModel>(tezosTokenViewModel =>
                ShowTezosCollectible?.Invoke(tezosTokenViewModel));
    }
}