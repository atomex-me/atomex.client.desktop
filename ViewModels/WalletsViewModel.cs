using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Atomex.Services;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Atomex.Core;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WalletsViewModel : ViewModelBase
    {
        private IAtomexApp App { get; }
        private Action<CurrencyConfig> SetConversionTab { get; }

        private ObservableCollection<IWalletViewModel> _wallets;

        public ObservableCollection<IWalletViewModel> Wallets
        {
            get => _wallets;
            set
            {
                _wallets = value;
                this.RaisePropertyChanged(nameof(Wallets));
            }
        }

        private IWalletViewModel _selected;

        public IWalletViewModel Selected
        {
            get => _selected;
            set
            {
                if (_selected != null)
                    _selected.IsSelected = false;

                _selected = value;

                if (_selected != null)
                    _selected.IsSelected = true;
                
                this.RaisePropertyChanged(nameof(Wallets));
            }
        }

        public WalletsViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public WalletsViewModel(IAtomexApp app,  Action<CurrencyConfig> setConversionTab)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            SetConversionTab = setConversionTab;

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.AtomexClientChanged += OnTerminalChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs e)
        {
            var walletsViewModels = new List<IWalletViewModel>();

            if (e.AtomexClient?.Account != null)
            {
                var currenciesViewModels = e.AtomexClient.Account.Currencies
                    .Select(currency => WalletViewModelCreator.CreateViewModel(
                        app: App,
                        setConversionTab: SetConversionTab,
                        currency: currency));

                walletsViewModels.AddRange(currenciesViewModels);

                walletsViewModels.Add(new TezosTokensWalletViewModel(
                    app: App,
                    setConversionTab: SetConversionTab));
            }

            Wallets  = new ObservableCollection<IWalletViewModel>(walletsViewModels);
            Selected = Wallets.FirstOrDefault();
        }

        private void DesignerMode()
        {
            // var currencies = DesignTime.Currencies.ToList();
            //
            // Wallets = new ObservableCollection<WalletViewModel>
            // {
            //     new WalletViewModel {CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currencies[0], subscribeToUpdates: false)},
            //     new WalletViewModel {CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currencies[1], subscribeToUpdates: false)}
            // };
        }
    }
}