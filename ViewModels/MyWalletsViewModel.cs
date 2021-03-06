using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Atomex.Client.Desktop.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.LiteDb;
using Atomex.Services;

using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class MyWalletsViewModel : ViewModelBase
    {
        private IAtomexApp AtomexApp { get; }

        public IEnumerable<WalletInfo> Wallets { get; set; }

        public MyWalletsViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public MyWalletsViewModel(
            IAtomexApp app, Action<ViewModelBase> showContent)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            Wallets = WalletInfo.AvailableWallets();
            AtomexApp.TerminalChanged += OnTerminalChangedEventHandler;

            ShowContent += showContent;
        }

        private Action<ViewModelBase> ShowContent;

        private ICommand _selectWalletCommand;

        private Action DoAfterTerminalChanged;


        public ICommand SelectWalletCommand => _selectWalletCommand ??= ReactiveCommand.Create<WalletInfo>(info =>
        {
            IAccount account = null;

            var unlockViewModel = new UnlockViewModel(info.Name, password =>
            {
                account = Account.LoadFromFile(
                    pathToAccount: info.Path,
                    password: password,
                    currenciesProvider: AtomexApp.CurrenciesProvider,
                    clientType: ClientType.Unknown,
                    migrationCompleteCallback: (MigrationActionType actionType) =>
                    {
                        if (actionType == MigrationActionType.XtzTransactionsDeleted)
                        {
                            DoAfterTerminalChanged = TezosTransactionsDeleted;
                        }
                    });
            }, () => ShowContent(this));

            unlockViewModel.Unlocked += (sender, args) =>
            {
                var atomexClient = new WebSocketAtomexClient(
                    configuration: App.Configuration,
                    account: account,
                    symbolsProvider: AtomexApp.SymbolsProvider,
                    quotesProvider: AtomexApp.QuotesProvider);

                AtomexApp.UseTerminal(atomexClient, restart: true);
            };


            ShowContent?.Invoke(unlockViewModel);
        });

        private void TezosTransactionsDeleted()
        {
            var xtzCurrencies = new[] {"XTZ", "TZBTC", "KUSD"};
            App.DialogService.Show(new RestoreDialogViewModel(AtomexApp, xtzCurrencies));
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs args)
        {
            var terminal = args.Terminal;

            if (terminal?.Account == null)
            {
                AtomexApp.TerminalChanged -= OnTerminalChangedEventHandler;
                return;
            }

            DoAfterTerminalChanged?.Invoke();
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