using System;

namespace Atomex.Client.Desktop.ViewModels
{
    public class DialogViewModel : ViewModelBase
    {
        private ViewModelBase PreviousVM;
        public DialogViewModel(ViewModelBase previousVM)
        {
            Console.WriteLine("Creating DialogViewModel");
            PreviousVM = previousVM;
        }

        public string cnt { get; set; } = "DialogViewModel content";

        public void OnPortfolio()
        {
            App.DialogService.Show(PreviousVM);
        }
    }
}