using System;
using System.Windows.Input;
using Atomex.Client.Desktop.Properties;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class SignOutWarningViewModel : ViewModelBase
    {
        public string WarningText => Resources.ActiveSwapsWarning;

        private ICommand _okCommand;

        public ICommand OkCommand => _okCommand ??= (_okCommand = ReactiveCommand.Create(() =>
        {
            Desktop.App.DialogService.CloseDialog();
        }));

        private ICommand _ignoreCommand;

        public ICommand IgnoreCommand => _ignoreCommand ??= (_ignoreCommand = ReactiveCommand.Create(() =>
        {
            OnIgnoreCommand?.Invoke();
            Desktop.App.DialogService.CloseDialog();
        }));

        public Action OnIgnoreCommand { get; set; }
    }
}