using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;

namespace Atomex.Client.Desktop.ViewModels.ConversionViewModels
{
    public class SelectCurrencyViewModelItem : ViewModelBase
    {
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }

        public SelectCurrencyViewModelItem(CurrencyViewModel currencyViewModel)
        {
            CurrencyViewModel = currencyViewModel;
        }
    }

    public class SelectCurrencyViewModel : ViewModelBase
    {
        [Reactive] public ObservableCollection<SelectCurrencyViewModelItem> Currencies { get; set; }

#if DEBUG
        public SelectCurrencyViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }
#endif

#if DEBUG
        public void DesignerMode()
        {
            var currencies = DesignTime.Currencies
                .Select(c => new SelectCurrencyViewModelItem(
                    CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false)));

            Currencies = new ObservableCollection<SelectCurrencyViewModelItem>(currencies);
        }
#endif
    }
}