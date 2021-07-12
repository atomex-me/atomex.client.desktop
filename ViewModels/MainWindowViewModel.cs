using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Client.Desktop.Controls;
using Atomex.Client.Desktop.Common;
using Atomex.Common;
using ReactiveUI;
using Atomex.Services;
using Atomex.Wallet;
using Serilog;


namespace Atomex.Client.Desktop.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
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
            ShowContent(new StartViewModel(ShowContent, ShowStart, AtomexApp, this));
        }

        private ViewModelBase _content;

        public ViewModelBase Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        private ViewModelBase MainWalletVM;

        public MainWindowViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public Action OnUpdateAction;

        public MainWindowViewModel(IAtomexApp app, IMainView mainView = null)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            MainWalletVM = new WalletMainViewModel(AtomexApp);
            
            SubscribeToServices();

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
                    
                    var currencies = AtomexApp.Account.Currencies.ToList();
                    var a = 5;

                    if (AccountRestored)
                    {
                        App.DialogService.Show(new RestoreDialogViewModel(AtomexApp));
                        AccountRestored = false;
                    }
                }

                this.RaisePropertyChanged(nameof(HasAccount));
            }
        }

        public bool AccountRestored { get; set; }

        private bool _updatesReady;

        public bool UpdatesReady => HasUpdates && UpdateDownloadProgress == 100;
        public bool IsDownloadingUpdate => HasUpdates && UpdateDownloadProgress > 0 && UpdateDownloadProgress < 100;

        private bool _hasUpdates;

        public bool HasUpdates
        {
            get => _hasUpdates;
            set
            {
                _hasUpdates = value;
                OnPropertyChanged(nameof(HasUpdates));
            }
        }

        private string _updateVersion;

        public string UpdateVersion
        {
            get => _updateVersion;
            set
            {
                _updateVersion = value;
                OnPropertyChanged(nameof(UpdateVersion));
            }
        }

        private bool _updateStarted;

        public bool UpdateStarted
        {
            get => _updateStarted;
            set
            {
                _updateStarted = value;
                OnPropertyChanged(nameof(UpdateStarted));
            }
        }

        private int _updateDownloadProgress;

        public int UpdateDownloadProgress
        {
            get => _updateDownloadProgress;
            set
            {
                if (_updateDownloadProgress != value)
                {
                    _updateDownloadProgress = value;
                    OnPropertyChanged(nameof(UpdateDownloadProgress));
                    OnPropertyChanged(nameof(IsDownloadingUpdate));
                    OnPropertyChanged(nameof(UpdatesReady));
                }
            }
        }

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

        private async void OnUpdateClick()
        {
            await SignOut(withAppUpdate: true);
            if (AtomexApp.Terminal != null) return;

            OnUpdateAction?.Invoke();
            UpdateStarted = true;
        }

        private ICommand _signOutCommand;
        public ICommand SignOutCommand => _signOutCommand ??= ReactiveCommand.Create(() => SignOut());

        private bool _userIgnoreActiveSwaps { get; set; }

        private async Task SignOut(bool withAppUpdate = false)
        {
            try
            {
                if (await WhetherToCancelClosingAsync() && !_userIgnoreActiveSwaps)
                {
                    App.DialogService.Show(new SignOutWarningViewModel
                    {
                        OnIgnoreCommand = async () =>
                        {
                            _userIgnoreActiveSwaps = true;
                            if (withAppUpdate)
                            {
                                OnUpdateClick();
                            }
                            else
                            {
                                await SignOut();
                            }
                        }
                    });
                    return;
                }

                AtomexApp.UseTerminal(null);
                _userIgnoreActiveSwaps = false;

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
            // if (AtomexApp.Account == null || _forcedClose)
            //     return;
            //
            // args.Cancel = true;
            //
            // try
            // {
            //     await Task.Yield();
            //
            //     var cancel = await WhetherToCancelClosingAsync();
            //
            //     if (!cancel)
            //     {
            //         _forcedClose = true;
            //         MainView.Close();
            //     }
            // }
            // catch (Exception e)
            // {
            //     Log.Error(e, "Closing error");
            // }
        }

        private async Task<bool> HasActiveSwapsAsync()
        {
            var swaps = await AtomexApp.Account
                .GetSwapsAsync();

            return swaps.Any(swap => swap.IsActive);
        }

        private async Task<bool> WhetherToCancelClosingAsync()
        {
            if (AtomexApp.Account == null) return false;
            if (!AtomexApp.Account.UserSettings.ShowActiveSwapWarning)
                return false;

            var hasActiveSwaps = await HasActiveSwapsAsync();

            if (hasActiveSwaps)
                return true;

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
            }, async () => await SignOut());

            var wasClosed = App.DialogService.CloseDialog();

            unlockViewModel.Unlocked += (s, a) =>
            {
                ShowContent(MainWalletVM);

                if (wasClosed)
                {
                    App.DialogService.ShowPrevious();
                }
            };


            ShowContent(unlockViewModel);
        }

        private void DesignerMode()
        {
            HasAccount = true;
        }
    }
}