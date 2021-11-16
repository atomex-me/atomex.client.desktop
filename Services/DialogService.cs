using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Threading;
using Serilog;


namespace Atomex.Client.Desktop.Services
{
    public sealed class DialogService
    {
        public static string MainDialogHostIdentifier => "MainDialogHost";
        private readonly Window _owner;
        private bool _isDialogOpened;
        private readonly bool _isLinux;
        private ViewModelBase _lastDialogViewModel;

        public DialogService(Window owner, bool isLinux)
        {
            _owner = owner;
            _isLinux = isLinux;
        }

        private async void ReRender()
        {
            if (!_isLinux) return;
            Random random = new Random();
            var opacity = random.NextDouble() * (0.9999 - 0.9990) + 0.9990;
            await Task.Delay(50);
            await Dispatcher.UIThread.InvokeAsync(() => _owner.Opacity = opacity);
        }


        public bool CloseDialog()
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
            if (_isDialogOpened)
                DialogHost.DialogHost.GetDialogSession(MainDialogHostIdentifier)?.Close();
            _ = DialogHost.DialogHost.Show(_lastDialogViewModel, MainDialogHostIdentifier);
            ReRender();
        }


        public void Show(ViewModelBase viewModel)
        {
            _lastDialogViewModel = viewModel;

            if (_isDialogOpened)
                DialogHost.DialogHost.GetDialogSession(MainDialogHostIdentifier)?.Close();
            _ = DialogHost.DialogHost.Show(viewModel, MainDialogHostIdentifier);
            ReRender();
            _isDialogOpened = true;
        }
    }
}