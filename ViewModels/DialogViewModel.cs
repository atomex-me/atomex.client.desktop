using System;

namespace Atomex.Client.Desktop.ViewModels
{
    public class DialogViewModel : ViewModelBase
    {
        public DialogViewModel()
        {
            Console.WriteLine("Creating DialogViewModel");
        }

        public string cnt { get; set; } = "DialogViewModel content";

        public void OnPortfolio()
        {
            Console.WriteLine("Portfolio click;");
        }
    }
}