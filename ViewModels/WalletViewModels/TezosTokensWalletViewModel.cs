using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Windows.Input;
using Avalonia.Threading;
using Serilog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Controls;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.ReceiveViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Core;
using Atomex.Services;
using Atomex.TezosTokens;
using Atomex.ViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokenViewModel : ViewModelBase, IExpandable
    {
        private bool _isPreviewDownloading = false;
        public TezosConfig TezosConfig { get; set; }

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

                foreach (var url in GetTokenPreviewUrls())
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

        public bool IsIpfsAsset => TokenBalance.ArtifactUri != null && HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;


        public IEnumerable<string> GetTokenPreviewUrls()
        {
            yield return $"https://d38roug276qjor.cloudfront.net/{TokenBalance.Contract}/{TokenBalance.TokenId}.png";

            if (TokenBalance.ArtifactUri != null && HasIpfsPrefix(TokenBalance.ArtifactUri))
                yield return $"https://api.dipdup.net/thumbnail/{RemoveIpfsPrefix(TokenBalance.ArtifactUri)}";

            yield return $"https://services.tzkt.io/v1/avatars/{TokenBalance.Contract}";
        }

        public static string RemovePrefix(string s, string prefix) =>
            s.StartsWith(prefix) ? s.Substring(prefix.Length) : s;

        public static string RemoveIpfsPrefix(string url) =>
            RemovePrefix(url, "ipfs://");

        public static bool HasIpfsPrefix(string url) =>
            url?.StartsWith("ipfs://") ?? false;

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

        private TezosTokenContractViewModel _tokenContract;

        public TezosTokenContractViewModel TokenContract
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

        public string TokenContractIconUrl => TokenContract?.IconUrl;

        private bool _isPreviewDownloading;

        public IBitmap TokenContractIconPreview
        {
            get
            {
                if (_isPreviewDownloading)
                    return null;

                if (TokenContractIconUrl == null)
                    return null;

                if (!Desktop.App.ImageService.GetImageLoaded(TokenContractIconUrl))
                {
                    // start async download
                    _ = Task.Run(async () =>
                    {
                        _isPreviewDownloading = true;
                        await Desktop.App.ImageService.LoadImageFromUrl(TokenContractIconUrl, async () =>
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

                return Desktop.App.ImageService.GetImage(TokenContractIconUrl);
            }
        }

        public bool IsConvertable => App.Account.Currencies
            .Any(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContractAddress);

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
            Action<CurrencyConfig> setConversionTab) : base(app, setConversionTab, null)
        {
            _ = ReloadTokenContractsAsync();
        }

        protected override void SubscribeToServices()
        {
            App.AtomexClientChanged += OnAtomexClientChanged;
            App.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
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
            var tokensContractsViewModels = (await App.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
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

        private async void TokenContractChanged(TezosTokenContractViewModel tokenContract)
        {
            if (tokenContract == null)
            {
                Tokens = new ObservableCollection<TezosTokenViewModel>();
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>();

                OnPropertyChanged(nameof(Tokens));
                OnPropertyChanged(nameof(Transfers));

                return;
            }

            var tezosConfig = App.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (tokenContract.IsFa12)
            {
                var tokenAccount = App.Account.GetTezosTokenAccount<Fa12Account>(
                    currency: Fa12,
                    tokenContract: tokenContract.Contract.Address,
                    tokenId: 0);

                var tokenAddresses = await tokenAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(tokenContract.Contract.Address);

                var tokenAddress = tokenAddresses.FirstOrDefault();

                var tezosTokenConfig = App.Account.Currencies
                    .FirstOrDefault(c => Currencies.IsTezosToken(c.Name) &&
                                         c is Fa12Config fa12Config &&
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
                var tezosAccount = App.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

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
                        TezosConfig = tezosConfig,
                        TokenBalance = a.TokenBalance,
                        Address = a.Address,
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
                app: App,
                tokenContract: TokenContract.Contract.Address,
                tokenId: 0,
                tokenType: TokenContract.Contract.GetContractType(),
                getTokenPreview: GetTokenPreview,
                balanceFormat: BalanceFormat,
                from: null);

            Desktop.App.DialogService.Show(sendViewModel.SelectFromViewModel);
        }

        private void SendCallback(TezosTokenViewModel tokenViewModel)
        {
            if (tokenViewModel?.TokenBalance == null)
                return;
            
            var sendViewModel = new TezosTokensSendViewModel(
                app: App,
                tokenContract: tokenViewModel.TokenBalance.Contract,
                tokenId: tokenViewModel.TokenBalance.TokenId,
                tokenType: TokenContract.Contract.GetContractType(),
                getTokenPreview: GetTokenPreview,
                from: tokenViewModel.Address);

            Desktop.App.DialogService.Show(sendViewModel.SelectToViewModel);
        }

        private IBitmap GetTokenPreview(string address, decimal tokenId)
        {
            return Tokens.FirstOrDefault(tokenViewModel =>
                           tokenViewModel.Address == address && tokenViewModel.TokenBalance.TokenId == tokenId)
                       ?.TokenPreview ??
                   TokenContract.IconPreview;
        }

        protected override void OnReceiveClick()
        {
            var tezosConfig = App.Account
                .Currencies
                .GetByName(TezosConfig.Xtz);

            var receiveViewModel = new ReceiveViewModel(
                app: App,
                currency: tezosConfig,
                tokenContract: TokenContract?.Contract?.Address,
                tokenType: TokenContract?.Contract?.GetContractType());

            Desktop.App.DialogService.Show(receiveViewModel);
        }

        protected override void OnConvertClick()
        {
            var currency = App.Account.Currencies
                .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContractAddress);

            SetConversionTab?.Invoke(currency);
        }

        protected override async void OnUpdateClick()
        {
            if (IsBalanceUpdating)
                return;

            IsBalanceUpdating = true;

            _cancellation = new CancellationTokenSource();

            var updatingModalVM = new TezosTokensScanDialogViewModel();
            updatingModalVM.OnCancel = () => _cancellation.Cancel();
            Desktop.App.DialogService.Show(updatingModalVM);

            try
            {
                var tezosAccount = App.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tezosTokensScanner = new TezosTokensScanner(tezosAccount);

                await tezosTokensScanner.ScanAsync(
                    skipUsed: false,
                    cancellationToken: _cancellation.Token);

                // reload balances for all tezos tokens account
                foreach (var currency in App.Account.Currencies)
                    if (Currencies.IsTezosToken(currency.Name))
                        App.Account
                            .GetCurrencyAccount<TezosTokenAccount>(currency.Name)
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

            Desktop.App.DialogService.Close();
            IsBalanceUpdating = false;
        }

        protected override void OnAddressesClick()
        {
            var tezosConfig = App.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var addressesViewModel = new AddressesViewModel(
                app: App,
                currency: tezosConfig,
                tokenContract: TokenContract?.Contract?.Address);

            Desktop.App.DialogService.Show(addressesViewModel);
        }

        protected override void SortTransactions(string columnName, SortType sortType)
        {
            DGSelectedIndex = -1;
            if (columnName.ToLower() == "time" && sortType == SortType.Asc)
            {
                Transfers =
                    new ObservableCollection<TezosTokenTransferViewModel>(Transfers.OrderBy(tx => tx.LocalTime));
            }

            if (columnName.ToLower() == "time" && sortType == SortType.Desc)
            {
                Transfers =
                    new ObservableCollection<TezosTokenTransferViewModel>(
                        Transfers.OrderByDescending(tx => tx.LocalTime));
            }

            if (columnName.ToLower() == "amount" && sortType == SortType.Asc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(Transfers.OrderBy(tx => tx.Amount));
            }

            if (columnName.ToLower() == "amount" && sortType == SortType.Desc)
            {
                Transfers =
                    new ObservableCollection<TezosTokenTransferViewModel>(Transfers.OrderByDescending(tx => tx.Amount));
            }

            if (columnName.ToLower() == "state" && sortType == SortType.Asc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(Transfers.OrderBy(tx => tx.State));
            }

            if (columnName.ToLower() == "state" && sortType == SortType.Desc)
            {
                Transfers =
                    new ObservableCollection<TezosTokenTransferViewModel>(Transfers.OrderByDescending(tx => tx.State));
            }

            if (columnName.ToLower() == "type" && sortType == SortType.Asc)
            {
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>(Transfers.OrderBy(tx => tx.Type));
            }

            if (columnName.ToLower() == "type" && sortType == SortType.Desc)
            {
                Transfers =
                    new ObservableCollection<TezosTokenTransferViewModel>(Transfers.OrderByDescending(tx => tx.Type));
            }

            OnPropertyChanged(nameof(Transfers));
        }

        private void DesignerMode()
        {
            TokensContracts = new ObservableCollection<TezosTokenContractViewModel>
            {
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1K9gCRgaLRFKTErYt1wVxA3Frb9FjasjTV",
                        Network = "mainnet",
                        Name = "kUSD",
                        Description = "FA1.2 Implementation of kUSD",
                        Interfaces = new List<string> { "TZIP-007-2021-01-29" }
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn",
                        Network = "mainnet",
                        Name = "tzBTC",
                        Description = "Wrapped Bitcon",
                        Interfaces = new List<string> { "TZIP-7", "TZIP-16", "TZIP-20" }
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1G1cCRNBgQ48mVDjopHjEmTN5Sbtar8nn9",
                        Network = "mainnet",
                        Name = "Hedgehoge",
                        Description = "such cute, much hedge!",
                        Interfaces = new List<string> { "TZIP-007", "TZIP-016" }
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
                        Network = "mainent",
                        Name = "hic et nunc NFTs",
                        Description = "NFT token for digital assets.",
                        Interfaces = new List<string> { "TZIP-12" }
                    }
                }
            };

            _tokenContract = null; // TokensContracts.First();

            var bcdApi = new BcdApi(new BcdApiSettings
            {
                MaxSize = 10,
                MaxTokensSize = 50,
                Network = "mainnet",
                Uri = "https://api.better-call.dev/v1/"
            });

            var address = "tz1YS2CmS5o24bDz9XNr84DSczBXuq4oGHxr";

            var tokensBalances = bcdApi
                .GetTokenBalancesAsync(
                    address: address,
                    count: 36)
                .WaitForResult();

            Tokens = new ObservableCollection<TezosTokenViewModel>(
                tokensBalances.Value.Select(tb => new TezosTokenViewModel
                {
                    TokenBalance = tb,
                    Address = address
                }));

            var transfers = bcdApi
                .GetTokenTransfers(
                    address: address,
                    contract: "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton")
                .WaitForResult()
                .Value;

            var tezosConfig = DesignTime.Currencies.Get<TezosConfig>(TezosConfig.Xtz);

            Transfers = new ObservableCollection<TezosTokenTransferViewModel>(transfers
                .Select(t => new TezosTokenTransferViewModel(t, tezosConfig)));
        }
    }
}