using System;
using System.Diagnostics;
using System.Linq;
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

            PortfolioViewModel = new PortfolioViewModel(AtomexApp)
            {
                SetDexTab = SelectConversion,
                SetWalletCurrency = SelectCurrencyWallet,
                SetWertCurrency = SelectWert
            };
            WalletsViewModel = new WalletsViewModel(AtomexApp)
            {
                SetConversionTab = SelectConversion,
                BackAction = SelectPortfolio
            };
            ConversionViewModel = new ConversionViewModel(AtomexApp);
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
            AtomexApp.AtomexClientChanged += OnTerminalChangedEventHandler;
            AtomexApp.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var terminal = args.AtomexClient;
            if (terminal?.Account == null)
                return;

            terminal.ServiceConnected += OnTerminalServiceStateChangedEventHandler;
            terminal.ServiceDisconnected += OnTerminalServiceStateChangedEventHandler;
        }

        private void OnTerminalServiceStateChangedEventHandler(object sender, TerminalServiceEventArgs args)
        {
            if (sender is not IAtomexClient terminal)
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
            if (sender is not ICurrencyQuotesProvider provider)
                return;

            IsQuotesProviderAvailable = provider.IsAvailable;
        }

        private void RefreshAllMenus()
        {
            this.RaisePropertyChanged(nameof(IsPortfolioSectionActive));
            this.RaisePropertyChanged(nameof(IsConversionSectionActive));
            this.RaisePropertyChanged(nameof(IsSettingsSectionActive));
            this.RaisePropertyChanged(nameof(IsWertSectionActive));
        }


        public void SelectPortfolio()
        {
            Content = PortfolioViewModel;
            RefreshAllMenus();
        }

        private void SelectCurrencyWallet(string? currencyDescription = null)
        {
            if (currencyDescription != null)
            {
                WalletsViewModel.Selected = WalletsViewModel
                    .Wallets
                    .First(wallet => wallet.Header == currencyDescription);
            }

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

        public void SelectWert(string? currencyDescription = null)
        {
            if (currencyDescription != null)
            {
                WertViewModel.Selected = WertViewModel
                    .Wallets
                    .First(wallet => wallet.Header == currencyDescription);
            }

            Content = WertViewModel;
            RefreshAllMenus();
        }

        public bool IsPortfolioSectionActive => Content.GetType() == typeof(PortfolioViewModel) ||
                                                Content.GetType() == typeof(WalletsViewModel);

        public bool IsConversionSectionActive => Content.GetType() == typeof(ConversionViewModel);
        public bool IsSettingsSectionActive => Content.GetType() == typeof(SettingsViewModel);
        public bool IsWertSectionActive => Content.GetType() == typeof(WertViewModel);

        public static string GetAssemblyFileVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersion.FileVersion;
        }
    }
}