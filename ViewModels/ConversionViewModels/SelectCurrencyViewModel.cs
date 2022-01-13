using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

using Avalonia.Controls;
using ReactiveUI;
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
        public Action<SelectCurrencyViewModelItem> CurrencySelected;

        [Reactive] public ObservableCollection<SelectCurrencyViewModelItem> Currencies { get; set; }

        [Reactive] public SelectCurrencyViewModelItem SelectedCurrency { get; set; }

        private ICommand _changeAddressesCommand;
        public ICommand ChangeAddressesCommand => _changeAddressesCommand ??= ReactiveCommand.Create<SelectCurrencyViewModelItem>(i =>
        {
            // todo: change addresses
        });

#if DEBUG
        public SelectCurrencyViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .Subscribe(i => {
                    CurrencySelected?.Invoke(i);
                });
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