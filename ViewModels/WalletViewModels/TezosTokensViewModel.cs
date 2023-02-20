using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain.Tezos.Tzkt;
using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

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
        [Reactive] public string SearchPattern { get; set; }
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
            _app.LocalStorage.BalanceChanged += OnBalanceChangedEventHandler;
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

            this.WhenAnyValue(vm => vm.SearchPattern)
                .WhereNotNull()
                .SubscribeInMainThread(searchPattern =>
                    Tokens = new ObservableCollection<TezosTokenViewModel>(InitialTokens.Where(token =>
                        {
                            if (token.TokenBalance.Name != null && token.TokenBalance.Symbol != null)
                            {
                                return token.TokenBalance.Name.ToLower().Contains(searchPattern.ToLower()) ||
                                       token.TokenBalance.Symbol.ToLower().Contains(searchPattern.ToLower()) ||
                                       token.TokenBalance.Contract.Contains(searchPattern.ToLower());
                            }

                            return token.TokenBalance.Contract.Contains(searchPattern.ToLower());
                        })
                        .Where(token => !DisabledTokens!.Contains(token.TokenBalance.Symbol))
                        .OrderByDescending(token => token.CanExchange)
                        .ThenByDescending(token => token.TotalAmountInBase)));

            DisabledTokens = _app.Account.UserData.DisabledTokens ?? Array.Empty<string>();
            HideLowBalances = _app.Account.UserData.HideTokensWithLowBalance ?? false;

            _ = ReloadTokenContractsAsync();
        }

        private async Task ReloadTokenContractsAsync()
        {
            var contracts = (await _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
                    .LocalStorage
                    .GetTokenContractsAsync())
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

        private void OnAtomexClientChanged(object? sender, AtomexClientChangedEventArgs args)
        {
            if (_app.Account != null) return;

            _app.AtomexClientChanged -= OnAtomexClientChanged;
            Contracts?.Clear();
        }

        private async void OnBalanceChangedEventHandler(object sender, BalanceChangedEventArgs args)
        {
            try
            {
                if (args is not TokenBalanceChangedEventArgs eventArgs)
                    return;

                await Dispatcher.UIThread.InvokeAsync(async () => { await ReloadTokenContractsAsync(); },
                    DispatcherPriority.Background);

                Log.Debug("Tezos tokens balances updated for contracts {@Contracts}", string.Join(',', eventArgs.Tokens.Select(p => p.Item1)));
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider)
                return;

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