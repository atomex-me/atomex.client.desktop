using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Services;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Threading;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokensViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        private Action<TezosTokenViewModel> ShowTezosToken { get; }
        private Action<CurrencyConfig> SetConversionTab { get; }
        [Reactive] private ObservableCollection<TokenContract>? Contracts { get; set; }
        [Reactive] public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }


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
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var tokens = new ObservableCollection<TezosTokenViewModel>();

            foreach (var contract in Contracts!)
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tokenWalletAddresses = await tezosAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(contract.Address);

                var tokenGroups = tokenWalletAddresses
                    .Where(walletAddress => walletAddress.Balance != 0)
                    .GroupBy(walletAddress => new
                    {
                        walletAddress.TokenBalance.TokenId,
                        walletAddress.TokenBalance.Contract
                    });

                var tokensViewModels = tokenGroups
                    .Select(walletAddressGroup =>
                        walletAddressGroup.Skip(1).Aggregate(walletAddressGroup.First(), (result, walletAddress) =>
                        {
                            result.Balance += walletAddress.Balance;
                            result.TokenBalance.ParsedBalance = result.TokenBalance.GetTokenBalance() +
                                                                walletAddress.TokenBalance.GetTokenBalance();

                            return result;
                        }))
                    .Select(walletAddress => new TezosTokenViewModel
                    {
                        TezosConfig = tezosConfig,
                        TokenBalance = walletAddress.TokenBalance,
                        Address = walletAddress.Address,
                        Contract = contract,
                    });

                tokens.AddRange(tokensViewModels);
            }

            return tokens
                .Where(token => !token.TokenBalance.IsNft);
        }


        private ReactiveCommand<TezosTokenViewModel, Unit> _setTokenCommand;

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
                if (!Currencies.IsTezosToken(args.Currency)) return;

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
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var tokens = await LoadTokens();

            Tokens = new ObservableCollection<TezosTokenViewModel>(tokens
                .Select(token =>
                {
                    token.AtomexApp = _app;
                    token.SetConversionTab = SetConversionTab;

                    var quote = quotesProvider.GetQuote(token.TokenBalance.Symbol,
                        TezosTokenViewModel.BaseCurrencyCode);

                    if (quote == null) return token;

                    token.CurrentQuote = quote.Bid;
                    token.AvailableAmountInBase = token.TokenBalance.GetTokenBalance().SafeMultiply(quote.Bid);

                    return token;
                })
                .OrderByDescending(token => token.CanExchange)
                .ThenByDescending(token => token.AvailableAmountInBase));
        }

        public TezosTokensViewModel()
        {
#if DEBUG
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