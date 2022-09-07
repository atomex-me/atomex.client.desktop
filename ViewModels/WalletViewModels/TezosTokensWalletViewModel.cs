using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Tzkt;
using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.TezosTokens;
using Atomex.ViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
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

                if (App.ImageService.GetImageLoaded(IconUrl)) return App.ImageService.GetImage(IconUrl);

                // start async download
                _ = Task.Run(async () =>
                {
                    _isPreviewDownloading = true;

                    await App.ImageService.LoadImageFromUrl(IconUrl, async () =>
                        {
                            _isPreviewDownloading = false;

                            await Dispatcher.UIThread.InvokeAsync(() => { OnPropertyChanged(nameof(IconPreview)); });
                        })
                        .ConfigureAwait(false);
                });

                return null;
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
                Log.Error(e, "Account getting error.");
            }
        }
    }

    public class TezosTokensWalletViewModel : WalletViewModel, IWalletViewModel
    {
        private const int MaxAmountDecimals = AddressesHelper.MaxTokenCurrencyFormatDecimals;
        private const string Fa12 = "FA12";

        public ObservableCollection<TezosTokenContractViewModel> TokensContracts { get; set; }
        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        [Reactive] public TezosTokenViewModel? SelectedToken { get; set; }

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

                if (App.ImageService.GetImageLoaded(TokenContractIconUrl))
                    return App.ImageService.GetImage(TokenContractIconUrl);

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
        }

        public bool IsConvertable => _app.Account.Currencies
            .Any(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContractAddress);

        public decimal Balance { get; set; }
        public string BalanceFormat { get; set; }
        public string BalanceCurrencyCode { get; set; }

        public int SelectedTabIndex { get; set; }

        public TezosTokensWalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public TezosTokensWalletViewModel(
            IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            Action<string>? setWertCurrency,
            Action<ViewModelBase?> showRightPopupContent
        ) : base(app, setConversionTab, setWertCurrency, showRightPopupContent, null)
        {
            _ = ReloadTokenContractsAsync();
        }

        protected override void SubscribeToServices()
        {
            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        private void OnAtomexClientChanged(object sender, AtomexClientChangedEventArgs e)
        {
            Tokens?.Clear();
            Transactions?.Clear();
            TokensContracts?.Clear();
            // TokenContract = null;
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
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

        private async Task ReloadTokenContractsAsync()
        {
            var tokensContractsViewModels = (await _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
                    .LocalStorage
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
                    comparer: new EqualityComparer<TezosTokenContractViewModel>(
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
                Transactions = new ObservableCollection<TransactionViewModelBase>();

                OnPropertyChanged(nameof(Tokens));
                return;
            }

            SelectedToken = null;

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (tokenContract.IsFa12)
            {
                var tokenAccount = _app.Account.GetTezosTokenAccount<Fa12Account>(
                    currency: Fa12,
                    tokenContract: tokenContract.Contract.Address,
                    tokenId: 0);

                var tokenAddresses = await tokenAccount
                    .LocalStorage
                    .GetTezosTokenAddressesByContractAsync(tokenContract.Contract.Address);

                var tokenAddress = tokenAddresses.FirstOrDefault();

                var tezosTokenConfig = _app.Account.Currencies
                    .FirstOrDefault(c => Currencies.IsTezosToken(c.Name) &&
                                         c is Fa12Config fa12Config &&
                                         fa12Config.TokenContractAddress == tokenContract.Contract.Address);

                Balance = (await tokenAccount
                    .GetBalanceAsync()
                    .ConfigureAwait(false))
                    .Confirmed;

                BalanceFormat = tokenAddress?.TokenBalance != null && tokenAddress.TokenBalance.Decimals != 0
                    ? $"F{Math.Min(tokenAddress.TokenBalance.Decimals, MaxAmountDecimals)}"
                    : $"F{MaxAmountDecimals}";

                BalanceCurrencyCode = tokenAddress?.TokenBalance != null && tokenAddress.TokenBalance.Symbol != null
                    ? tokenAddress.TokenBalance.Symbol
                    : tezosTokenConfig?.Name ?? "";

                OnPropertyChanged(nameof(Balance));
                OnPropertyChanged(nameof(BalanceFormat));
                OnPropertyChanged(nameof(BalanceCurrencyCode));

                Transactions = new ObservableCollection<TransactionViewModelBase>((await tokenAccount
                        .LocalStorage
                        .GetTezosTokenTransfersAsync(tokenContract.Contract.Address, offset: 0, limit: int.MaxValue))
                    .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                    .ToList()
                    .SortList((t1, t2) => t2.Time.CompareTo(t1.Time))
                    .ForEachDo(t => t.OnClose = () => ShowRightPopupContent?.Invoke(null)));

                Tokens = new ObservableCollection<TezosTokenViewModel>();
            }
            else if (tokenContract.IsFa2)
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tokenAddresses = await tezosAccount
                    .LocalStorage
                    .GetTezosTokenAddressesByContractAsync(tokenContract.Contract.Address);

                Transactions = new ObservableCollection<TransactionViewModelBase>((await tezosAccount
                        .LocalStorage
                        .GetTezosTokenTransfersAsync(tokenContract.Contract.Address, offset: 0, limit: int.MaxValue))
                    .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                    .ToList()
                    .SortList((t1, t2) => t2.Time.CompareTo(t1.Time))
                    .ForEachDo(t => t.OnClose = () => ShowRightPopupContent?.Invoke(null)));

                Tokens = new ObservableCollection<TezosTokenViewModel>(tokenAddresses
                    .Where(a => a.Balance != 0)
                    .Select(a => new TezosTokenViewModel
                    {
                        TezosConfig = tezosConfig,
                        TokenBalance = a.TokenBalance,
                        SendCallback = SendCallback
                    }));
            }

            CurrentSortDirection = SortDirection.Desc;
            CurrentSortField = TxSortField.ByTime;

            OnPropertyChanged(nameof(Tokens));
            SelectedTabIndex = tokenContract.IsFa2 ? 0 : 1;
            OnPropertyChanged(nameof(SelectedTabIndex));
            OnPropertyChanged(nameof(TokenContractIconPreview));
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
                tokenPreview: null,
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
                tokenId: (int)tokenViewModel.TokenBalance.TokenId,
                tokenType: TokenContract.Contract.GetContractType(),
                tokenPreview: null,
                from: null);

            App.DialogService.Show(sendViewModel.SelectToViewModel);
        }

        protected override void OnReceiveClick()
        {
            var tezosConfig = _app.Account
                .Currencies
                .GetByName(TezosConfig.Xtz);

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
                .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContractAddress);

            SetConversionTab?.Invoke(currency);
        }

        protected override async Task OnUpdateClick()
        {
            _cancellation = new CancellationTokenSource();

            var updatingModalVm = MessageViewModel.Message(
                title: "Updating",
                text: "Tezos tokens balance updating, please wait...",
                nextAction: () => _cancellation.Cancel(),
                buttonTitle: "Cancel",
                withProgressBar: true
            );

            App.DialogService.Show(updatingModalVm);

            try
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tezosTokensScanner = new TezosTokensWalletScanner(tezosAccount);

                await tezosTokensScanner.UpdateBalanceAsync(
                    cancellationToken: _cancellation.Token);
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
        }

        protected void OnAddressesClick()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var addressesViewModel = new AddressesViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: TokenContract?.Contract?.Address);

            App.DialogService.Show(addressesViewModel);
        }

        // protected override void SortTransactions(string columnName, SortDirection sortDirection)
        // {
        //     DGSelectedIndex = -1;
        //
        //     if (columnName.ToLower() == "time" && sortDirection == SortDirection.Asc)
        //     {
        //         Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
        //             Transfers.OrderBy(tx => tx.LocalTime));
        //     }
        //
        //     if (columnName.ToLower() == "time" && sortDirection == SortDirection.Desc)
        //     {
        //         Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
        //             Transfers.OrderByDescending(tx => tx.LocalTime));
        //     }
        //
        //     if (columnName.ToLower() == "amount" && sortDirection == SortDirection.Asc)
        //     {
        //         Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
        //             Transfers.OrderBy(tx => tx.Amount));
        //     }
        //
        //     if (columnName.ToLower() == "amount" && sortDirection == SortDirection.Desc)
        //     {
        //         Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
        //             Transfers.OrderByDescending(tx => tx.Amount));
        //     }
        //
        //     if (columnName.ToLower() == "state" && sortDirection == SortDirection.Asc)
        //     {
        //         Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
        //             Transfers.OrderBy(tx => tx.State));
        //     }
        //
        //     if (columnName.ToLower() == "state" && sortDirection == SortDirection.Desc)
        //     {
        //         Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
        //             Transfers.OrderByDescending(tx => tx.State));
        //     }
        //
        //     if (columnName.ToLower() == "type" && sortDirection == SortDirection.Asc)
        //     {
        //         Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
        //             Transfers.OrderBy(tx => tx.Type));
        //     }
        //
        //     if (columnName.ToLower() == "type" && sortDirection == SortDirection.Desc)
        //     {
        //         Transfers = new ObservableCollection<TezosTokenTransferViewModel>(
        //             Transfers.OrderByDescending(tx => tx.Type));
        //     }
        //
        //     OnPropertyChanged(nameof(Transfers));
        // }

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

            _tokenContract = TokensContracts.First();

            var tezosConfig = DesignTime.MainNetCurrencies.Get<TezosConfig>("XTZ");
            var tzktApi = new TzktApi(tezosConfig);

            var address = "tz1YS2CmS5o24bDz9XNr84DSczBXuq4oGHxr";

            var tokensBalances = tzktApi
                .GetTokenBalanceAsync(addresses: new [] { address })
                .WaitForResult();

            Tokens = new ObservableCollection<TezosTokenViewModel>(
                tokensBalances.Value.Select(tb => new TezosTokenViewModel
                {
                    TokenBalance = tb,
                }));

            var transfers = tzktApi
                .GetTokenTransfersAsync(
                    addresses: new[] { address },
                    tokenContracts: new[] { "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton" })
                .WaitForResult()
                .Value;

            Transactions = new ObservableCollection<TransactionViewModelBase>(transfers
                .Select(t => new TezosTokenTransferViewModel(t, tezosConfig)));
        }
#endif
    }
}