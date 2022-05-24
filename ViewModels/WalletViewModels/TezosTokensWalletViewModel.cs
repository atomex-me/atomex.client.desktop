using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Serilog;

using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Tzkt;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.Services;
using Atomex.TezosTokens;
using Atomex.ViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokenViewModel : ViewModelBase, IExpandable
    {
        private bool _isPreviewDownloading = false;
        public TezosConfig_OLD TezosConfig { get; set; }
        public TokenBalance TokenBalance { get; set; }
        public string Address { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        public IBitmap TokenPreview
        {
            get
            {
                if (_isPreviewDownloading)
                    return null;

                var thumbsApiSettings = new ThumbsApiSettings
                {
                    ThumbsApiUri   = TezosConfig.ThumbsApiUri,
                    IpfsGatewayUri = TezosConfig.IpfsGatewayUri,
                    CatavaApiUri   = TezosConfig.CatavaApiUri
                };

                var thumbsApi = new ThumbsApi(thumbsApiSettings);

                foreach (var url in thumbsApi.GetTokenPreviewUrls(TokenBalance.Contract, TokenBalance.ThumbnailUri, TokenBalance.DisplayUri))
                {
                    if (!App.ImageService.GetImageLoaded(url))
                    {
                        // start async download
                        _ = Task.Run(async () =>
                        {
                            _isPreviewDownloading = true;

                            await App.ImageService.LoadImageFromUrl(url, async () =>
                            {
                                _isPreviewDownloading = false;

                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    OnPropertyChanged(nameof(TokenPreview));
                                });
                            })
                            .ConfigureAwait(false);
                        });

                        return null;
                    }

                    return App.ImageService.GetImage(url);
                }

                return null;
            }
        }

        public string Balance => TokenBalance.Balance != "1"
            ? $"{TokenBalance.GetTokenBalance().ToString($"F{Math.Min(TokenBalance.Decimals, AddressesHelper.MaxTokenCurrencyFormatDecimals)}", CultureInfo.InvariantCulture)}  {TokenBalance.Symbol}"
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

        public bool IsIpfsAsset => TokenBalance.ArtifactUri != null && ThumbsApi.HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string? AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{ThumbsApi.RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;

        public Action<TezosTokenViewModel> SendCallback;

        private ICommand _send;
        public ICommand Send => _send ??= ReactiveCommand.Create(() => { SendCallback?.Invoke(this); });

        public string AddressExplorerUri => TezosConfig != null
            ? $"{TezosConfig.AddressExplorerUri}{Address}"
            : "";

        private ICommand _openAddressInExplorerCommand;
        public ICommand OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??=
            ReactiveCommand.Create<string>((address) =>
            {
                if (TezosConfig == null)
                    return;

                if (Uri.TryCreate($"{TezosConfig.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                    Process.Start(uri.ToString());
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

    public class TezosTokenContractViewModel : ViewModelBase
    {
        public TokenContract Contract { get; set; }
        public string IconUrl => $"https://services.tzkt.io/v1/avatars/{Contract.Address}";
        public Action PreviewLoadedCallback { get; set; }
        private bool _isPreviewDownloading;

        public IBitmap IconPreview
        {
            get
            {
                if (_isPreviewDownloading)
                    return null;

                if (!App.ImageService.GetImageLoaded(IconUrl))
                {
                    // start async download
                    _ = Task.Run(async () =>
                    {
                        _isPreviewDownloading = true;

                        await App.ImageService.LoadImageFromUrl(IconUrl, async () =>
                        {
                            _isPreviewDownloading = false;

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                OnPropertyChanged(nameof(IconPreview));
                            });
                        })
                        .ConfigureAwait(false);
                    });

                    return null;
                }

                return App.ImageService.GetImage(IconUrl);
            }
        }

        public bool IsFa12 => Contract.GetContractType() == "FA12";
        public bool IsFa2 => Contract.GetContractType() == "FA2";

        private bool _isTriedToGetFromTzkt;

        private string _name;
        public string Name
        {
            get
            {
                if (_name != null)
                    return _name;

                if (!_isTriedToGetFromTzkt)
                {
                    _isTriedToGetFromTzkt = true;
                    _ = TryGetAliasAsync();
                }

                _name = Contract.Name;
                return _name;
            }
        }

        public bool HasName => !string.IsNullOrEmpty(_name);

        private async Task TryGetAliasAsync()
        {
            try
            {
                var response = await HttpHelper.HttpClient
                    .GetAsync($"https://api.tzkt.io/v1/accounts/{Contract.Address}")
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return;

                var stringResponse = await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);

                var alias = JsonConvert.DeserializeObject<JObject>(stringResponse)
                    ?["alias"]
                    ?.Value<string>();

                if (alias != null)
                    _name = alias;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(HasName));
                    PreviewLoadedCallback?.Invoke();
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Alias getting error.");
            }
        }
    }

    public class TezosTokensWalletViewModel : WalletViewModel, IWalletViewModel
    {
        private const int MaxAmountDecimals = AddressesHelper.MaxTokenCurrencyFormatDecimals;
        private const string Fa12 = "FA12";

        public ObservableCollection<TezosTokenContractViewModel> TokensContracts { get; set; }
        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public ObservableCollection<TezosTokenTransferViewModel> Transfers { get; set; }

        private TezosTokenContractViewModel? _tokenContract;
        public TezosTokenContractViewModel? TokenContract
        {
            get => _tokenContract;
            set
            {
                _tokenContract = value;
                OnPropertyChanged(nameof(TokenContract));
                OnPropertyChanged(nameof(HasTokenContract));
                OnPropertyChanged(nameof(IsFa12));
                OnPropertyChanged(nameof(IsFa2));
                OnPropertyChanged(nameof(TokenContractAddress));
                OnPropertyChanged(nameof(TokenContractName));
                OnPropertyChanged(nameof(TokenContractIconUrl));
                OnPropertyChanged(nameof(IsConvertable));

                TokenContractChanged(TokenContract);
            }
        }

        public bool HasTokenContract => TokenContract != null;
        public bool IsFa12 => TokenContract?.IsFa12 ?? false;
        public bool IsFa2 => TokenContract?.IsFa2 ?? false;
        public string TokenContractAddress => TokenContract?.Contract?.Address ?? "";
        public string TokenContractName => TokenContract?.Name ?? "";
        public string? TokenContractIconUrl => TokenContract?.IconUrl;

        private bool _isPreviewDownloading;

        public IBitmap? TokenContractIconPreview
        {
            get
            {
                if (_isPreviewDownloading)
                    return null;

                if (TokenContractIconUrl == null)
                    return null;

                if (!App.ImageService.GetImageLoaded(TokenContractIconUrl))
                {
                    // start async download
                    _ = Task.Run(async () =>
                    {
                        _isPreviewDownloading = true;

                        await App.ImageService.LoadImageFromUrl(TokenContractIconUrl, async () =>
                        {
                            _isPreviewDownloading = false;

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                OnPropertyChanged(nameof(TokenContractIconPreview));
                            });
                        })
                        .ConfigureAwait(false);
                    });

                    return null;
                }

                return App.ImageService.GetImage(TokenContractIconUrl);
            }
        }

        public bool IsConvertable => _app.Account.Currencies
            .Any(c => c is Fa12Config_OLD fa12 && fa12.TokenContractAddress == TokenContractAddress);

        public string Header => "Tezos Tokens";
        public decimal Balance { get; set; }
        public string BalanceFormat { get; set; }
        public string BalanceCurrencyCode { get; set; }

        public IBrush Background => IsSelected
            ? TezosCurrencyViewModel.DefaultIconBrush
            : TezosCurrencyViewModel.DefaultUnselectedIconBrush;

        public IBrush OpacityMask => IsSelected
            ? null
            : TezosCurrencyViewModel.DefaultIconMaskBrush;

        public int SelectedTabIndex { get; set; }

        public TezosTokensWalletViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TezosTokensWalletViewModel(
            IAtomexApp app,
            Action<CurrencyConfig_OLD> setConversionTab) : base(app, setConversionTab, null)
        {
            _ = ReloadTokenContractsAsync();
        }

        protected override void SubscribeToServices()
        {
            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        protected override Task LoadTransactionsAsync()
        {
            return null!;
        }

        private void OnAtomexClientChanged(object sender, AtomexClientChangedEventArgs e)
        {
            Tokens?.Clear();
            Transfers?.Clear();
            TokensContracts?.Clear();
            TokenContract = null;
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currencies.IsTezosToken(args.Currency))
                {
                    await Dispatcher.UIThread.InvokeAsync(async () => { await ReloadTokenContractsAsync(); },
                        DispatcherPriority.Background);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async Task ReloadTokenContractsAsync()
        {
            var tokensContractsViewModels = (await _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig_OLD.Xtz)
                .DataRepository
                .GetTezosTokenContractsAsync())
                .Select(c => new TezosTokenContractViewModel
                {
                    Contract = c,
                    PreviewLoadedCallback = () => OnPropertyChanged(nameof(TokenContractName))
                });

            if (TokensContracts != null)
            {
                // add new token contracts if exists
                var newTokenContracts = tokensContractsViewModels.Except(
                    second: TokensContracts,
                    comparer: new Atomex.Common.EqualityComparer<TezosTokenContractViewModel>(
                        (x, y) => x.Contract.Address.Equals(y.Contract.Address),
                        x => x.Contract.Address.GetHashCode()));

                if (newTokenContracts.Any())
                {
                    foreach (var newTokenContract in newTokenContracts)
                        TokensContracts.Add(newTokenContract);

                    if (TokenContract == null)
                        TokenContract = TokensContracts.FirstOrDefault();
                }
                else
                {
                    // update current token contract
                    if (TokenContract != null)
                        TokenContractChanged(TokenContract);
                }
            }
            else
            {
                TokensContracts = new ObservableCollection<TezosTokenContractViewModel>(tokensContractsViewModels);
                OnPropertyChanged(nameof(TokensContracts));

                TokenContract = TokensContracts.FirstOrDefault();
            }
        }

        private async void TokenContractChanged(TezosTokenContractViewModel? tokenContract)
        {
            if (tokenContract == null)
            {
                Tokens = new ObservableCollection<TezosTokenViewModel>();
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>();

                OnPropertyChanged(nameof(Tokens));
                OnPropertyChanged(nameof(Transfers));

                return;
            }

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig_OLD>(TezosConfig_OLD.Xtz);

            if (tokenContract.IsFa12)
            {
                var tokenAccount = _app.Account.GetTezosTokenAccount<Fa12Account_OLD>(
                    currency: Fa12,
                    tokenContract: tokenContract.Contract.Address,
                    tokenId: 0);

                var tokenAddresses = await tokenAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(tokenContract.Contract.Address);

                var tokenAddress = tokenAddresses.FirstOrDefault();

                var tezosTokenConfig = _app.Account.Currencies
                    .FirstOrDefault(c => Currencies.IsTezosToken(c.Name) &&
                                         c is Fa12Config_OLD fa12Config &&
                                         fa12Config.TokenContractAddress == tokenContract.Contract.Address);

                Balance = tokenAccount
                    .GetBalance()
                    .Available;

                BalanceFormat = tokenAddress?.TokenBalance != null && tokenAddress.TokenBalance.Decimals != 0
                    ? $"F{Math.Min(tokenAddress.TokenBalance.Decimals, MaxAmountDecimals)}"
                    : $"F{MaxAmountDecimals}";

                BalanceCurrencyCode = tokenAddress?.TokenBalance != null && tokenAddress.TokenBalance.Symbol != null
                    ? tokenAddress.TokenBalance.Symbol
                    : tezosTokenConfig?.Name ?? "";

                OnPropertyChanged(nameof(Balance));
                OnPropertyChanged(nameof(BalanceFormat));
                OnPropertyChanged(nameof(BalanceCurrencyCode));

                Transfers = new ObservableCollection<TezosTokenTransferViewModel>((await tokenAccount
                    .DataRepository
                    .GetTezosTokenTransfersAsync(tokenContract.Contract.Address, offset: 0, limit: int.MaxValue))
                    .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                    .ToList()
                    .SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));

                Tokens = new ObservableCollection<TezosTokenViewModel>();
            }
            else if (tokenContract.IsFa2)
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig_OLD.Xtz);

                var tokenAddresses = await tezosAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(tokenContract.Contract.Address);

                Transfers = new ObservableCollection<TezosTokenTransferViewModel>((await tezosAccount
                    .DataRepository
                    .GetTezosTokenTransfersAsync(tokenContract.Contract.Address, offset: 0, limit: int.MaxValue))
                    .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                    .ToList()
                    .SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));

                Tokens = new ObservableCollection<TezosTokenViewModel>(tokenAddresses
                    .Where(a => a.Balance != 0)
                    .Select(a => new TezosTokenViewModel
                    {
                        TezosConfig  = tezosConfig,
                        TokenBalance = a.TokenBalance,
                        Address      = a.Address,
                        SendCallback = SendCallback
                    }));
            }

            OnPropertyChanged(nameof(Tokens));
            OnPropertyChanged(nameof(Transfers));

            SelectedTabIndex = tokenContract.IsFa2 ? 0 : 1;
            OnPropertyChanged(nameof(SelectedTabIndex));

            OnPropertyChanged(nameof(TokenContractIconPreview));

            _sortInfo = null;
            OnPropertyChanged(nameof(SortInfo));
        }

        protected override void OnSendClick()
        {
            if (TokenContract?.Contract?.Address == null)
                return;

            var sendViewModel = new TezosTokensSendViewModel(
                app: _app,
                tokenContract: TokenContract.Contract.Address,
                tokenId: 0,
                tokenType: TokenContract.Contract.GetContractType(),
                getTokenPreview: GetTokenPreview,
                balanceFormat: BalanceFormat,
                from: null);

            App.DialogService.Show(sendViewModel.SelectFromViewModel);
        }

        private void SendCallback(TezosTokenViewModel tokenViewModel)
        {
            if (tokenViewModel?.TokenBalance == null)
                return;
            
            var sendViewModel = new TezosTokensSendViewModel(
                app: _app,
                tokenContract: tokenViewModel.TokenBalance.Contract,
                tokenId: tokenViewModel.TokenBalance.TokenId,
                tokenType: TokenContract.Contract.GetContractType(),
                getTokenPreview: GetTokenPreview,
                from: tokenViewModel.Address);

            App.DialogService.Show(sendViewModel.SelectToViewModel);
        }

        private IBitmap GetTokenPreview(string address, decimal tokenId)
        {
            return Tokens
                .FirstOrDefault(tokenViewModel => tokenViewModel.Address == address && tokenViewModel.TokenBalance.TokenId == tokenId)
                ?.TokenPreview ?? TokenContract.IconPreview;
        }

        protected override void OnReceiveClick()
        {
            var tezosConfig = _app.Account
                .Currencies
                .GetByName(TezosConfig_OLD.Xtz);

            var receiveViewModel = new ReceiveViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: TokenContract?.Contract?.Address,
                tokenType: TokenContract?.Contract?.GetContractType());

            App.DialogService.Show(receiveViewModel);
        }

        protected override void OnConvertClick()
        {
            var currency = _app.Account
                .Currencies
                .FirstOrDefault(c => c is Fa12Config_OLD fa12 && fa12.TokenContractAddress == TokenContractAddress);

            SetConversionTab?.Invoke(currency);
        }

        protected override async void OnUpdateClick()
        {
            if (IsBalanceUpdating)
                return;
 
            IsBalanceUpdating = true;

            _cancellation = new CancellationTokenSource();
                
            var updatingModalVm = MessageViewModel.Message(
                title: "Updating",
                text: "Tezos tokens balance updating, please wait...",
                nextAction:() => _cancellation.Cancel(),
                buttonTitle: "Cancel",
                withProgressBar: true
                );
            
            App.DialogService.Show(updatingModalVm);

            try
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig_OLD.Xtz);

                var tezosTokensScanner = new TezosTokensScanner_OLD(tezosAccount);

                await tezosTokensScanner.ScanAsync(
                    skipUsed: false,
                    cancellationToken: _cancellation.Token);

                // reload balances for all tezos tokens account
                foreach (var currency in _app.Account.Currencies)
                    if (Currencies.IsTezosToken(currency.Name))
                        _app.Account
                            .GetCurrencyAccount<TezosTokenAccount_OLD>(currency.Name)
                            .ReloadBalances();
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "WalletViewModel.OnUpdateClick");
                // todo: message to user!?
            }

            App.DialogService.Close();
            IsBalanceUpdating = false;
        }

        protected override void OnAddressesClick()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig_OLD>(TezosConfig_OLD.Xtz);

            var addressesViewModel = new AddressesViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: TokenContract?.Contract?.Address);

            App.DialogService.Show(addressesViewModel);
        }

        protected override void SortTransactions(string columnName, SortType sortType)
        {
            DGSelectedIndex = -1;

            if (columnName.ToLower() == "time" && sortType == SortType.Asc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
                    Transfers.OrderBy(tx => tx.LocalTime));
            }

            if (columnName.ToLower() == "time" && sortType == SortType.Desc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
                    Transfers.OrderByDescending(tx => tx.LocalTime));
            }

            if (columnName.ToLower() == "amount" && sortType == SortType.Asc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
                    Transfers.OrderBy(tx => tx.Amount));
            }

            if (columnName.ToLower() == "amount" && sortType == SortType.Desc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
                    Transfers.OrderByDescending(tx => tx.Amount));
            }

            if (columnName.ToLower() == "state" && sortType == SortType.Asc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
                    Transfers.OrderBy(tx => tx.State));
            }

            if (columnName.ToLower() == "state" && sortType == SortType.Desc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
                    Transfers.OrderByDescending(tx => tx.State));
            }

            if (columnName.ToLower() == "type" && sortType == SortType.Asc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
                    Transfers.OrderBy(tx => tx.Type));
            }

            if (columnName.ToLower() == "type" && sortType == SortType.Desc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
                    Transfers.OrderByDescending(tx => tx.Type));
            }

            OnPropertyChanged(nameof(Transfers));
        }

