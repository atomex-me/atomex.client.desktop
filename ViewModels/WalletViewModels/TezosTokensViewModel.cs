using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;


namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokensViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        private Action<TezosTokenViewModel> ShowTezosToken { get; }
        private Action<CurrencyConfig> SetConversionTab { get; }
        [Reactive] public bool HideLowBalances { get; set; }
        [Reactive] public string[] DisabledTokens { get; set; }
        [Reactive] private ObservableCollection<TokenContract>? Contracts { get; set; }
        [Reactive] public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public ObservableCollection<TezosTokenViewModel> InitialTokens { get; set; }


        public TezosTokensViewModel(IAtomexApp app,
            Action<TezosTokenViewModel> showTezosToken,
            Action<CurrencyConfig> setConversionTab)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            ShowTezosToken = showTezosToken ?? throw new ArgumentNullException(nameof(showTezosToken));
            SetConversionTab = setConversionTab ?? throw new ArgumentNullException(nameof(setConversionTab));

            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
            _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;

            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .SubscribeInMainThread(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.DisabledTokens)
                .WhereNotNull()
                .Skip(1)
                .SubscribeInMainThread(disabledTokens =>
                {
                    _app.Account.UserData.DisabledTokens = disabledTokens;
                    _app.Account.UserData.SaveToFile(_app.Account.SettingsFilePath);
                    OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
                });

            this.WhenAnyValue(vm => vm.HideLowBalances)
                .WhereNotNull()
                .Skip(1)
                .SubscribeInMainThread(hideLowBalances =>
                {
                    _app.Account.UserData.HideTokensWithLowBalance = hideLowBalances;
                    _app.Account.UserData.SaveToFile(_app.Account.SettingsFilePath);
                    OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
                });

            DisabledTokens = _app.Account.UserData.DisabledTokens ?? Array.Empty<string>();
            HideLowBalances = _app.Account.UserData.HideTokensWithLowBalance ?? false;

            _ = ReloadTokenContractsAsync();
        }

        private async Task ReloadTokenContractsAsync()
        {
            var contracts = (await _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
                    .DataRepository
                    .GetTezosTokenContractsAsync())
                .ToList();

            Contracts = new ObservableCollection<TokenContract>(contracts);
        }

        private async Task<IEnumerable<TezosTokenViewModel>> LoadTokens()
        {
            var tokens = new ObservableCollection<TezosTokenViewModel>();

            foreach (var contract in Contracts!)
                tokens.AddRange(await TezosTokenViewModelCreator.CreateOrGet(_app, contract, false, SetConversionTab));

            return tokens.OrderByDescending(token => token.CanExchange)
                .ThenByDescending(token => token.TotalAmountInBase);
        }


        private ReactiveCommand<TezosTokenViewModel, Unit>? _setTokenCommand;

        private ReactiveCommand<TezosTokenViewModel, Unit> SetTokenCommand => _setTokenCommand ??=
            ReactiveCommand.Create<TezosTokenViewModel>(
                tezosTokenViewModel => ShowTezosToken.Invoke(tezosTokenViewModel));

        private void OnAtomexClientChanged(object sender, AtomexClientChangedEventArgs e)
        {
            Contracts?.Clear();
        }

        private async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (!args.IsTokenUpdate)
                    return;

                await Dispatcher.UIThread.InvokeAsync(async () => { await ReloadTokenContractsAsync(); },
                    DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider) return;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var tezosTokenViewModels = (await LoadTokens()).ToList();

                InitialTokens = new ObservableCollection<TezosTokenViewModel>(tezosTokenViewModels);
                Tokens = new ObservableCollection<TezosTokenViewModel>(tezosTokenViewModels
                    .Where(token => !DisabledTokens.Contains(token.TokenBalance.Symbol))
                    .Where(token => !HideLowBalances || token.TotalAmountInBase > Constants.MinBalanceForTokensUsd)
                );
            }, DispatcherPriority.Background);
        }

        public TezosTokensViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }
#if DEBUG
        private void DesignerMode()
        {
        }
#endif
    }
}