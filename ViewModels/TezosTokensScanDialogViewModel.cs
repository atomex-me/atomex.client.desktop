using System;
using System.Windows.Input;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class TezosTokensScanDialogViewModel : ViewModelBase
    {
        public Action OnCancel { get; set; }

        private ICommand _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= (_cancelCommand = ReactiveCommand.Create(() =>
        {
            OnCancel?.Invoke();
        }));
    }
}