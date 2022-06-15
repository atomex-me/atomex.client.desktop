using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.ViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TezosTokenViewModel : ViewModelBase, IAssetViewModel, IDisposable
    {
        private const int MaxBalanceDecimals = AddressesHelper.MaxTokenCurrencyFormatDecimals;
        private static readonly string[] ConvertibleTokens = { "tzbtc", "kusd" };
        public const string Fa12 = "FA12";
        public const string Fa2 = "FA2";
        public bool CanExchange => ConvertibleTokens.Contains(TokenBalance.Symbol?.ToLower());
        public IAtomexApp AtomexApp { get; set; }
        public TezosConfig TezosConfig { get; set; }
        public TokenBalance TokenBalance { get; set; }
        public TokenContract Contract { get; set; }
        public string Address { get; set; }
        public static string BaseCurrencyFormat => "$0.##"; // todo: use from settings
        public static string BaseCurrencyCode => "USD"; // todo: use base currency from settings
        public bool IsFa12 => Contract.GetContractType() == Fa12;
        public bool IsFa2 => Contract.GetContractType() == Fa2;
        public Action<CurrencyConfig> SetConversionTab { get; set; }

        public string CurrencyFormat => TokenBalance.Decimals != 0
            ? $"F{Math.Min(TokenBalance.Decimals, MaxBalanceDecimals)}"
            : $"F{MaxBalanceDecimals}";

        [Reactive] public decimal CurrentQuote { get; set; }
        [Reactive] public bool IsPopupOpened { get; set; }
        [Reactive] public decimal TotalAmount { get; set; }
        [Reactive] public decimal TotalAmountInBase { get; set; }
        [ObservableAsProperty] public string Balance { get; }

        public string IconPath => string.Empty;
        public string DisabledIconPath => string.Empty;
        public string CurrencyCode => TokenBalance.Symbol;
        public string CurrencyDescription => TokenBalance.Name;
        string IAssetViewModel.BaseCurrencyFormat => BaseCurrencyFormat;

        public decimal? DailyChangePercent => null;

        private ThumbsApi ThumbsApi => new ThumbsApi(
            new ThumbsApiSettings
            {
                ThumbsApiUri = TezosConfig.ThumbsApiUri,
                IpfsGatewayUri = TezosConfig.IpfsGatewayUri,
                CatavaApiUri = TezosConfig.CatavaApiUri
            });

        private bool _isPreviewDownloading;

        public IBitmap? BitmapIcon
        {
            get
            {
                if (_isPreviewDownloading)
                    return null;

                foreach (var url in ThumbsApi.GetTokenPreviewUrls(TokenBalance.Contract, TokenBalance.ThumbnailUri,
                             TokenBalance.DisplayUri ?? TokenBalance.ArtifactUri))
                {
                    if (App.ImageService.GetImageLoaded(url)) return App.ImageService.GetImage(url);

                    // start async download
                    _ = Task.Run(async () =>
                    {
                        _isPreviewDownloading = true;

                        _ = App.ImageService.LoadImageFromUrl(url, async () =>
                        {
                            _isPreviewDownloading = false;
                            await Dispatcher.UIThread.InvokeAsync(() => { OnPropertyChanged(nameof(BitmapIcon)); });
                        });
                    });

                    return null;
                }

                return null;
            }
        }

        // public decimal DecimalBalance => TokenBalance.GetTokenBalance();

        public TezosTokenViewModel()
        {
            this.WhenAnyValue(vm => vm.TotalAmount)
                .WhereNotNull()
                .Select(totalAmount => $"{totalAmount.ToString(CurrencyFormat, CultureInfo.CurrentCulture)} {CurrencyCode}")
                .ToPropertyExInMainThread(this, vm => vm.Balance);

            SendCommand.Merge(ReceiveCommand)
                .SubscribeInMainThread(_ => IsPopupOpened = false);
        }

        public void SubscribeToUpdates()
        {
            AtomexApp.Account.BalanceUpdated += OnBalanceChangedEventHandler;
            AtomexApp.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private async void OnBalanceChangedEventHandler(object? sender, CurrencyEventArgs args)
        {
            try
            {
                //if (Currency.Name.Equals(args.Currency))
                await UpdateAsync();

                Log.Fatal($"UPDATING BALANCE FOR {TokenBalance.Symbol}");
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error for currency {args.Currency}");
            }
        }

        private void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            UpdateQuotesInBaseCurrency(quotesProvider);
        }

        public void UpdateQuotesInBaseCurrency(ICurrencyQuotesProvider quotesProvider)
        {
            var quote = quotesProvider.GetQuote(TokenBalance.Symbol, BaseCurrencyCode);
            if (quote == null) return;

            CurrentQuote = quote.Bid;
            TotalAmountInBase = TokenBalance.GetTokenBalance().SafeMultiply(quote.Bid);

            Log.Fatal($"UPDATING QUOTES FOR {TokenBalance.Symbol}");
        }

        public async Task UpdateAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var tezosAccount = AtomexApp.Account.GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tokenWalletAddresses = await tezosAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(Contract.Address);

                var addresses = tokenWalletAddresses
                    .Where(walletAddress => walletAddress.TokenBalance.TokenId == TokenBalance.TokenId)
                    .ToList();

                var tokenBalance = 0m;
                addresses.ForEach(a => { tokenBalance += a.TokenBalance.GetTokenBalance(); });
                
                TotalAmount = tokenBalance;

                UpdateQuotesInBaseCurrency(AtomexApp.QuotesProvider);
            }, DispatcherPriority.Background);
        }

        private ReceiveViewModel GetReceiveDialog()
        {
            var tezosConfig = AtomexApp.Account
                .Currencies
                .GetByName(TezosConfig.Xtz);

            return new ReceiveViewModel(
                app: AtomexApp,
                currency: tezosConfig,
                tokenContract: Contract.Address,
                tokenType: Contract.GetContractType());
        }

        private TezosTokensSendViewModel GetSendDialog()
        {
            return new TezosTokensSendViewModel(
                app: AtomexApp,
                tokenContract: Contract.Address,
                tokenId: TokenBalance.TokenId,
                tokenType: Contract.GetContractType(),
                tokenPreview: BitmapIcon,
                from: null);
        }


        public bool IsIpfsAsset =>
            TokenBalance.ArtifactUri != null && ThumbsApi.HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string? AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{ThumbsApi.RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;

        private ReactiveCommand<Unit, Unit> _sendCommand;

        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.Create(
            () => App.DialogService.Show(GetSendDialog().SelectFromViewModel));

        private ReactiveCommand<Unit, Unit> _receiveCommand;

        public ReactiveCommand<Unit, Unit> ReceiveCommand => _receiveCommand ??= ReactiveCommand.Create(
            () => App.DialogService.Show(GetReceiveDialog()));

        private ReactiveCommand<Unit, Unit> _exchangeCommand;

        public ReactiveCommand<Unit, Unit> ExchangeCommand => _exchangeCommand ??= ReactiveCommand.Create(
            () =>
            {
                var currency = AtomexApp.Account
                    .Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == Contract.Address);

                if (currency != null)
                    SetConversionTab?.Invoke(currency);
            });

        public Action<TezosTokenViewModel> SendCallback;

        private ReactiveCommand<Unit, Unit> _send;

        public ReactiveCommand<Unit, Unit> Send =>
            _send ??= ReactiveCommand.Create(() => { SendCallback?.Invoke(this); });

        private ReactiveCommand<Unit, Unit> _openInBrowser;

        public ReactiveCommand<Unit, Unit> OpenInBrowser => _openInBrowser ??= ReactiveCommand.Create(() =>
        {
            var assetUrl = AssetUrl;

            if (assetUrl != null && Uri.TryCreate(assetUrl, UriKind.Absolute, out var uri))
                App.OpenBrowser(uri.ToString());
            else
                Log.Error("Invalid uri for ipfs asset");
        });

        private ReactiveCommand<Unit, Unit> _openPopupCommand;

        private ReactiveCommand<Unit, Unit> OpenPopupCommand => _openPopupCommand ??=
            ReactiveCommand.Create(() => { IsPopupOpened = !IsPopupOpened; });

        private ReactiveCommand<string, Unit> _openAddressInExplorerCommand;

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

        private ReactiveCommand<string, Unit> _copyAddressCommand;

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
            AtomexApp.Account.BalanceUpdated -= OnBalanceChangedEventHandler;
            AtomexApp.QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
        }
    }
}