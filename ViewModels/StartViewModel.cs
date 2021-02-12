using System;
using System.Windows.Input;
using Atomex.Client.Desktop.Common;
using Atomex.Wallet.Abstract;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class StartViewModel : ViewModelBase
    {
        public StartViewModel(Action<ViewModelBase> showContent, Action showStart)
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif

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

        private ViewModelBase _content;

        public ViewModelBase Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        public void MyWalletsCommand()
        {
        }

        private ICommand _createNewCommand;

        // _createNewCommand ??=
        public ICommand CreateNewCommand =>
            ReactiveCommand.Create(() =>
            {
                ShowContent?.Invoke(new CreateWalletViewModel(
                    app: AtomexApp,
                    scenario: CreateWalletScenario.CreateNew,
                    onAccountCreated: OnAccountCreated,
                    onCanceled: OnCanceled));
            });

        public void RestoreByMnemonicCommand()
        {
        }

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
            // var atomexClient = new WebSocketAtomexClient(
            //     configuration: App.Configuration,
            //     account: account,
            //     symbolsProvider: AtomexApp.SymbolsProvider,
            //     quotesProvider: AtomexApp.QuotesProvider);
            //
            // AtomexApp.UseTerminal(atomexClient, restart: true);
            //
            // DialogViewer?.HideDialog(Dialogs.CreateWallet);
            // DialogViewer?.HideDialog(Dialogs.Start);
        }

        private void DesignerMode()
        {
            HasWallets = true;
        }
    }
}