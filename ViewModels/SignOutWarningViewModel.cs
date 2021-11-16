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
            Desktop.App.DialogService.Close();
        }));

        private ICommand _ignoreCommand;

        public ICommand IgnoreCommand => _ignoreCommand ??= (_ignoreCommand = ReactiveCommand.Create(() =>
        {
            OnIgnoreCommand?.Invoke();
        }));

        public Action OnIgnoreCommand { get; set; }
    }
}