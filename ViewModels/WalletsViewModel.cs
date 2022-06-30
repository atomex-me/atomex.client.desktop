using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Atomex.Core;

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

        // todo: remove
        [Reactive] private bool IsTezosTokensSelected { get; set; }

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

            // todo: remove
            this.WhenAnyValue(vm => vm.Selected)
                .WhereNotNull()
                .Select(walletViewModel => walletViewModel is TezosTokensWalletViewModel)
                .SubscribeInMainThread(res => IsTezosTokensSelected = res);

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.AtomexClientChanged += OnAtomexClientChangedEventHandler;
        }

        private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs e)
        {
            var walletsViewModels = new List<IWalletViewModel>();

            if (e.AtomexClient != null && App?.Account != null)
            {
                var wallets = App.Account.Currencies
                    .Select(currency => WalletViewModelCreator.CreateViewModel(
                        app: App,
                        setConversionTab: SetConversionTab,
                        setWertCurrency: SetWertCurrency,
                        showRightPopupContent: ShowRightPopupContent,
                        showTezosToken: ShowTezosToken,
                        currency: currency))
                    .ToList();

                walletsViewModels.AddRange(wallets);
                walletsViewModels.Add(new TezosTokenWalletViewModel(App, ShowRightPopupContent));
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
            if (Wallets.Last() is not TezosTokenWalletViewModel tezosTokenWalletViewModel) return;

            tezosTokenWalletViewModel.TokenViewModel = tokenViewModel;
            Selected = tezosTokenWalletViewModel;
        }

        private ReactiveCommand<Unit, Unit> _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??=
            (_backCommand = ReactiveCommand.Create(() =>
            {
                if (Selected is TezosTokenWalletViewModel tokenWalletViewModel)
                {
                    tokenWalletViewModel.TokenViewModel = null;
                    Selected = Wallets.First(wallet => wallet is TezosWalletViewModel);
                    return;
                }

                BackAction?.Invoke();
            }));

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