using System;
using System.IO;
using System.Collections.Generic;
using System.Reactive;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.LiteDb;
using Atomex.Services;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Desktop.ViewModels
{
    public class MyWalletsViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        private readonly Action<ViewModelBase> _showContent;
        private Action? _doAfterAtomexClientChanged;

        public IEnumerable<WalletInfo> Wallets { get; set; }
        [Reactive] public WalletInfo? SelectedWallet { get; set; }

        public MyWalletsViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public MyWalletsViewModel(
            IAtomexApp app,
            Action<ViewModelBase> showContent)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            Wallets = WalletInfo.AvailableWallets();

            this.WhenAnyValue(vm => vm.SelectedWallet)
                .WhereNotNull()
                .InvokeCommandInMainThread(SelectWalletCommand);

            _app.AtomexClientChanged += OnAtomexClientChangedEventHandler;

            _showContent += showContent;
        }

        private ReactiveCommand<WalletInfo, Unit> _selectWalletCommand;
        public ReactiveCommand<WalletInfo, Unit> SelectWalletCommand => _selectWalletCommand ??=
            ReactiveCommand.Create<WalletInfo>(OnSelectWallet);

        private void OnSelectWallet(WalletInfo info)
        {
            IAccount account = null;
            ILocalStorage localStorage = null;

            var unlockViewModel = new UnlockViewModel(
                walletName: info.Name,
                unlockAction: password =>
                {
                    var wallet = HdWallet.LoadFromFile(info.Path, password);

                    localStorage = new LiteDbLocalStorage(
                        pathToDb: Path.Combine(Path.GetDirectoryName(wallet.PathToWallet), Account.DefaultDataFileName),
                        password: password,
                        currencies: _app.CurrenciesProvider.GetCurrencies(wallet.Network),
                        network: wallet.Network,
                        migrationComplete: actionType =>
                        {
                            _doAfterAtomexClientChanged = actionType switch
                            {
                                MigrationActionType.XtzTransactionsDeleted => TezosTransactionsDeleted,
                                MigrationActionType.XtzTokensDataDeleted => OnTezosTokensDataDeleted,
                                _ => null
                            };
                        });

                    account = new Account(
                        wallet: wallet,
                        localStorage: localStorage,
                        currenciesProvider: _app.CurrenciesProvider);
                },
                goBack: () =>
                {
                    _showContent(this);
                    SelectedWallet = null;
                },
                onUnlock: () =>
                {
                    var atomexClient = new WebSocketAtomexClientLegacy(
                        exchangeUrl: App.Configuration[$"Services:{account!.Network}:Exchange:Url"],
                        marketDataUrl: App.Configuration[$"Services:{account!.Network}:MarketData:Url"],
                        clientType: PlatformHelper.GetClientType(),
                        authMessageSigner: account.DefaultAuthMessageSigner());

                    _app.ChangeAtomexClient(
                        atomexClient: atomexClient,
                        account: account,
                        localStorage: localStorage,
                        restart: true);
                });

            _showContent?.Invoke(unlockViewModel);
        }

        private void TezosTransactionsDeleted()
        {
            var xtzCurrencies = new[] { "XTZ", "TZBTC", "KUSD", "USDT_XTZ" };
            var restoreDialogViewModel = new RestoreDialogViewModel(_app);
            restoreDialogViewModel.ScanCurrenciesAsync(xtzCurrencies);
        }

        private void OnTezosTokensDataDeleted()
        {
            var restoreDialogViewModel = new RestoreDialogViewModel(_app);
            restoreDialogViewModel.ScanTezosTokens();
        }

        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            if (_app != null && _app?.AtomexClient == null)
            {
                _app!.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
                return;
            }

            _doAfterAtomexClientChanged?.Invoke();
        }

        private void DesignerMode()
        {
            Wallets = new List<WalletInfo>
            {
                new WalletInfo { Name = "default", Path = "wallets/default/", Network = Network.MainNet },
                new WalletInfo { Name = "market_maker", Path = "wallets/marketmaker/", Network = Network.MainNet },
                new WalletInfo { Name = "wallet1", Path = "wallets/default/", Network = Network.TestNet },
                new WalletInfo { Name = "my_first_wallet", Path = "wallets/marketmaker/", Network = Network.TestNet },
                new WalletInfo { Name = "mega_wallet", Path = "wallets/marketmaker/", Network = Network.MainNet }
            };
        }
    }
}