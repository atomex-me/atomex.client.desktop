using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;

using ReactiveUI;

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
        private readonly Action<ViewModelBase> ShowContent;
        private Action DoAfterAtomexClientChanged;

        public IEnumerable<WalletInfo> Wallets { get; set; }

        public MyWalletsViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public MyWalletsViewModel(
            IAtomexApp app,
            Action<ViewModelBase> showContent)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            Wallets = WalletInfo.AvailableWallets();
            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;

            ShowContent += showContent;
        }

        private ICommand _selectWalletCommand;
        public ICommand SelectWalletCommand => _selectWalletCommand ??= ReactiveCommand.Create<WalletInfo>(info =>
        {
            IAccount_OLD account = null;

            var unlockViewModel = new UnlockViewModel(
                walletName: info.Name,
                unlockAction: password =>
                {
                    var clientType = ClientType.Unknown;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) clientType = ClientType.AvaloniaWindows;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) clientType = ClientType.AvaloniaMac;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) clientType = ClientType.AvaloniaLinux;
                
                    account = Account_OLD.LoadFromFile(
                        pathToAccount: info.Path,
                        password: password,
                        currenciesProvider: AtomexApp.CurrenciesProvider,
                        clientType: clientType,
                        migrationCompleteCallback: (MigrationActionType actionType) =>
                        {
                            if (actionType == MigrationActionType.XtzTransactionsDeleted)
                            {
                                DoAfterAtomexClientChanged = TezosTransactionsDeleted;
                            }
                        });
                },
                goBack: () => ShowContent(this),
                onUnlock: () =>
                {
                    var atomexClient = new WebSocketAtomexClient_OLD(
                        configuration: App.Configuration,
                        account: account,
                        symbolsProvider: AtomexApp.SymbolsProvider);

                    AtomexApp.UseAtomexClient(atomexClient, restart: true);
                });

            ShowContent?.Invoke(unlockViewModel);
        });

        private void TezosTransactionsDeleted()
        {
            var xtzCurrencies = new[] {"XTZ", "TZBTC", "KUSD"};
            App.DialogService.Show(new RestoreDialogViewModel(AtomexApp, xtzCurrencies));
        }

        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            var atomexClient = args.AtomexClient;

            if (atomexClient?.Account == null)
            {
                AtomexApp.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
                return;
            }

            DoAfterAtomexClientChanged?.Invoke();
        }

        private void DesignerMode()
        {
            Wallets = new List<WalletInfo>
            {
                new WalletInfo {Name = "default", Path = "wallets/default/", Network = Network.MainNet},
                new WalletInfo {Name = "market_maker", Path = "wallets/marketmaker/", Network = Network.MainNet},
                new WalletInfo {Name = "wallet1", Path = "wallets/default/", Network = Network.TestNet},
                new WalletInfo {Name = "my_first_wallet", Path = "wallets/marketmaker/", Network = Network.TestNet},
                new WalletInfo {Name = "mega_wallet", Path = "wallets/marketmaker/", Network = Network.MainNet}
            };
        }
    }
}