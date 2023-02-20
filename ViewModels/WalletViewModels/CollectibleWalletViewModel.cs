using System;
using System.Reactive;

using ReactiveUI;

using Atomex.Client.Desktop.ViewModels.SendViewModels;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class CollectibleWalletViewModel : TezosTokenWalletViewModel
    {
        private readonly TezosConfig _tezosConfig;

        public string TokenExplorerUri =>
            $"{_tezosConfig.AddressExplorerUri}{TokenViewModel?.Contract.Address}/tokens/{TokenViewModel?.TokenBalance.TokenId}";

        public CollectibleWalletViewModel(IAtomexApp app, Action<ViewModelBase?> showRightPopupContent)
            : base(app: app, showRightPopupContent)
        {
            _tezosConfig = app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);
        }

        protected override void OnSendClick()
        {
            if (TokenViewModel == null) return;

            var sendViewModel = new CollectibleSendViewModel(
                app: _app,
                tokenContract: TokenViewModel.Contract.Address,
                tokenId: (int)TokenViewModel.TokenBalance.TokenId,
                tokenType: TokenViewModel.Contract.GetContractType(),
                previewUrl: TokenViewModel.CollectiblePreviewUrl,
                collectibleName: TokenViewModel.TokenBalance.Name,
                from: null);

            App.DialogService.Show(sendViewModel.SelectFromViewModel);
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