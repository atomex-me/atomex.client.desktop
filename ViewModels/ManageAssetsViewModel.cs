using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Atomex.Client.Desktop.ViewModels
{
    public class CurrencyWithSelection : ViewModelBase
    {
        public CurrencyViewModel Currency { get; set; }
        [Reactive] public bool IsSelected { get; set; }
        public Action OnChanged { get; set; }

        public CurrencyWithSelection()
        {
            this.WhenAnyValue(vm => vm.IsSelected)
                .SubscribeInMainThread(_ => OnChanged?.Invoke());
        }
    }

    public class ManageAssetsViewModel : ViewModelBase
    {
        private ObservableCollection<CurrencyWithSelection> InitialCurrencies { get; set; }
        private ObservableCollection<CurrencyWithSelection> BeforeSearchCurrencies { get; set; }
        [Reactive] public ObservableCollection<CurrencyWithSelection> AvailableCurrencies { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        public Action<IEnumerable<CurrencyViewModel>> OnAssetsChanged { get; set; }

        public ManageAssetsViewModel()
        {
            this.WhenAnyValue(vm => vm.AvailableCurrencies)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(_ =>
                {
                    AvailableCurrencies.ForEachDo(curr => curr.OnChanged = () =>
                    {
                        OnAssetsChanged?.Invoke(AvailableCurrencies
                            .Where(currency => currency.IsSelected)
                            .Select(currencyWithSelection => currencyWithSelection.Currency));
                    });

                    InitialCurrencies = new ObservableCollection<CurrencyWithSelection>(AvailableCurrencies);
                });

            this.WhenAnyValue(vm => vm.SearchPattern)
                .WhereNotNull()
                .SubscribeInMainThread(searchPattern =>
                {
                    if (searchPattern == string.Empty)
                        BeforeSearchCurrencies = new ObservableCollection<CurrencyWithSelection>(AvailableCurrencies);

                    var filteredCurrencies = InitialCurrencies
                        .Where(c => c.Currency.Currency.Name.ToLower()
                                        .Contains(searchPattern?.ToLower() ?? string.Empty) ||
                                    c.Currency.Currency.Description.ToLower()
                                        .Contains(searchPattern?.ToLower() ?? string.Empty));

                    AvailableCurrencies = new ObservableCollection<CurrencyWithSelection>(filteredCurrencies);
                });

            SearchPattern = string.Empty;
        }
    }
}