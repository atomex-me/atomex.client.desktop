using System;
using System.Windows.Input;
using System.Linq;

using Avalonia.Controls;
using ReactiveUI;

using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Services;
using Atomex.Wallet.Abstract;

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
            HasWallets = WalletInfo.AvailableWallets().Any();

            MainWindowVM = mainWindowWM;
            ShowContent += showContent;
            ShowStart += showStart;
        }

        private readonly MainWindowViewModel MainWindowVM;

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
            var atomexClient = new WebSocketAtomexClientLegacy(
                exchangeUrl: App.Configuration[$"Services:{account!.Network}:Exchange:Url"],
                marketDataUrl: App.Configuration[$"Services:{account!.Network}:MarketData:Url"],
                clientType: PlatformHelper.GetClientType(),
                authMessageSigner: new AuthMessageSigner(async (data, algorithm) =>
                {
                    const int keyIndex = 0;

                    var securePublicKey = account.Wallet.GetServicePublicKey(keyIndex);
                    var publicKey = securePublicKey.ToUnsecuredBytes();

                    var signature = await account.Wallet
                        .SignByServiceKeyAsync(data, keyIndex)
                        .ConfigureAwait(false);

                    return (publicKey, signature);
                }));

            AtomexApp.ChangeAtomexClient(atomexClient, account, restart: true);
        }
        
        private void OnAccountRestored(IAccount account)
        {
            var atomexClient = new WebSocketAtomexClientLegacy(
                exchangeUrl: App.Configuration[$"Services:{account!.Network}:Exchange:Url"],
                marketDataUrl: App.Configuration[$"Services:{account!.Network}:MarketData:Url"],
                clientType: PlatformHelper.GetClientType(),
                authMessageSigner: new AuthMessageSigner(async (data, algorithm) =>
                {
                    const int keyIndex = 0;

                    var securePublicKey = account.Wallet.GetServicePublicKey(keyIndex);
                    var publicKey = securePublicKey.ToUnsecuredBytes();

                    var signature = await account.Wallet
                        .SignByServiceKeyAsync(data, keyIndex)
                        .ConfigureAwait(false);

                    return (publicKey, signature);
                }));

            MainWindowVM.AccountRestored = true;

            AtomexApp.ChangeAtomexClient(atomexClient, account, restart: true);
        }

        private void DesignerMode()
        {
            HasWallets = true;
        }
    }
}