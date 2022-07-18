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
        // private readonly IAtomexApp _app;
        private readonly TezosConfig TezosConfig;
        [Reactive] public TezosTokenViewModel Collectible { get; set; }
        public string ContractExplorerUri => $"{TezosConfig.AddressExplorerUri}{Collectible.Contract.Address}";

        public CollectibleWalletViewModel(IAtomexApp app, Action<ViewModelBase?> showRightPopupContent)
            : base(app: app, showRightPopupContent)
        {
            // _app = app ?? throw new ArgumentNullException(nameof(app));

            TezosConfig = app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);
        }


        private ReactiveCommand<Unit, Unit>? _openInExplorerCommand;

        public ReactiveCommand<Unit, Unit> OpenInExplorerCommand => _openInExplorerCommand ??= ReactiveCommand.Create(
            () =>
            {
                if (Uri.TryCreate(ContractExplorerUri, UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
            });
    }
}