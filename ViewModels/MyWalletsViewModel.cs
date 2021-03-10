using System;
using System.Collections.Generic;
using System.Windows.Input;
using Atomex.Client.Desktop.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.Subsystems;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using ReactiveUI;

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
            IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            Wallets = WalletInfo.AvailableWallets();
        }

        private ICommand _selectWalletCommand;


        public ICommand SelectWalletCommand => _selectWalletCommand ??= ReactiveCommand.Create<WalletInfo>(info =>
        {
            IAccount account = null;
            Console.WriteLine(info.Name);

            // var unlockViewModel = new UnlockViewModel(info.Name, password =>
            // {
            //     account = Account.LoadFromFile(
            //         pathToAccount: info.Path,
            //         password: password,
            //         currenciesProvider: AtomexApp.CurrenciesProvider,
            //         clientType: ClientType.Wpf);
            // });
            //
            // unlockViewModel.Unlocked += (sender, args) =>
            // {
            //     var atomexClient = new WebSocketAtomexClient(
            //         configuration: App.Configuration,
            //         account: account,
            //         symbolsProvider: AtomexApp.SymbolsProvider,
            //         quotesProvider: AtomexApp.QuotesProvider);
            //
            //     AtomexApp.UseTerminal(atomexClient, restart: true);
            //
            //     DialogViewer.HideDialog(Dialogs.MyWallets);
            //     DialogViewer.HideDialog(Dialogs.Start);
            //     DialogViewer.HideDialog(Dialogs.Unlock);
            // };
            //
            // DialogViewer.ShowDialog(Dialogs.Unlock, unlockViewModel);
        });

        public void CloseButtonCommand()
        {
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