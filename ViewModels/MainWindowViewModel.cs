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
        private readonly IDialogService<ViewModelBase> _unsavedChangesDialogService;
        
        public string Greeting => $"Welcome to Avalonia! {net.ToString()}";
        private Network net = Network.MainNet;

        private ViewModelBase dialogVM;

        public MainWindowViewModel(IDialogService<ViewModelBase> unsavedChangesDialogService)
        {
            _unsavedChangesDialogService = unsavedChangesDialogService;
            dialogVM = new DialogViewModel();


            Increase = ReactiveCommand.Create(DoIncrease);
        }

        public void ShowDialog()
        {
            var unsavedChangesDialogViewModel = new DialogServiceViewModel(dialogVM);
            _unsavedChangesDialogService.Show(unsavedChangesDialogViewModel);
        }

        public void ShowCustomDialog()
        {
            var secondDialog = new DialogServiceViewModel(new SecondDialogViewModel());
            _unsavedChangesDialogService.Show(secondDialog);
        }

        private int _currentStep = 1;
        public int CS
        {
            get => _currentStep;
            set => this.RaiseAndSetIfChanged(ref _currentStep, value);
        }


        public ReactiveCommand<Unit, Unit> Increase { get; }

        void DoIncrease()
        {
            CS += 1;
        }
        
        public ReactiveCommand<Unit, Unit> Decrease { get; }
        
    }
}
