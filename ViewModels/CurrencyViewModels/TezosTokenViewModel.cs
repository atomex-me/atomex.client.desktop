using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Tzkt;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TezosTokenViewModel : ViewModelBase, IAssetViewModel, IDisposable
    {
        private const int MaxBalanceDecimals = CurrencyConfig.MaxPrecision;

        public bool CanExchange => AtomexApp
            ?.Account
            ?.Currencies
            .Any(c => c is TezosTokenConfig tezosTokenConfig &&
                      tezosTokenConfig.TokenContractAddress == Contract?.Address &&
                      tezosTokenConfig.TokenId == TokenBalance?.TokenId) ?? false;

        public IAtomexApp AtomexApp { get; init; }
        public TezosConfig TezosConfig { get; init; }
        [Reactive] public TokenBalance TokenBalance { get; init; }
        public TokenContract Contract { get; init; }
        public static string BaseCurrencyFormat => "$0.##"; // todo: use from settings
        public static string BaseCurrencyCode => "USD"; // todo: use base currency from settings
        public Action<CurrencyConfig>? SetConversionTab { get; init; }

        public string CurrencyFormat => TokenBalance.Decimals != 0
            ? $"F{Math.Min(TokenBalance.Decimals, MaxBalanceDecimals)}"
            : $"F{MaxBalanceDecimals}";

        [Reactive] public decimal CurrentQuote { get; set; }
        [Reactive] public bool IsPopupOpened { get; set; }
        [Reactive] public decimal TotalAmount { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }
        [Reactive] public string Balance { get; set; }

        public string IconPath => string.Empty;
        public string DisabledIconPath => string.Empty;
        public string PreviewUrl => ThumbsApi.GetTokenPreviewUrl(Contract.Address, TokenBalance.TokenId);
        public string CurrencyName => TokenBalance.Symbol;
        public string CurrencyCode => TokenBalance.Symbol;
        public string CurrencyDescription => TokenBalance.Name;
        string IAssetViewModel.BaseCurrencyFormat => BaseCurrencyFormat;

        public string CollectiblePreviewUrl =>
            ThumbsApi.GetTokenPreviewUrl(Contract.Address, TokenBalance.TokenId);

        public TezosTokenViewModel()
        {
            this.WhenAnyValue(vm => vm.TotalAmount, vm => vm.TokenBalance)
                .Where(values => values.Item2 != null)
                .Select(values =>
                    values.Item1 == 0
                        ? $"0 {CurrencyCode}"
                        : $"{values.Item1.ToString(CurrencyFormat, CultureInfo.CurrentCulture)} {CurrencyCode}")
                .Subscribe(formattedAmount => Balance = formattedAmount);

            this.WhenAnyValue(vm => vm.TotalAmount)
                .WhereNotNull()
                .Where(_ => AtomexApp != null)
                .Skip(1)
                .SubscribeInMainThread(_ => UpdateQuotesInBaseCurrency(AtomexApp.QuotesProvider));

            SendCommand.Merge(ReceiveCommand)
                .SubscribeInMainThread(_ => IsPopupOpened = false);
        }

        public void SubscribeToUpdates()
        {
            AtomexApp.LocalStorage.BalanceChanged += OnBalanceChangedEventHandler;

            if (TokenBalance.IsNft)
                return;

            AtomexApp.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private async void OnBalanceChangedEventHandler(object? sender, BalanceChangedEventArgs args)
        {
            try
            {
                var isTokenUpdate = args is TokenBalanceChangedEventArgs eventArgs &&
                    eventArgs.Tokens.Contains((TokenBalance.Contract, TokenBalance.TokenId));

                if (!isTokenUpdate)
                    return;

                await UpdateAsync();

                Log.Debug("Balance updated for tezos token {Symbol}", TokenBalance.Symbol);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error updating tezos token {Symbol}", TokenBalance.Symbol);
            }
        }

        private void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        public void UpdateQuotesInBaseCurrency(IQuotesProvider quotesProvider)
        {
            var tokenQuote = quotesProvider.GetQuote(TokenBalance.Symbol, BaseCurrencyCode);
            var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);

            if (tokenQuote == null || xtzQuote == null)
                return;

            CurrentQuote = tokenQuote.Bid.SafeMultiply(xtzQuote.Bid);
            TotalAmountInBase = TotalAmount.SafeMultiply(CurrentQuote);

            Log.Debug("Quotes updated for tezos token {Symbol}", TokenBalance.Symbol);
        }

        private async Task UpdateAsync()
        {
            var tezosAccount = AtomexApp.Account.GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            var tokenWalletAddresses = await tezosAccount
                .LocalStorage
                .GetAddressesAsync(currency: Contract.Type, tokenContract: Contract.Address)
                .ConfigureAwait(false);

            var addresses = tokenWalletAddresses
                .Where(walletAddress => walletAddress.TokenBalance.TokenId == TokenBalance.TokenId)
                .ToList();

            var tokenBalance = 0m;

            addresses.ForEach(a => { tokenBalance += a.TokenBalance.ToDecimalBalance(); });

            TotalAmount = tokenBalance;
        }

        private ReceiveViewModel GetReceiveDialog()
        {
            return new ReceiveViewModel(
                app: AtomexApp,
                currency: TezosConfig,
                tokenContract: Contract.Address,
                tokenType: Contract.GetContractType());
        }

        private TezosTokensSendViewModel GetSendDialog()
        {
            return new TezosTokensSendViewModel(
                app: AtomexApp,
                tokenContract: Contract.Address,
                tokenId: (int)TokenBalance.TokenId,
                tokenType: Contract.GetContractType(),
                tokenPreviewUrl: PreviewUrl,
                from: null);
        }

        public bool IsIpfsAsset =>
            TokenBalance.ArtifactUri != null && ThumbsApi.HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string? AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{ThumbsApi.RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;

        private ReactiveCommand<Unit, Unit>? _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(
            () => App.DialogService.Show(GetSendDialog().SelectFromViewModel));

        private ReactiveCommand<Unit, Unit>? _receiveCommand;
        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(
            () => App.DialogService.Show(GetReceiveDialog()));

        private ReactiveCommand<Unit, Unit>? _exchangeCommand;
        public ReactiveCommand<Unit, Unit> ExchangeCommand => _exchangeCommand ??= ReactiveCommand.Create(() =>
        {
            var currency = AtomexApp.Account
                .Currencies
                .FirstOrDefault(c => c is TezosTokenConfig tokenConfig &&
                                     tokenConfig.TokenContractAddress == Contract.Address &&
                                     tokenConfig.TokenId == TokenBalance.TokenId);

            if (currency != null)
                SetConversionTab?.Invoke(currency);
        });

        public Action<TezosTokenViewModel> SendCallback;

        private ReactiveCommand<Unit, Unit>? _send;
        public ReactiveCommand<Unit, Unit> Send =>
            _send ??= ReactiveCommand.Create(() => { SendCallback?.Invoke(this); });

        private ReactiveCommand<Unit, Unit>? _openInBrowser;
        public ReactiveCommand<Unit, Unit> OpenInBrowser => _openInBrowser ??= ReactiveCommand.Create(() =>
        {
            var assetUrl = AssetUrl;

            if (assetUrl != null && Uri.TryCreate(assetUrl, UriKind.Absolute, out var uri))
                App.OpenBrowser(uri.ToString());
            else
                Log.Error("Invalid uri for ipfs asset");
        });

        private ReactiveCommand<Unit, Unit>? _openPopupCommand;
        private ReactiveCommand<Unit, Unit> OpenPopupCommand => _openPopupCommand ??=
            ReactiveCommand.Create(() => { IsPopupOpened = !IsPopupOpened; });

        private ReactiveCommand<string, Unit>? _openAddressInExplorerCommand;
        public ReactiveCommand<string, Unit> OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??=
            ReactiveCommand.Create<string>((address) =>
            {
                if (TezosConfig == null)
                    return;

                if (Uri.TryCreate($"{TezosConfig.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
                else
                    Log.Error("Invalid uri for address explorer");
            });

        private ReactiveCommand<string, Unit>? _copyAddressCommand;
        public ReactiveCommand<string, Unit> CopyAddressCommand => _copyAddressCommand ??=
            ReactiveCommand.Create<string>((s) =>
            {
                try
                {
                    App.Clipboard.SetTextAsync(s);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Copy to clipboard error");
                }
            });

        public void Dispose()
        {
            if (AtomexApp.LocalStorage != null)
                AtomexApp.LocalStorage.BalanceChanged -= OnBalanceChangedEventHandler;

            if (AtomexApp.QuotesProvider != null)
                AtomexApp.QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
        }
    }
}