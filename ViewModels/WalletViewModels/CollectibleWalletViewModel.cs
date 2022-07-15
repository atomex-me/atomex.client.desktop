using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class CollectibleWalletViewModel : WalletViewModel
    {
        [Reactive] public TezosTokenViewModel Collectible { get; set; }
    }
}