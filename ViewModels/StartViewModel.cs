using System;
using System.Windows.Input;
using System.Linq;

using ReactiveUI;

using Atomex.Client.Desktop.Common;
using Atomex.Services;
using Atomex.Wallet.Abstract;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.ViewModels
{
    public class StartViewModel : ViewModelBase
    {
        public static string TwitterAddress => "https://twitter.com/atomex_official";
        public static string TelegramAddress => "tg://resolve?domain=atomex_official";
        public static string GithubAddress => "https://github.com/atomex-me";
        
        public StartViewModel()
        {
        }

        public StartViewModel(
            Action<ViewModelBase> showContent,
            Action showStart,
            IAtomexApp app,
            MainWindowViewModel mainWindowWM)
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            HasWallets = WalletInfo.AvailableWallets().Count() > 0;

            MainWindowVM = mainWindowWM;
            ShowContent += showContent;
            ShowStart += showStart;
        }

        private MainWindowViewModel MainWindowVM;

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
                onAccountCreated: OnAccountRestored,
                onCanceled: OnCanceled));
        });

        public void TwitterCommand()
        {
            App.OpenBrowser(TwitterAddress);
        }

        public void GithubCommand()
        {
            App.OpenBrowser(GithubAddress);
        }

        public void TelegramCommand()
        {
            App.OpenBrowser(TelegramAddress);
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
                symbolsProvider: AtomexApp.SymbolsProvider);

            AtomexApp.UseAtomexClient(atomexClient, restart: true);
        }
        
        private void OnAccountRestored(IAccount account)
        {
            var atomexClient = new WebSocketAtomexClient(
                configuration: App.Configuration,
                account: account,
                symbolsProvider: AtomexApp.SymbolsProvider);

            MainWindowVM.AccountRestored = true;

            AtomexApp.UseAtomexClient(atomexClient, restart: true);
        }

        private void DesignerMode()
        {
            HasWallets = true;
        }
    }
}