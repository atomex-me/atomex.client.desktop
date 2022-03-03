using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.ConversionViewModels;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels
{
    public class SelectCurrencyInPortfolioViewModel : ViewModelBase
    {
        public SelectCurrencyType Type { get; set; }
        public ObservableCollection<CurrencyViewModel> Currencies { get; set; }
        [Reactive] public CurrencyViewModel? SelectedCurrency { get; set; }
        public Action<CurrencyViewModel> OnSelected { get; set; }

        public SelectCurrencyInPortfolioViewModel(SelectCurrencyType type, IEnumerable<CurrencyViewModel> currencies)
        {
            Type = type;
            Currencies = new ObservableCollection<CurrencyViewModel>(currencies);

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .SubscribeInMainThread(_ => OnSelected?.Invoke(SelectedCurrency));
        }

#if DEBUG
        public SelectCurrencyInPortfolioViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }
#endif

        private void DesignerMode()
        {
            Type = SelectCurrencyType.From;
            var random = new Random();

            var currencies = DesignTime.Currencies
                .Select(c =>
                {
                    var vm = CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false);
                    vm.AvailableAmount = random.Next(10, 1000);
                    return vm;
                })
                .ToList();

            Currencies = new ObservableCollection<CurrencyViewModel>(currencies);
        }
    }
}