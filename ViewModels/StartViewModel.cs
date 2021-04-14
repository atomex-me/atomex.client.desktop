using System;
using System.Windows.Input;
using System.Linq;
using Atomex.Client.Desktop.Common;
using Atomex.Subsystems;
using Atomex.Wallet.Abstract;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class StartViewModel : ViewModelBase
    {
        public StartViewModel(Action<ViewModelBase> showContent, Action showStart, IAtomexApp app)
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            HasWallets = WalletInfo.AvailableWallets().Count() > 0;

            ShowContent += showContent;
            ShowStart += showStart;
        }

        public event Action<ViewModelBase> ShowContent;
        public event Action ShowStart;

        private IAtomexApp AtomexApp { get; }

        private bool _hasWallets;

        public bool HasWallets
        {
            get => _hasWallets;
            private set => this.RaiseAndSetIfChanged(ref _hasWallets, value);
        }

        private ICommand _myWalletsCommand;

        public ICommand MyWalletsCommand => _myWalletsCommand ??= ReactiveCommand.Create(() =>
        {
            ShowContent?.Invoke(new MyWalletsViewModel(AtomexApp, ShowContent));
        });

        private ICommand _createNewCommand;

        public ICommand CreateNewCommand => _createNewCommand ??=
            ReactiveCommand.Create(() =>
            {
                ShowContent?.Invoke(new CreateWalletViewModel(
                    app: AtomexApp,
                    scenario: CreateWalletScenario.CreateNew,
                    onAccountCreated: OnAccountCreated,
                    onCanceled: OnCanceled));
            });

        private ICommand _restoreByMnemonicCommand;

        public ICommand RestoreByMnemonicCommand => _restoreByMnemonicCommand ??= ReactiveCommand.Create(() =>
        {
            ShowContent?.Invoke(new CreateWalletViewModel(
                app: AtomexApp,
                scenario: CreateWalletScenario.Restore,
                onAccountCreated: OnAccountCreated,
                onCanceled: OnCanceled));
        });

        public void TwitterCommand()
        {
        }

        public void GithubCommand()
        {
        }

        public void TelegramCommand()
        {
        }

        private void OnCanceled()
        {
            ShowStart();
        }

        private void OnAccountCreated(IAccount account)
        {
            var atomexClient = new WebSocketAtomexClient(
                configuration: App.Configuration,
                account: account,
                symbolsProvider: AtomexApp.SymbolsProvider,
                quotesProvider: AtomexApp.QuotesProvider);

            AtomexApp.UseTerminal(atomexClient, restart: true);
        }

        private void DesignerMode()
        {
            HasWallets = true;
        }
    }
}