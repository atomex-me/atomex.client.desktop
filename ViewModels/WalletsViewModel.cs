using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Atomex.Services;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Atomex.Core;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WalletsViewModel : ViewModelBase
    {
        private IAtomexApp App { get; }
        public Action<CurrencyConfig> SetConversionTab { get; init; }
        public Action<string> SetWertCurrency { get; init; }
        public Action BackAction { get; init; }
        public Action<ViewModelBase?> ShowRightPopupContent { get; set; }
        [Reactive] public ObservableCollection<IWalletViewModel> Wallets { get; private set; }
        [Reactive] public IWalletViewModel Selected { get; set; }

        public WalletsViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public WalletsViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            
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
                        setWertCurrency: SetWertCurrency,
                        showRightPopupContent: ShowRightPopupContent,
                        currency: currency));

                walletsViewModels.AddRange(currenciesViewModels);

                // walletsViewModels.Add(new TezosTokensWalletViewModel(
                //     app: App,
                //     setConversionTab: SetConversionTab,
                //     setWertCurrency: SetWertCurrency));
            }

            Wallets = new ObservableCollection<IWalletViewModel>(walletsViewModels);
        }

        private ReactiveCommand<Unit, Unit> _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??=
            (_backCommand = ReactiveCommand.Create(() => BackAction?.Invoke()));

        private void DesignerMode()
        {
            var currencies = DesignTime.TestNetCurrencies.ToList();

            Wallets = new ObservableCollection<IWalletViewModel>
            {
                new WalletViewModel
                {
                    CurrencyViewModel =
                        CurrencyViewModelCreator.CreateViewModel(currencies[0], subscribeToUpdates: false)
                },
                new WalletViewModel
                {
                    CurrencyViewModel =
                        CurrencyViewModelCreator.CreateViewModel(currencies[1], subscribeToUpdates: false)
                }
            };

            Selected = Wallets[1];
        }
    }
}