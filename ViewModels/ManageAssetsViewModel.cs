using System.Collections.Generic;
using System.Collections.ObjectModel;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels
{
    public class CurrencyWithSelection
    {
        public CurrencyViewModel Currency { get; set; }
        [Reactive] public bool IsSelected { get; set; }
    }

    public class ManageAssetsViewModel : ViewModelBase
    {
        public ObservableCollection<CurrencyWithSelection> AvailableCurrencies { get; set; }
        public ObservableCollection<CurrencyWithSelection> SelectedCurrencies { get; set; }
        [Reactive] public string SearchPattern { get; set; }

        public ManageAssetsViewModel()
        {
            SelectedCurrencies = new ObservableCollection<CurrencyWithSelection>();
        }
    }
}