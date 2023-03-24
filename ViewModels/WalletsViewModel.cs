using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WalletsViewModel : ViewModelBase
    {
        private IAtomexApp App { get; }
        public Action<CurrencyConfig> SetConversionTab { get; init; }
        public Action<string>? SetWertCurrency { get; init; }
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
            App.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object? sender, AtomexClientChangedEventArgs e)
        {
            var walletsViewModels = new List<IWalletViewModel>();

            if (e.AtomexClient != null && App?.Account != null)
            {
                var wallets = App.Account
                    .Currencies
                    .GetOrderedPreset()
                    .Select(currency => WalletViewModelCreator.CreateViewModel(
                        app: App,
                        setConversionTab: SetConversionTab,
                        setWertCurrency: SetWertCurrency,
                        showRightPopupContent: ShowRightPopupContent,
                        showTezosToken: ShowTezosToken,
                        showTezosCollection: ShowTezosCollection,
                        currency: currency))
                    .ToList();

                walletsViewModels.AddRange(wallets);
                walletsViewModels.Add(new TezosTokenWalletViewModel(App, ShowRightPopupContent));
                walletsViewModels.Add(new CollectiblesWalletViewModel(ShowTezosCollectible));
                walletsViewModels.Add(new CollectibleWalletViewModel(App, ShowRightPopupContent));
            }
            else
            {
                CurrencyViewModelCreator.Reset();
                TezosTokenViewModelCreator.Reset();
            }

            Wallets = new ObservableCollection<IWalletViewModel>(walletsViewModels);
        }

        private void ShowTezosToken(TezosTokenViewModel tokenViewModel)
        {
            var walletViewModel = Wallets.FirstOrDefault(w => w is TezosTokenWalletViewModel);
            if (walletViewModel is not TezosTokenWalletViewModel tezosTokenWalletViewModel) return;

            tezosTokenWalletViewModel.TokenViewModel = tokenViewModel;
            Selected = tezosTokenWalletViewModel;
        }

        private void ShowTezosCollection(IEnumerable<TezosTokenViewModel> tokens)
        {
            var walletViewModel = Wallets.FirstOrDefault(w => w is CollectiblesWalletViewModel);
            if (walletViewModel is not CollectiblesWalletViewModel collectiblesWalletViewModel) return;

            collectiblesWalletViewModel.Tokens = new ObservableCollection<TezosTokenViewModel>(tokens);
            collectiblesWalletViewModel.InitialTokens = new ObservableCollection<TezosTokenViewModel>(tokens);
            Selected = collectiblesWalletViewModel;
        }

        private void ShowTezosCollectible(TezosTokenViewModel tokenViewModel)
        {
            var walletViewModel = Wallets.FirstOrDefault(w => w is CollectibleWalletViewModel);
            if (walletViewModel is not CollectibleWalletViewModel collectibleWalletViewModel) return;

            collectibleWalletViewModel.TokenViewModel = tokenViewModel;
            Selected = collectibleWalletViewModel;
        }

        private ReactiveCommand<Unit, Unit>? _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??= _backCommand = ReactiveCommand.Create(() =>
        {
            switch (Selected)
            {
                case CollectibleWalletViewModel collectibleWalletViewModel:
                    collectibleWalletViewModel.TokenViewModel = null;
                    Selected = Wallets.First(wallet => wallet is CollectiblesWalletViewModel);
                    return;

                case TezosTokenWalletViewModel tokenWalletViewModel:
                    tokenWalletViewModel.TokenViewModel = null;
                    Selected = Wallets.First(wallet => wallet is TezosWalletViewModel);
                    return;

                case CollectiblesWalletViewModel:
                    Selected = Wallets.First(wallet => wallet is TezosWalletViewModel);
                    return;

                default:
                    BackAction?.Invoke();
                    break;
            }
        });

        private void DesignerMode()
        {
            var currencies = DesignTime.TestNetCurrencies.ToList();

            Wallets = new ObservableCollection<IWalletViewModel>
            {
                new WalletViewModel
                {
                    CurrencyViewModel =
                        CurrencyViewModelCreator.CreateOrGet(currencies[0], subscribeToUpdates: false)
                },
                new WalletViewModel
                {
                    CurrencyViewModel =
                        CurrencyViewModelCreator.CreateOrGet(currencies[1], subscribeToUpdates: false)
                }
            };

            Selected = Wallets[1];
        }
    }
}