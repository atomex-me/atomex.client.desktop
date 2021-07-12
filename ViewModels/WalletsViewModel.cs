using System;
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
        private Action<Currency> SetConversionTab { get; }

        private ObservableCollection<WalletViewModel> _wallets;

        public ObservableCollection<WalletViewModel> Wallets
        {
            get => _wallets;
            set
            {
                _wallets = value;
                this.RaisePropertyChanged(nameof(Wallets));
            }
        }

        private WalletViewModel _selected;

        public WalletViewModel Selected
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

        public WalletsViewModel(IAtomexApp app,  Action<Currency> setConversionTab)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            SetConversionTab = setConversionTab;

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.TerminalChanged += OnTerminalChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs e)
        {
            Wallets = e.Terminal?.Account != null
                ? new ObservableCollection<WalletViewModel>(
                    e.Terminal.Account.Currencies.Select(currency => WalletViewModelCreator.CreateViewModel(
                        app: App,
                        setConversionTab: SetConversionTab,
                        currency: currency)))
                : new ObservableCollection<WalletViewModel>();

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