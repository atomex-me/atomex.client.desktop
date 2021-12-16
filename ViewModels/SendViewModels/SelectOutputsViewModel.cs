using System;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class SelectOutputsViewModel : ViewModelBase
    {
        public Action BackAction { get; set; }

        public SelectOutputsViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        private ICommand _closeCommand;

        public ICommand CloseCommand => _closeCommand ??=
            (_closeCommand = ReactiveCommand.Create(() => { Desktop.App.DialogService.Close(); }));

        private ICommand _backCommand;

        public ICommand BackCommand => _backCommand ??=
            (_backCommand = ReactiveCommand.Create(() => { BackAction?.Invoke(); }));

        private void DesignerMode()
        {
        }
    }
}