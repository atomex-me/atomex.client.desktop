using System;
using System.Globalization;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Blockchain.Tezos;
using Atomex.ViewModels;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TezosTokenViewModel : ViewModelBase
    {
        private bool _isPreviewDownloading;
        public TezosConfig TezosConfig { get; set; }
        public TokenBalance TokenBalance { get; set; }
        public TokenContract Contract { get; set; }
        public string Address { get; set; }
        public static string BaseCurrencyFormat => "$0.##"; // todo: use from settings
        public static string BaseCurrencyCode => "USD"; // todo: use base currency from settings
        [Reactive] public decimal BalanceInBase { get; set; }
        [Reactive] public decimal CurrentQuote { get; set; }
        [Reactive] public bool IsPopupOpened { get; set; }

        private ThumbsApi ThumbsApi => new ThumbsApi(
            new ThumbsApiSettings
            {
                ThumbsApiUri = TezosConfig.ThumbsApiUri,
                IpfsGatewayUri = TezosConfig.IpfsGatewayUri,
                CatavaApiUri = TezosConfig.CatavaApiUri
            });

        public IBitmap? TokenPreview
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
                            await Dispatcher.UIThread.InvokeAsync(() => { OnPropertyChanged(nameof(TokenPreview)); });
                        });
                    });

                    return null;
                }

                return null;
            }
        }

        public decimal DecimalBalance => TokenBalance.GetTokenBalance();

        public string Balance => TokenBalance.Balance != "1"
            ? $"{DecimalBalance.ToString($"F{Math.Min(TokenBalance.Decimals, AddressesHelper.MaxTokenCurrencyFormatDecimals)}", CultureInfo.CurrentCulture)}  {TokenBalance.Symbol}"
            : "";

        private ICommand _openInBrowser;

        public ICommand OpenInBrowser => _openInBrowser ??= ReactiveCommand.Create(() =>
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

        public bool IsIpfsAsset =>
            TokenBalance.ArtifactUri != null && ThumbsApi.HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string? AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{ThumbsApi.RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;

        public Action<TezosTokenViewModel> SendCallback;

        private ICommand _send;
        public ICommand Send => _send ??= ReactiveCommand.Create(() => { SendCallback?.Invoke(this); });

        // public string AddressExplorerUri => TezosConfig != null
        //     ? $"{TezosConfig.AddressExplorerUri}{Address}"
        //     : "";

        private ICommand _openAddressInExplorerCommand;

        public ICommand OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??=
            ReactiveCommand.Create<string>((address) =>
            {
                if (TezosConfig == null)
                    return;

                if (Uri.TryCreate($"{TezosConfig.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
                else
                    Log.Error("Invalid uri for address explorer");
            });

        private ICommand _copyAddressCommand;

        public ICommand CopyAddressCommand => _copyAddressCommand ??= ReactiveCommand.Create<string>((s) =>
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
    }
}