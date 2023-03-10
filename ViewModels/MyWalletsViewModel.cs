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
using Atomex.Wallet;
using Atomex.Wallet.Abstract;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class MyWalletsViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        private readonly Action<ViewModelBase> _showContent;
        private LiteDbMigrationResult? _migrationResult;

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
            IAccount? account = null;
            ILocalStorage? localStorage = null;

            var unlockViewModel = new UnlockViewModel(
                walletName: info.Name,
                unlockAction: password =>
                {
                    var wallet = HdWallet.LoadFromFile(info.Path, password);

                    var pathToDb = Path.Combine(Path.GetDirectoryName(wallet.PathToWallet)!, Account.DefaultDataFileName);

                    _migrationResult = LiteDbMigrationManager.Migrate(
                        pathToDb: pathToDb,
                        sessionPassword: SessionPasswordHelper.GetSessionPassword(password),
                        network: wallet.Network);

                    localStorage = new LiteDbCachedLocalStorage(
                        pathToDb: pathToDb,
                        password: password,
                        network: wallet.Network);
                        //currencies: _app.CurrenciesProvider.GetCurrencies(wallet.Network),
                        //network: wallet.Network,
                        //migrationComplete: actionType =>
                        //{
                        //    _doAfterAtomexClientChanged = actionType switch
                        //    {
                        //        MigrationActionType.XtzTransactionsDeleted => TezosTransactionsDeleted,
                        //        MigrationActionType.XtzTokensDataDeleted => OnTezosTokensDataDeleted,
                        //        _ => null
                        //    };
                        //});

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
                    var atomexClient = AtomexClientCreator.Create(
                        configuration: App.Configuration,
                        network: account!.Network,
                        platformType: PlatformHelper.GetClientType(),
                        account.DefaultAuthMessageSigner());

                    _app.ChangeAtomexClient(
                        atomexClient: atomexClient,
                        account: account,
                        localStorage: localStorage,
                        restart: true);

                    App.DialogService.UnlockWallet();
                });

            _showContent?.Invoke(unlockViewModel);
        }

        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            if (_app != null && _app?.AtomexClient == null)
            {
                _app!.AtomexClientChanged -= OnAtomexClientChangedEventHandler;
                return;
            }

            DoAfterMigrations();
        }

        private async void DoAfterMigrations()
        {
            if (_migrationResult == null)
                return; // nothing to do

            try
            {
                var restoreDialogViewModel = new RestoreDialogViewModel(_app);

                await restoreDialogViewModel
                    .ScanAsync(_migrationResult);
            }
            catch (Exception e)
            {
                Log.Error(e, "DoAfterMigrations error");
            }
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