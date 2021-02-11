using System;
using System.Reactive;
using Atomex.Core;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.Dialogs.ViewModels;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    internal sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly IDialogService<ViewModelBase> _dialogService;

        public MainWindowViewModel(IDialogService<ViewModelBase> unsavedChangesDialogService)
        {
            _dialogService = unsavedChangesDialogService;
            _firstDialog = new DialogViewModel();
            _secondDialog = new SecondDialogViewModel();

            Content = new StartViewModel();
        }


        private ViewModelBase _firstDialog;
        private ViewModelBase _secondDialog;

        public ViewModelBase Content { get; set; }

        public void ShowDialog()
        {
            var firstDialogWrapped = new DialogServiceViewModel(_firstDialog);
            _dialogService.Show(firstDialogWrapped);
        }

        public void ShowCustomDialog()
        {
            var secondDialogWrapper = new DialogServiceViewModel(_secondDialog);
            _dialogService.Show(secondDialogWrapper);
        }
    }
}