using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using Atomex.Client.Desktop.Dialogs.Models;
using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Dialogs.ViewModels
{
    internal sealed class DialogServiceViewModel : ViewModelBase
    {
        private DialogResult _dialogResult = Models.DialogResult.Cancel;

        public DialogServiceViewModel(ViewModelBase content)
        {
            YesCommand = ReactiveCommand.Create<Window>(window => Close(DialogResult.Yes, window));
            NoCommand = ReactiveCommand.Create<Window>(window => Close(DialogResult.No, window));
            CancelCommand = ReactiveCommand.Create<Window>(window => Close(DialogResult.Cancel, window));
            Content = content;
        }

        public ICommand YesCommand { get; }

        public ICommand NoCommand { get; }

        public ICommand CancelCommand { get; }

        public DialogResult DialogResult
        {
            get => _dialogResult;
            set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
        }

        public ViewModelBase Content { get; set; }

        private void Close(DialogResult dialogResult, Window window)
        {
            DialogResult = dialogResult;
            window.Close();
        }
    }
}
