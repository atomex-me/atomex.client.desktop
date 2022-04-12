using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Atomex.Client.Desktop.Common;
using ReactiveUI;
using Atomex.Core;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.Services;
using Atomex.Services.Abstract;
using ReactiveUI.Fody.Helpers;

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
            
            this.WhenAnyValue(vm => vm.Content)
                .Select(content => content is ViewModels.PortfolioViewModel or ViewModels.WalletsViewModel)
                .ToPropertyExInMainThread(this, vm => vm.IsPortfolioSectionActive);
            
            this.WhenAnyValue(vm => vm.Content)
                .Select(content => content is ConversionViewModel)
                .ToPropertyExInMainThread(this, vm => vm.IsConversionSectionActive);
                        
            this.WhenAnyValue(vm => vm.Content)
                .Select(content => content is SettingsViewModel)
                .ToPropertyExInMainThread(this, vm => vm.IsSettingsSectionActive);
            
            this.WhenAnyValue(vm => vm.Content)
                .Select(content => content is WertViewModel)
                .ToPropertyExInMainThread(this, vm => vm.IsWertSectionActive);

            this.WhenAnyValue(vm => vm.RightPopupContent)
                .Select(content => content != null)
                .ToPropertyExInMainThread(this, vm => vm.RightPopupOpened);

            PortfolioViewModel = new PortfolioViewModel(AtomexApp)
            {
                SetDexTab = SelectConversion,
                SetWalletCurrency = SelectCurrencyWallet,
                SetWertCurrency = SelectWert
            };
            WalletsViewModel = new WalletsViewModel(AtomexApp)
            {
                SetConversionTab = SelectConversion,
                SetWertCurrency = SelectWert,
                BackAction = SelectPortfolio,
                ShowRightPopupContent = ShowRightPopupContent
            };
            ConversionViewModel = new ConversionViewModel(AtomexApp)
            {
                ShowRightPopupContent = ShowRightPopupContent
            };

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

        [Reactive] public ViewModelBase? RightPopupContent { get; set; }
        [Reactive] public ViewModelBase Content { get; set; }
        [Reactive] public string InstalledVersion { get; set; }
        [Reactive] public bool IsExchangeConnected { get; set; }
        [Reactive] public bool IsMarketDataConnected { get; set; }
        [Reactive] public bool IsQuotesProviderAvailable { get; set; }
        [ObservableAsProperty] public bool IsPortfolioSectionActive { get; }
        [ObservableAsProperty] public bool IsConversionSectionActive { get; }
        [ObservableAsProperty] public bool IsSettingsSectionActive { get; }
        [ObservableAsProperty] public bool IsWertSectionActive { get; }
        [ObservableAsProperty] public bool RightPopupOpened { get; }


        private void SubscribeToServices()
        {
            AtomexApp.AtomexClientChanged += OnTerminalChangedEventHandler;
            AtomexApp.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {
            var terminal = args.AtomexClient;
            if (terminal?.Account == null)
            {
                SelectPortfolio();
                return;
            }

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
            if (args.Service != TerminalService.MarketData || !IsMarketDataConnected) return;
            
            terminal.SubscribeToMarketData(SubscriptionType.TopOfBook);
            terminal.SubscribeToMarketData(SubscriptionType.DepthTwenty);
        }

        private void OnQuotesProviderAvailabilityChangedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider provider)
                return;

            IsQuotesProviderAvailable = provider.IsAvailable;
        }

        private void ShowRightPopupContent(ViewModelBase? content)
        {
            RightPopupContent = content;
        }
        
        public void SelectPortfolio()
        {
            Content = PortfolioViewModel;
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
        }

        public void SelectConversion(CurrencyConfig? fromCurrency = null)
        {
            Content = ConversionViewModel;
            if (fromCurrency != null)
            {
                ConversionViewModel.SetFromCurrency(fromCurrency);
            }
        }

        public void SelectSettings()
        {
            Content = SettingsViewModel;
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
        }

        private static string GetAssemblyFileVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersion.FileVersion;
        }
    }
}