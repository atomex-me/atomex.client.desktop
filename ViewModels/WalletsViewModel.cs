using System;
using System.Collections.ObjectModel;
using System.Linq;
using Atomex.Subsystems;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WalletsViewModel : ViewModelBase
    {
        private IAtomexApp App { get; }
        private IMenuSelector MenuSelector { get; }
        private IConversionViewModel ConversionViewModel { get; }

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

                Console.WriteLine($"Selected WalletViewModel {Selected.Header}");
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

        public WalletsViewModel(
            IAtomexApp app,
            IMenuSelector menuSelector,
            IConversionViewModel conversionViewModel)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            MenuSelector = menuSelector ?? throw new ArgumentNullException(nameof(menuSelector));
            ConversionViewModel = conversionViewModel ?? throw new ArgumentNullException(nameof(conversionViewModel));

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
                        menuSelector: MenuSelector,
                        conversionViewModel: ConversionViewModel,
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