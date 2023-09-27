using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Client.Common;
using Atomex.Client.Desktop.Controls;
using Atomex.Client.Desktop.Properties;
using Atomex.Wallet;
using Atomex.LiteDb;

namespace Atomex.Client.Desktop.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private bool _userIgnoreActiveSwaps;
        private UnlockViewModel _unlockViewModel;
        private readonly IAtomexApp _app;
        private readonly IMainView _mainView;
        private WalletMainViewModel _walletMainViewModel;

        public Action OnUpdateAction;

        [Reactive] public ViewModelBase Content { get; set; }
        [Reactive] public bool HasAccount { get; set; }
        public bool AccountRestored { get; set; }
        [ObservableAsProperty] public bool UpdatesReady { get; }
        [ObservableAsProperty] public bool IsDownloadingUpdate { get; }
        public bool IsLinux { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        [Reactive] public bool HasUpdates { get; set; }
        [Reactive] public string UpdateVersion { get; set; }
        [Reactive] public bool UpdateStarted { get; set; }
        [Reactive] public string? StartupData { get; set; }
        [Reactive] public int UpdateDownloadProgress { get; set; }

        #region Commands

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ??= ReactiveCommand.Create(OnUpdateClick);

        private ICommand _signOutCommand;
        public ICommand SignOutCommand => _signOutCommand ??= ReactiveCommand.Create(() => SignOut());

        #endregion Commands

        public MainWindowViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public MainWindowViewModel(IAtomexApp app, IMainView? mainView = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _walletMainViewModel = new WalletMainViewModel(_app);

            this.WhenAnyValue(vm => vm.HasAccount)
                .Subscribe(hasAccount =>
                {
                    if (hasAccount)
                    {
                        //_walletMainViewModel = new WalletMainViewModel(_app);

                        ShowContent(_walletMainViewModel);

                        if (app.Account?.Network == Core.Network.TestNet)
                        {
                            var messageViewModel = MessageViewModel.Message(
                                title: "Warning",
                                text: Resources.TestNetWalletWarning,
                                nextTitle: "Ok",
                                nextAction: () => App.DialogService.Close());

                            App.DialogService.Show(messageViewModel);
                        }

                        if (AccountRestored)
                        {
                            var restoreViewModel = new RestoreDialogViewModel(_app)
                            {
                                OnRestored = () => AccountRestored = false
                            };

                            _ = restoreViewModel.ScanAsync(new LiteDbMigrationResult
                            {
                                { MigrationEntityType.Addresses, "BTC"},
                                { MigrationEntityType.Addresses, "LTC"},
                                { MigrationEntityType.Addresses, "ETH"},
                                { MigrationEntityType.Addresses, "USDT" },
                                { MigrationEntityType.Addresses, "WBTC" },
                                { MigrationEntityType.Addresses, "TBTC" },
                                { MigrationEntityType.Addresses, "XTZ"},
                                { MigrationEntityType.Addresses, "FA12"},
                                { MigrationEntityType.Addresses, "FA2"}
                            });
                        }
                    }
                    else
                    {
                        AccountRestored = false;
                        StartupData = null;
                    }
                });

            this.WhenAnyValue(vm => vm.UpdateDownloadProgress)
                .Select(downloadProgress => HasUpdates && downloadProgress is > 0 and < 100)
                .ToPropertyEx(this, vm => vm.IsDownloadingUpdate);

            this.WhenAnyValue(vm => vm.UpdateDownloadProgress)
                .Select(downloadProgress => HasUpdates && downloadProgress == 100)
                .ToPropertyEx(this, vm => vm.UpdatesReady);

            this.WhenAnyValue(vm => vm.StartupData)
                .Subscribe(startupData =>
                {
                    if (!string.IsNullOrEmpty(startupData) && HasAccount)
                    {
                        App.ConnectTezosDapp?.Invoke(startupData);
                    }
                });

            SubscribeToServices();

            if (mainView != null)
            {
                _mainView = mainView;
                _mainView.Inactivity += InactivityHandler;
            }

            ShowStart();
        }

        public void CloseDialog()
        {
            App.DialogService.Close(closedByButton: true);
        }

        private void SubscribeToServices()
        {
            _app.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs args)
        {
            if (_app?.Account == null)
            {
                HasAccount = false;
                _mainView?.StopInactivityControl();

                return;
            }

            HasAccount = true;

            // auto sign out after timeout
            if (_mainView != null && _app.Account.UserData.AutoSignOut)
                _mainView.StartInactivityControl(TimeSpan.FromMinutes(_app.Account.UserData.PeriodOfInactivityInMin));

            StartLookingForUserMessages(TimeSpan.FromSeconds(90));
        }

        private async void OnUpdateClick()
        {
            await SignOut(withAppUpdate: true);

            if (_app.AtomexClient != null)
                return;

            OnUpdateAction?.Invoke();
            UpdateStarted = true;
        }

        private void ShowContent(ViewModelBase content)
        {
            if (Content?.GetType() != content.GetType())
            {
                Content = content;
            }
        }

        private void ShowStart()
        {
            ShowContent(new StartViewModel(ShowContent, ShowStart, _app, this));
        }

        private async Task SignOut(bool withAppUpdate = false)
        {
            try
            {
                if (await WhetherToCancelClosingAsync() && !_userIgnoreActiveSwaps)
                {
                    var messageViewModel = MessageViewModel.Message(
                        title: "Warning",
                        text: Resources.ActiveSwapsWarning,
                        nextTitle: "Close",
                        backAction: () => App.DialogService.Close(),
                        nextAction: () =>
                        {
                            _userIgnoreActiveSwaps = true;

                            if (withAppUpdate)
                            {
                                OnUpdateClick();
                            }
                            else
                            {
                                _ = SignOut();
                            }
                        });

                    App.DialogService.Show(messageViewModel);
                    return;
                }

                _ = Dispatcher.UIThread.InvokeAsync(() => { App.DialogService.Close(); });

                _app.ChangeAtomexClient(
                    atomexClient: null,
                    account: null,
                    localStorage: null);

                _userIgnoreActiveSwaps = false;

                ShowStart();
            }
            catch (Exception e)
            {
                Log.Error(e, "Sign Out error");
            }
        }

        private async Task<bool> HasActiveSwapsAsync()
        {
            var swaps = await _app.Account
                .GetSwapsAsync();

            return swaps.Any(swap => swap.IsActive);
        }

        private async Task<bool> WhetherToCancelClosingAsync()
        {
            if (_app.Account == null)
                return false;

            if (!_app.Account.UserData.ShowActiveSwapWarning)
                return false;

            var hasActiveSwaps = await HasActiveSwapsAsync();

            return hasActiveSwaps;
        }

        private void InactivityHandler(object? sender, EventArgs args)
        {
            if (_app?.Account == null)
                return;

            var pathToAccount = _app.Account.Wallet.PathToWallet;
            var accountDirectory = Path.GetDirectoryName(pathToAccount);

            if (accountDirectory == null)
                return;

            var accountName = new DirectoryInfo(accountDirectory).Name;

            _unlockViewModel = new UnlockViewModel(
                walletName: accountName,
                unlockAction: password =>
                {
                    var _ = HdWallet.LoadFromFile(pathToAccount, password);
                },
                goBack: async () => await SignOut(),
                onUnlock: async () =>
                {
                    ShowContent(_walletMainViewModel);
                    App.DialogService.UnlockWallet();

                    var userId = Atomex.ViewModels.Helpers.GetUserId(_app.Account);
                    var messages = await Atomex.ViewModels.Helpers.GetUserMessages(userId);
                    if (messages == null) return;

                    foreach (var message in messages.Where(message => !message.IsReaded))
                    {
                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            App.DialogService.Show(
                                MessageViewModel.Success(
                                    title: Resources.CvWarning,
                                    text: message.Message,
                                    nextAction: async () =>
                                    {
                                        await Atomex.ViewModels.Helpers.MarkUserMessageReaded(message.Id);
                                        App.DialogService.Close();
                                    }));
                        });
                    }
                });

            App.DialogService.LockWallet();
            ShowContent(_unlockViewModel);
        }

        private void DesignerMode()
        {
            HasAccount = true;
        }

        private void StartLookingForUserMessages(TimeSpan delayInterval)
        {
            var userId = Atomex.ViewModels.Helpers.GetUserId(_app.Account);
            var firstRun = true;

            _ = Task.Run(async () =>
            {
                while (HasAccount)
                {
                    if (firstRun)
                    {
                        firstRun = false;
                    }
                    else
                    {
                        await Task.Delay(delayInterval);

                        if (!HasAccount)
                            return;
                    }

                    if (AccountRestored || Content is UnlockViewModel)
                        continue;

                    var messages = await Atomex.ViewModels.Helpers.GetUserMessages(userId);

                    if (messages == null)
                        continue;

                    foreach (var message in messages.Where(message => !message.IsReaded))
                    {
                        _ = Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            App.DialogService.Show(
                                MessageViewModel.Success(
                                    title: Resources.CvWarning,
                                    text: message.Message,
                                    nextAction: async () =>
                                    {
                                        await Atomex.ViewModels.Helpers.MarkUserMessageReaded(message.Id);
                                        App.DialogService.Close();
                                    }));
                        });

                        break;
                    }
                }
            });
        }
    }
}