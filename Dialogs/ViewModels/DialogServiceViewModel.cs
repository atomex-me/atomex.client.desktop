using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using Atomex.Client.Desktop.Dialogs.Models;
using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Dialogs.ViewModels
{
    internal sealed class DialogServiceViewModel : ViewModelBase
    {
        private DialogResult _dialogResult = DialogResult.Cancel;

        public DialogServiceViewModel()
        {
            YesCommand = ReactiveCommand.Create<Window>(_ => Close(DialogResult.Yes));
            NoCommand = ReactiveCommand.Create<Window>(_ => Close(DialogResult.No));
            CancelCommand = ReactiveCommand.Create<Window>(_ => Close(DialogResult.Cancel));
        }

        public ICommand YesCommand { get; }

        public ICommand NoCommand { get; }

        public ICommand CancelCommand { get; }

        public DialogResult DialogResult
        {
            get => _dialogResult;
            set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
        }

        private ViewModelBase _content;

        public ViewModelBase Content
        {
            get => _content;
            set
            {
                _content = value;
                this.RaisePropertyChanged(nameof(Content));
            }
        }

        private void Close(DialogResult dialogResult)
        {
            DialogResult = dialogResult;
            App.DialogService.CloseDialog();
        }
    }
}