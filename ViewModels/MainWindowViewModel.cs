using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Client.Desktop.Controls;
using Atomex.Client.Desktop.Dialogs.ViewModels;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using ReactiveUI;
using Atomex.Subsystems;
using Atomex.Wallet;
using Avalonia.Controls.ApplicationLifetimes;
using Serilog;


namespace Atomex.Client.Desktop.ViewModels
{
    internal sealed class MainWindowViewModel : ViewModelBase
    {
        private void ShowContent(ViewModelBase content)
        {
            if (Content?.GetType() != content.GetType())
            {
                Content = content;
            }
        }

        public void ShowStart()
        {
            ShowContent(new StartViewModel(ShowContent, ShowStart, AtomexApp));
        }

        private ViewModelBase _content;

        public ViewModelBase Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        // private ViewModelBase _firstDialog;
        // private ViewModelBase _secondDialog;
        private ViewModelBase MainWalletVM;

        // public void ShowDialog()
        // {
        //     var firstDialogWrapped = new DialogServiceViewModel(_firstDialog);
        //     App.DialogService.Show(firstDialogWrapped);
        // }
        //
        // public void ShowCustomDialog()
        // {
        //     var secondDialogWrapper = new DialogServiceViewModel(_secondDialog);
        //     App.DialogService.Show(secondDialogWrapper);
        // }

        public MainWindowViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public MainWindowViewModel(IAtomexApp app, IMainView mainView = null)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            // _firstDialog = new DialogViewModel();
            // _secondDialog = new SecondDialogViewModel();
            MainWalletVM = new WalletMainViewModel(AtomexApp);

            // InstalledVersion = App.Updater.InstalledVersion.ToString();

            SubscribeToServices();

            // todo: UPDATES
            // SubscribeToUpdates(App.Updater);

            if (mainView != null)
            {
                MainView = mainView;
                MainView.MainViewClosing += (sender, args) => Closing(args);
                MainView.Inactivity += InactivityHandler;
            }

            ShowStart();
        }

        public static IAtomexApp AtomexApp { get; private set; }
        public IMainView MainView { get; set; }


        private bool _hasAccount;

        public bool HasAccount
        {
            get => _hasAccount;
            set
            {
                _hasAccount = value;
                if (_hasAccount)
                {
                    ShowContent(MainWalletVM);
                }

                this.RaisePropertyChanged(nameof(HasAccount));
            }
        }

        // private void SubscribeToUpdates(Updater updater)
        // {
        //     updater.UpdatesReady += OnUpdatesReadyEventHandler;
        // }

        // private void OnUpdatesReadyEventHandler(object sender, ReadyEventArgs e)
        // {
        //     UpdatesReady = true;
        // }

        private void SubscribeToServices()
        {
            AtomexApp.TerminalChanged += OnTerminalChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs args)
        {
            var terminal = args.Terminal;

            if (terminal?.Account == null)
            {
                HasAccount = false;
                MainView?.StopInactivityControl();
                return;
            }

            var account = terminal.Account;

            HasAccount = true;

            // auto sign out after timeout
            if (MainView != null && account.UserSettings.AutoSignOut)
                MainView.StartInactivityControl(TimeSpan.FromMinutes(account.UserSettings.PeriodOfInactivityInMin));
        }


        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ??= ReactiveCommand.Create(OnUpdateClick);

        private void OnUpdateClick()
        {
            (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown(101);
        }

        private ICommand _signOutCommand;
        public ICommand SignOutCommand => _signOutCommand ??= ReactiveCommand.Create(SignOut);

        private async void SignOut()
        {
            try
            {
                if (await WhetherToCancelClosingAsync())
                    return;

                AtomexApp.UseTerminal(null);

                ShowStart();
            }
            catch (Exception e)
            {
                Log.Error(e, "Sign Out error");
            }
        }

        private bool _forcedClose;

        private async void Closing(CancelEventArgs args)
        {
            if (AtomexApp.Account == null || _forcedClose)
                return;

            args.Cancel = true;

            try
            {
                await Task.Yield();

                var cancel = await WhetherToCancelClosingAsync();

                if (!cancel)
                {
                    _forcedClose = true;
                    MainView.Close();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Closing error");
            }
        }

        private async Task<bool> HasActiveSwapsAsync()
        {
            var swaps = await AtomexApp.Account
                .GetSwapsAsync();

            return swaps.Any(swap => swap.IsActive);
        }

        private async Task<bool> WhetherToCancelClosingAsync()
        {
            if (!AtomexApp.Account.UserSettings.ShowActiveSwapWarning)
                return false;

            var hasActiveSwaps = await HasActiveSwapsAsync();

            if (!hasActiveSwaps)
                return false;

            // var result = await DialogViewer
            //     .ShowMessageAsync(
            //         title: Resources.Warning,
            //         message: Resources.ActiveSwapsWarning,
            //         style: MessageDialogStyle.AffirmativeAndNegative);
            //
            // return result == MessageDialogResult.Negative;

            return false;
        }

        private void InactivityHandler(object sender, EventArgs args)
        {
            if (AtomexApp?.Account == null)
                return;

            var pathToAccount = AtomexApp.Account.Wallet.PathToWallet;
            var accountDirectory = Path.GetDirectoryName(pathToAccount);

            if (accountDirectory == null)
                return;

            var accountName = new DirectoryInfo(accountDirectory).Name;

            var unlockViewModel = new UnlockViewModel(accountName, password =>
            {
                var _ = Account.LoadFromFile(
                    pathToAccount: pathToAccount,
                    password: password,
                    currenciesProvider: AtomexApp.CurrenciesProvider,
                    clientType: ClientType.Unknown);
            }, SignOut);

            unlockViewModel.Unlocked += (s, a) => { ShowContent(MainWalletVM); };

            App.DialogService?.CloseDialog();
            ShowContent(unlockViewModel);
        }

        private void DesignerMode()
        {
            HasAccount = true;
        }
    }
}