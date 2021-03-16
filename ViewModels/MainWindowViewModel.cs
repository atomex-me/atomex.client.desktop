using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Client.Desktop.Controls;
using Atomex.Core;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.Dialogs.ViewModels;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Views;
using Atomex.Common;
using ReactiveUI;
using Atomex.Subsystems;
using Atomex.Subsystems.Abstract;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;
using Atomex.MarketData;
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

        private ViewModelBase _firstDialog;
        private ViewModelBase _secondDialog;

        public void ShowDialog()
        {
            var firstDialogWrapped = new DialogServiceViewModel(_firstDialog);
            _dialogService.Show(firstDialogWrapped);
        }

        public void ShowCustomDialog()
        {
            var secondDialogWrapper = new DialogServiceViewModel(_secondDialog);
            _dialogService.Show(secondDialogWrapper);
        }

        public MainWindowViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public MainWindowViewModel(
            IDialogService<ViewModelBase> dialogService,
            IAtomexApp app,
            IMainView mainView = null
        )
        {
            _dialogService = dialogService;
            _firstDialog = new DialogViewModel();
            _secondDialog = new SecondDialogViewModel();

            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));

            // PortfolioViewModel = new PortfolioViewModel(AtomexApp);
            // ConversionViewModel = new ConversionViewModel(AtomexApp);
            // WalletsViewModel = new WalletsViewModel(AtomexApp, this, ConversionViewModel);
            // SettingsViewModel = new SettingsViewModel(AtomexApp, DialogViewer);

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


        private readonly IDialogService<ViewModelBase> _dialogService;
        public static IAtomexApp AtomexApp { get; private set; }
        public IMainView MainView { get; set; }

        // public PortfolioViewModel PortfolioViewModel { get; set; }
        // public WalletsViewModel WalletsViewModel { get; set; }
        // public ConversionViewModel ConversionViewModel { get; set; }
        // public SettingsViewModel SettingsViewModel { get; set; }

        private int _selectedMenuIndex;

        public int SelectedMenuIndex
        {
            get => _selectedMenuIndex;
            set
            {
                _selectedMenuIndex = value;
                this.RaisePropertyChanged(nameof(SelectedMenuIndex));
            }
        }

        private string _installedVersion;

        public string InstalledVersion
        {
            get => _installedVersion;
            set
            {
                _installedVersion = value;
                this.RaisePropertyChanged(nameof(InstalledVersion));
            }
        }

        private bool _updatesReady;

        public bool UpdatesReady
        {
            get => _updatesReady;
            set
            {
                _updatesReady = value;
                this.RaisePropertyChanged(nameof(UpdatesReady));
            }
        }

        private bool _hasAccount;

        public bool HasAccount
        {
            get => _hasAccount;
            set
            {
                _hasAccount = value;
                this.RaisePropertyChanged(nameof(HasAccount));
            }
        }

        private bool _isLocked;

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                _isLocked = value;
                this.RaisePropertyChanged(nameof(IsLocked));
            }
        }

        private bool _isExchangeConnected;

        public bool IsExchangeConnected
        {
            get => _isExchangeConnected;
            set
            {
                _isExchangeConnected = value;
                this.RaisePropertyChanged(nameof(IsExchangeConnected));
            }
        }

        private bool _isMarketDataConnected;

        public bool IsMarketDataConnected
        {
            get => _isMarketDataConnected;
            set
            {
                _isMarketDataConnected = value;
                this.RaisePropertyChanged(nameof(IsMarketDataConnected));
            }
        }

        private bool _isQuotesProviderAvailable;

        public bool IsQuotesProviderAvailable
        {
            get => _isQuotesProviderAvailable;
            set
            {
                _isQuotesProviderAvailable = value;
                this.RaisePropertyChanged(nameof(IsQuotesProviderAvailable));
            }
        }

        private string _login;

        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                this.RaisePropertyChanged(nameof(Login));
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
            AtomexApp.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
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

            terminal.ServiceConnected += OnTerminalServiceStateChangedEventHandler;
            terminal.ServiceDisconnected += OnTerminalServiceStateChangedEventHandler;

            var account = terminal.Account;
            account.Locked += OnAccountLockChangedEventHandler;
            account.Unlocked += OnAccountLockChangedEventHandler;

            IsLocked = account.IsLocked;
            HasAccount = true;

            // auto sign out after timeout
            if (MainView != null && account.UserSettings.AutoSignOut)
                MainView.StartInactivityControl(TimeSpan.FromMinutes(account.UserSettings.PeriodOfInactivityInMin));
        }

        private void OnTerminalServiceStateChangedEventHandler(object sender, TerminalServiceEventArgs args)
        {
            if (!(sender is IAtomexClient terminal))
                return;

            IsExchangeConnected = terminal.IsServiceConnected(TerminalService.Exchange);
            IsMarketDataConnected = terminal.IsServiceConnected(TerminalService.MarketData);

            // subscribe to symbols updates
            if (args.Service == TerminalService.MarketData && IsMarketDataConnected)
            {
                terminal.SubscribeToMarketData(SubscriptionType.TopOfBook);
                terminal.SubscribeToMarketData(SubscriptionType.DepthTwenty);
            }
        }

        private void OnQuotesProviderAvailabilityChangedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;

            IsQuotesProviderAvailable = provider.IsAvailable;
        }

        private void OnAccountLockChangedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is IAccount account))
                return;

            IsLocked = account.IsLocked;
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

                // DialogViewer.HideAllDialogs();

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

            unlockViewModel.Unlocked += (s, a) =>
            {
                // todo: show main wallet window
            };

            ShowContent(unlockViewModel);
        }

        private void DesignerMode()
        {
            HasAccount = true;
        }
    }
}