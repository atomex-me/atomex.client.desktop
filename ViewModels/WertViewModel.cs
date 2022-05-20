using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Atomex.Client.Desktop.Api;
using Atomex.Client.Desktop.Common;
using Atomex.Services;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WertViewModel : ViewModelBase
    {
        public static IEnumerable<string> CurrenciesToBuy => new[] { "BTC", "ETH", "XTZ" };
        private IAtomexApp App { get; }

        private ObservableCollection<WertCurrencyViewModel> _wallets;

        public ObservableCollection<WertCurrencyViewModel> Wallets
        {
            get => _wallets;
            set
            {
                _wallets = value;
                OnPropertyChanged(nameof(Wallets));
            }
        }

        private WertCurrencyViewModel _selected;

        public WertCurrencyViewModel Selected
        {
            get => _selected;
            set
            {
                if (_selected != null)
                    _selected.IsSelected = false;

                _selected = value;

                if (_selected != null)
                    _selected.IsSelected = true;

                OnPropertyChanged(nameof(Wallets));
            }
        }

        public WertViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public WertViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs e)
        {
            var wertApi = new WertApi(App);

            Wallets = e.AtomexClient?.Account != null
                ? new ObservableCollection<WertCurrencyViewModel>(
                    e.AtomexClient.Account.Currencies
                        .Where(currency => CurrenciesToBuy.Contains(currency.Name))
                        .Select(currency => new WertCurrencyViewModel(currency, App, wertApi)))
                : new ObservableCollection<WertCurrencyViewModel>();

            Selected = Wallets.FirstOrDefault();
        }

        private void DesignerMode()
        {
        }
    }
}