#if DEBUG
        protected override void DesignerMode()
        {
            TokensContracts = new ObservableCollection<TezosTokenContractViewModel>
            {
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1K9gCRgaLRFKTErYt1wVxA3Frb9FjasjTV",
                        Name = "kUSD",
                        Type = "FA12"
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn",
                        Name = "tzBTC",
                        Type = "FA12"
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1G1cCRNBgQ48mVDjopHjEmTN5Sbtar8nn9",
                        Name = "Hedgehoge",
                        Type = "FA12"
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
                        Name = "hic et nunc NFTs",
                        Type = "FA2"
                    }
                }
            };

            _tokenContract = null; // TokensContracts.First();

            var tezosConfig = DesignTime.MainNetCurrencies.Get<TezosConfig_OLD>("XTZ");
            var tzktApi = new TzktApi(tezosConfig);

            var address = "tz1YS2CmS5o24bDz9XNr84DSczBXuq4oGHxr";

            var tokensBalances = tzktApi
                .GetTokenBalancesAsync(address)
                .WaitForResult();

            Tokens = new ObservableCollection<TezosTokenViewModel>(
                tokensBalances.Value.Select(tb => new TezosTokenViewModel
                {
                    TokenBalance = tb,
                    Address = address
                }));

            var transfers = tzktApi
                .GetTokenTransfersAsync(
                    address: address,
                    contractAddress: "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton")
                .WaitForResult()
                .Value;

            Transfers = new ObservableCollection<TezosTokenTransferViewModel>(transfers
                .Select(t => new TezosTokenTransferViewModel(t, tezosConfig)));
        }
#endif
    }
}