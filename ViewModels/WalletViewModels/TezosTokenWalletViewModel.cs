using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;


namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokenWalletViewModel : WalletViewModel
    {
        [Reactive] public TezosTokenViewModel? TokenViewModel { get; set; }

        public TezosTokenWalletViewModel(
            IAtomexApp app,
            Action<ViewModelBase?> showRightPopupContent) :
            base(app: app,
                showRightPopupContent: showRightPopupContent,
                setConversionTab: null,
                setWertCurrency: null,
                currency: null)
        {
            this.WhenAnyValue(vm => vm.TokenViewModel)
                .WhereNotNull()
                .Select(tokenViewModel => tokenViewModel.TokenBalance.Name)
                .SubscribeInMainThread(header => Header = header);

            this.WhenAnyValue(vm => vm.TokenViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(tokenViewModel =>
                {
                    LoadAddresses();
                    _ = LoadTransfers(tokenViewModel);
                });
        }

        private async Task LoadTransfers(TezosTokenViewModel tokenViewModel)
        {
            await LoadTransactionsSemaphore.WaitAsync();

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                IsTransactionsLoading = true;

                if (tokenViewModel.IsFa12)
                {
                    var tokenAccount = _app.Account.GetTezosTokenAccount<Fa12Account>(
                        currency: TezosTokenViewModel.Fa12,
                        tokenContract: tokenViewModel.Contract.Address,
                        tokenId: 0);

                    var selectedTransactionId = SelectedTransaction?.Id;

                    Transactions = SortTransactions(
                        new ObservableCollection<TransactionViewModelBase>((await tokenAccount
                                .DataRepository
                                .GetTezosTokenTransfersAsync(tokenViewModel.Contract.Address,
                                    offset: 0,
                                    limit: int.MaxValue))
                            .Where(token => token.Token.TokenId == tokenViewModel.TokenBalance.TokenId)
                            .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                            .ToList()
                            .ForEachDo(t => t.OnClose = () => ShowRightPopupContent?.Invoke(null))));

                    if (selectedTransactionId != null)
                        SelectedTransaction = Transactions.FirstOrDefault(t => t.Id == selectedTransactionId);
                }
                else if (tokenViewModel.IsFa2)
                {
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    var selectedTransactionId = SelectedTransaction?.Id;

                    Transactions = SortTransactions(
                        new ObservableCollection<TransactionViewModelBase>((await tezosAccount
                                .DataRepository
                                .GetTezosTokenTransfersAsync(tokenViewModel.Contract.Address,
                                    offset: 0,
                                    limit: int.MaxValue))
                            .Where(token => token.Token.TokenId == tokenViewModel.TokenBalance.TokenId)
                            .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                            .ToList()
                            .ForEachDo(t => t.OnClose = () => ShowRightPopupContent?.Invoke(null))));

                    if (selectedTransactionId != null)
                        SelectedTransaction = Transactions.FirstOrDefault(t => t.Id == selectedTransactionId);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransfers for {Contract} canceled", tokenViewModel.Contract.Address);
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransfers error for contract {Contract}", tokenViewModel.Contract.Address);
            }
            finally
            {
                LoadTransactionsSemaphore.Release();
                Log.Debug("Token transfers loaded for contract {Contract}", tokenViewModel.Contract.Address);
            }
        }

        protected override void SubscribeToServices()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (!Currencies.IsTezosBased(args.Currency) || TokenViewModel == null) return;

                await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await LoadTransfers(TokenViewModel);
                        var tezosAccount = _app.Account
                            .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                        var tokenWalletAddresses = (await tezosAccount
                                .DataRepository
                                .GetTezosTokenAddressesByContractAsync(TokenViewModel.Contract.Address))
                            .Where(address => address.TokenBalance.TokenId == TokenViewModel.TokenBalance.TokenId);

                        var tokenBalance = tokenWalletAddresses.Sum(address => address.TokenBalance.GetTokenBalance());

                        TokenViewModel.TotalAmount = tokenBalance;

                        // todo: quotes update event
                        var quote = _app.QuotesProvider.GetQuote(TokenViewModel.TokenBalance.Symbol);
                        if (quote != null)
                            TokenViewModel.TotalAmountInBase = TokenViewModel.TotalAmount.SafeMultiply(quote.Bid);
                    },
                    DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        protected override void OnReceiveClick()
        {
            TokenViewModel?.ReceiveCommand.Execute().Subscribe();
        }

        protected override void OnSendClick()
        {
            TokenViewModel?.SendCommand.Execute().Subscribe();
        }

        protected override void OnConvertClick()
        {
            TokenViewModel?.ExchangeCommand.Execute().Subscribe();
        }

        protected override void LoadAddresses()
        {
            if (TokenViewModel == null) return;

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            AddressesViewModel?.Dispose();

            AddressesViewModel = new AddressesViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: TokenViewModel.Contract.Address,
                tokenId: TokenViewModel.TokenBalance.TokenId);
        }

        protected override async Task OnUpdateClick()
        {
            _cancellation = new CancellationTokenSource();

            try
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tezosTokensScanner = new TezosTokensScanner(tezosAccount);

                await tezosTokensScanner.ScanContractAsync(
                    contractAddress: TokenViewModel!.Contract.Address,
                    cancellationToken: _cancellation.Token);

                // reload balances for all tezos tokens account
                foreach (var currency in _app.Account.Currencies)
                    if (Currencies.IsTezosToken(currency.Name))
                        _app.Account
                            .GetCurrencyAccount<TezosTokenAccount>(currency.Name)
                            .ReloadBalances();
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Tezos tokens Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens Wallet update exception");
            }
        }

        public TezosTokenWalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

#if DEBUG
        protected override void DesignerMode()
        {
            base.DesignerMode();
        }
#endif
    }
}