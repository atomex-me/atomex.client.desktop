using System;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Dialogs.ViewModels;
using Avalonia.Controls;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Threading;
using Serilog;


namespace Atomex.Client.Desktop.Services
{
    public sealed class DialogService
    {
        private static string MainDialogHostIdentifier => "MainDialogHost";
        private readonly Window _owner;
        private bool _isDialogOpened;
        private readonly bool _isLinux;
        private readonly DialogServiceViewModel _dialogServiceViewModel;

        public DialogService(Window owner, bool isLinux)
        {
            _owner = owner;
            _isLinux = isLinux;
            _dialogServiceViewModel = new DialogServiceViewModel();
        }

        private async void ReRender()
        {
            if (!_isLinux) return;
            Random random = new Random();
            var opacity = random.NextDouble() * (0.9999 - 0.9990) + 0.9990;
            await Task.Delay(50);
            await Dispatcher.UIThread.InvokeAsync(() => _owner.Opacity = opacity);
        }


        public bool Close()
        {
            var result = _isDialogOpened;
            if (_isDialogOpened)
            {
                DialogHost.DialogHost.GetDialogSession(MainDialogHostIdentifier)?.Close();
                _isDialogOpened = false;
            }

            return result;
        }

        public void ShowPrevious()
        {
            if (_isDialogOpened) return;
            _ = DialogHost.DialogHost.Show(_dialogServiceViewModel.Content, MainDialogHostIdentifier);
            ReRender();
        }


        public void Show(ViewModelBase viewModel)
        {
            _dialogServiceViewModel.Content = viewModel;
            if (_isDialogOpened) return;
            
            _ = DialogHost.DialogHost.Show(_dialogServiceViewModel, MainDialogHostIdentifier);
            ReRender();
            _isDialogOpened = true;
        }
    }
}