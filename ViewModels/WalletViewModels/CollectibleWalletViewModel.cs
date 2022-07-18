using System;
using System.Reactive;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class CollectibleWalletViewModel : WalletViewModel
    {
        private readonly TezosConfig _tezosConfig;
        [Reactive] public TezosTokenViewModel Collectible { get; set; }
        public string ContractExplorerUri => $"{_tezosConfig.AddressExplorerUri}{Collectible.Contract.Address}";

        public CollectibleWalletViewModel(IAtomexApp app, Action<ViewModelBase?> showRightPopupContent)
            : base(app: app, showRightPopupContent)
        {
            _tezosConfig = app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);
        }


        private ReactiveCommand<string, Unit>? _openInExplorerCommand;

        public ReactiveCommand<string, Unit> OpenInExplorerCommand => _openInExplorerCommand ??= 
            ReactiveCommand.Create<string>(address =>
            {
                if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
            });
    }
}