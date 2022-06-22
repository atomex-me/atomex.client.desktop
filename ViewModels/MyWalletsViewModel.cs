using System;
using System.Collections.Generic;
using System.Reactive;
using System.Runtime.InteropServices;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
        private IAtomexApp AtomexApp { get; }
        private readonly Action<ViewModelBase> _showContent;
        private Action _doAfterAtomexClientChanged;

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
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            Wallets = WalletInfo.AvailableWallets();

            this.WhenAnyValue(vm => vm.SelectedWallet)
                .WhereNotNull()
                .InvokeCommandInMainThread(SelectWalletCommand);

            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;

            _showContent += showContent;
        }

        private ReactiveCommand<WalletInfo, Unit> _selectWalletCommand;

        public ReactiveCommand<WalletInfo, Unit> SelectWalletCommand => _selectWalletCommand ??=
            ReactiveCommand.Create<WalletInfo>(OnSelectWallet);

        private void OnSelectWallet(WalletInfo info)
        {
            IAccount account = null;

            var unlockViewModel = new UnlockViewModel(
                walletName: info.Name,
                unlockAction: password =>
                {
                    var clientType = ClientType.Unknown;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        clientType = ClientType.AvaloniaWindows;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        clientType = ClientType.AvaloniaMac;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        clientType = ClientType.AvaloniaLinux;

                    account = Account.LoadFromFile(
                        pathToAccount: info.Path,
                        password: password,
                        currenciesProvider: AtomexApp.CurrenciesProvider,
                        clientType: clientType,
                        migrationCompleteCallback: actionType =>
                        {
                            _doAfterAtomexClientChanged = actionType switch
                            {
                                MigrationActionType.XtzTransactionsDeleted => TezosTransactionsDeleted,
                                MigrationActionType.XtzTokensDataDeleted => OnTezosTokensDataDeleted,
                                _ => _doAfterAtomexClientChanged
                            };
                        });
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
                        account: account,
                        symbolsProvider: AtomexApp.SymbolsProvider);

                    AtomexApp.UseAtomexClient(atomexClient, restart: true);
                });

            _showContent?.Invoke(unlockViewModel);
        }

        private void TezosTransactionsDeleted()
        {
            var xtzCurrencies = new[] { "XTZ", "TZBTC", "KUSD", "USDT_XTZ" };
            var restoreDialogViewModel = new RestoreDialogViewModel(AtomexApp);
            restoreDialogViewModel.ScanCurrenciesAsync(xtzCurrencies);
        }

        private void OnTezosTokensDataDeleted()
        {
            var restoreDialogViewModel = new RestoreDialogViewModel(AtomexApp);
            restoreDialogViewModel.ScanTezosTokens();
        }

        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            var atomexClient = args.AtomexClient;

            if (atomexClient?.Account == null)
            {
                AtomexApp.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
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