using Atomex.Core;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.Dialogs.ViewModels;

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
    }
}
