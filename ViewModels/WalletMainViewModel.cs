using System;
using Atomex.Client.Desktop.Dialogs.ViewModels;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.Subsystems;
using Atomex.Subsystems.Abstract;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WalletMainViewModel : ViewModelBase, IMenuSelector
    {
        public WalletMainViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            
            PortfolioViewModel = new PortfolioViewModel(AtomexApp);
            ConversionViewModel = new ConversionViewModel(AtomexApp);
            WalletsViewModel = new WalletsViewModel(AtomexApp, this, ConversionViewModel);
            SettingsViewModel = new SettingsViewModel(AtomexApp);

            SelectPortfolio();

            // InstalledVersion = App.Updater.InstalledVersion.ToString();
            InstalledVersion = "1.0.72";

            SubscribeToServices();
        }

        public IAtomexApp AtomexApp { get; set; }
        public PortfolioViewModel PortfolioViewModel { get; set; }
        public WalletsViewModel WalletsViewModel { get; set; }
        public ConversionViewModel ConversionViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }
        
        private ViewModelBase _content;

        public ViewModelBase Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        private int _selectedMenuIndex;

        public int SelectedMenuIndex
        {
            get => _selectedMenuIndex;
            set
            {
                _selectedMenuIndex = value;
                this.RaisePropertyChanged(nameof(SelectedMenuIndex));
            }
        }

        private string _installedVersion;

        public string InstalledVersion
        {
            get => _installedVersion;
            set
            {
                _installedVersion = value;
                this.RaisePropertyChanged(nameof(SelectedMenuIndex));
            }
        }

        private bool _updatesReady;

        public bool UpdatesReady
        {
            get => _updatesReady;
            set
            {
                _updatesReady = value;
                this.RaisePropertyChanged(nameof(SelectedMenuIndex));
            }
        }

        private bool _isExchangeConnected;

        public bool IsExchangeConnected
        {
            get => _isExchangeConnected;
            set
            {
                _isExchangeConnected = value;
                this.RaisePropertyChanged(nameof(IsExchangeConnected));
            }
        }

        private bool _isMarketDataConnected;

        public bool IsMarketDataConnected
        {
            get => _isMarketDataConnected;
            set
            {
                _isMarketDataConnected = value;
                this.RaisePropertyChanged(nameof(IsMarketDataConnected));
            }
        }

        private bool _isQuotesProviderAvailable;

        public bool IsQuotesProviderAvailable
        {
            get => _isQuotesProviderAvailable;
            set
            {
                _isQuotesProviderAvailable = value;
                this.RaisePropertyChanged(nameof(IsQuotesProviderAvailable));
            }
        }
        
        
        private void SubscribeToServices()
        {
            AtomexApp.TerminalChanged += OnTerminalChangedEventHandler;
            AtomexApp.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
        }
        
        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs args)
        {
            var terminal = args.Terminal;
            if (terminal?.Account == null)
                return;

            terminal.ServiceConnected += OnTerminalServiceStateChangedEventHandler;
            terminal.ServiceDisconnected += OnTerminalServiceStateChangedEventHandler;
        }
        
        private void OnTerminalServiceStateChangedEventHandler(object sender, TerminalServiceEventArgs args)
        {
            if (!(sender is IAtomexClient terminal))
                return;
            
            IsExchangeConnected = terminal.IsServiceConnected(TerminalService.Exchange);
            IsMarketDataConnected = terminal.IsServiceConnected(TerminalService.MarketData);
            
            // subscribe to symbols updates
            if (args.Service == TerminalService.MarketData && IsMarketDataConnected)
            {
                terminal.SubscribeToMarketData(SubscriptionType.TopOfBook);
                terminal.SubscribeToMarketData(SubscriptionType.DepthTwenty);
            }
        }
        
        private void OnQuotesProviderAvailabilityChangedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;
            
            IsQuotesProviderAvailable = provider.IsAvailable;
        }
        
        
        public void SelectMenu(int index)
        {
            SelectedMenuIndex = index;
        }

        private void RefreshAllMenus()
        {
            this.RaisePropertyChanged(nameof(IsPortfolioSectionActive));
            this.RaisePropertyChanged(nameof(IsWalletsSectionActive));
            this.RaisePropertyChanged(nameof(IsConversionSectionActive));
            this.RaisePropertyChanged(nameof(IsSettingsSectionActive));
        }


        public void SelectPortfolio()
        {
            Content = PortfolioViewModel;
            RefreshAllMenus();
        }
        
        public void SelectWallets()
        {
            Content = WalletsViewModel;
            RefreshAllMenus();
        }
        
        public void SelectConversion()
        {
            Content = ConversionViewModel;
            RefreshAllMenus();
        }
        
        public void SelectSettings()
        {
            Content = SettingsViewModel;
            RefreshAllMenus();
        }

        public bool IsPortfolioSectionActive => Content.GetType() == typeof(PortfolioViewModel);
        public bool IsWalletsSectionActive => Content.GetType() == typeof(WalletsViewModel);
        public bool IsConversionSectionActive => Content.GetType() == typeof(ConversionViewModel);
        public bool IsSettingsSectionActive => Content.GetType() == typeof(SettingsViewModel);
    }
}