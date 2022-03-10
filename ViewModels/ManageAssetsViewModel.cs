using System.Collections.ObjectModel;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using ReactiveUI.Fody.Helpers;


namespace Atomex.Client.Desktop.ViewModels
{
    public class CurrencyWithSelection : ViewModelBase
    {
        public CurrencyViewModel Currency { get; set; }
        [Reactive] public bool IsSelected { get; set; }
    }

    public class ManageAssetsViewModel : ViewModelBase
    {
        [Reactive] public ObservableCollection<CurrencyWithSelection> AvailableCurrencies { get; set; }
        [Reactive] public string SearchPattern { get; set; }
    }
}