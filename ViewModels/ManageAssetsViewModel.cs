using System.Collections.Generic;
using System.Collections.ObjectModel;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ManageAssetsViewModel : ViewModelBase
    {
        public ObservableCollection<CurrencyViewModel> AvailableCurrencies { get; set; }
        public ObservableCollection<CurrencyViewModel> SelectedCurrencies { get; set; }
        [Reactive] public string SearchPattern { get; set; }

        public ManageAssetsViewModel()
        {
            SelectedCurrencies = new ObservableCollection<CurrencyViewModel>();
        }
    }
}