using System;
using System.Diagnostics;

using ReactiveUI;

using Atomex.Core;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.Services;
using Atomex.Services.Abstract;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WalletMainViewModel : ViewModelBase
    {
        public WalletMainViewModel()
        {
        }

        public WalletMainViewModel(IAtomexApp app)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            
            PortfolioViewModel = new PortfolioViewModel(AtomexApp);
            ConversionViewModel = new ConversionViewModel(AtomexApp);
            WalletsViewModel = new WalletsViewModel(AtomexApp, SelectConversion);
            SettingsViewModel = new SettingsViewModel(AtomexApp);
            WertViewModel = new WertViewModel(AtomexApp);

            SelectPortfolio();
            SubscribeToServices();
            
            InstalledVersion = GetAssemblyFileVersion();
        }

        public IAtomexApp AtomexApp { get; set; }
        public PortfolioViewModel PortfolioViewModel { get; set; }
        public WalletsViewModel WalletsViewModel { get; set; }
        public ConversionViewModel ConversionViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }
        public WertViewModel WertViewModel { get; set; }
        
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
            AtomexApp.AtomexClientChanged += OnAtomexClientChangedEventHandler;
            AtomexApp.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
        }
        
        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var atomexClient = args.AtomexClient;
            if (atomexClient?.Account == null)
                return;

            atomexClient.ServiceConnected += OnAtomexClientServiceStateChangedEventHandler;
            atomexClient.ServiceDisconnected += OnAtomexClientServiceStateChangedEventHandler;
        }
        
        private void OnAtomexClientServiceStateChangedEventHandler(object sender, AtomexClientServiceEventArgs args)
        {
            if (sender is not IAtomexClient atomexClient)
                return;
            
            IsExchangeConnected = atomexClient.IsServiceConnected(AtomexClientService.Exchange);
            IsMarketDataConnected = atomexClient.IsServiceConnected(AtomexClientService.MarketData);
            
            // subscribe to symbols updates
            if (args.Service == AtomexClientService.MarketData && IsMarketDataConnected)
            {
                atomexClient.SubscribeToMarketData(SubscriptionType.TopOfBook);
                atomexClient.SubscribeToMarketData(SubscriptionType.DepthTwenty);
            }
        }
        
        private void OnQuotesProviderAvailabilityChangedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider provider)
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
            this.RaisePropertyChanged(nameof(IsWertSectionActive));
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
        
        public void SelectConversion(CurrencyConfig? fromCurrency = null)
        {
            Content = ConversionViewModel;
            if (fromCurrency != null)
            {
                ConversionViewModel.SetFromCurrency(fromCurrency);
            }
            RefreshAllMenus();
        }
        
        public void SelectSettings()
        {
            Content = SettingsViewModel;
            RefreshAllMenus();
        }
        
        public void SelectWert()
        {
            Content = WertViewModel;
            RefreshAllMenus();
        }

        public void SurveyCommand()
        {
            App.OpenBrowser("https://forms.gle/TACSt9JDJSd7tZyk9");
        }

        public bool IsPortfolioSectionActive => Content.GetType() == typeof(PortfolioViewModel);
        public bool IsWalletsSectionActive => Content.GetType() == typeof(WalletsViewModel);
        public bool IsConversionSectionActive => Content.GetType() == typeof(ConversionViewModel);
        public bool IsSettingsSectionActive => Content.GetType() == typeof(SettingsViewModel);
        public bool IsWertSectionActive => Content.GetType() == typeof(WertViewModel);
        
        public static string GetAssemblyFileVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersion.FileVersion;
        }
    }
}