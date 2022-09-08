using System;
using Atomex.Client.Desktop.Dialogs.ViewModels;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Threading;
using DialogHost;


namespace Atomex.Client.Desktop.Services
{
    public sealed class DialogService
    {
        private static string MainDialogHostIdentifier => "MainDialogHost";
        private bool _isDialogOpened;
        private readonly DialogServiceViewModel _dialogServiceViewModel;
        private bool _walletLocked;
        private bool _showAfterUnlock;

        public DialogService()
        {
            _dialogServiceViewModel = new DialogServiceViewModel();
        }

        public bool Close()
        {
            var result = _isDialogOpened;
            if (!_isDialogOpened) return result;
            Dispatcher.UIThread.InvokeAsync(() =>
                DialogHost.DialogHost.GetDialogSession(MainDialogHostIdentifier)?.Close());
            _isDialogOpened = false;

            return result;
        }

        public bool IsCurrentlyShowing(ViewModelBase vm)
        {
            return _dialogServiceViewModel.Content.GetType() == vm.GetType();
        }

        public void ShowPrevious()
        {
            if (_isDialogOpened) return;
            Dispatcher.UIThread.InvokeAsync(() =>
                DialogHost.DialogHost.Show(_dialogServiceViewModel, MainDialogHostIdentifier, ClosingEventHandler));
            _isDialogOpened = true;
        }

        private void ClosingEventHandler(object sender, DialogClosingEventArgs args)
        {
            if (_dialogServiceViewModel.Content is IDisposable vm)
            {
                vm.Dispose();
            }
        }


        public void Show(ViewModelBase viewModel)
        {
            _dialogServiceViewModel.Content = viewModel;

            if (!_walletLocked)
                ShowPrevious();
            else
                _showAfterUnlock = true;
        }

        public void LockWallet()
        {
            _walletLocked = true;
            if (_isDialogOpened)
                _showAfterUnlock = Close();
        }

        public void UnlockWallet()
        {
            _walletLocked = false;
            if (!_showAfterUnlock) return;
            
            ShowPrevious();
            _showAfterUnlock = false;
        }
    }
}