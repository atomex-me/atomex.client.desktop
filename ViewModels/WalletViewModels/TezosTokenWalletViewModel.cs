using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokenWalletViewModel : WalletViewModel
    {
        [Reactive] public TezosTokenViewModel? TokenViewModel { get; set; }

        public TezosTokenWalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public TezosTokenWalletViewModel(
            IAtomexApp app,
            Action<ViewModelBase?> showRightPopupContent) :
            base(app: app, showRightPopupContent: showRightPopupContent)
        {
            this.WhenAnyValue(vm => vm.TokenViewModel)
                .WhereNotNull()
                .Where(_ => this is not CollectibleWalletViewModel)
                .Select(tokenViewModel => tokenViewModel.TokenBalance.Name)
                .SubscribeInMainThread(header => Header = header);

            this.WhenAnyValue(vm => vm.TokenViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(tokenViewModel =>
                {
                    LoadAddresses();
                    _ = LoadTransfers(tokenViewModel, reset: true);
                });
            
            this.WhenAnyValue(vm => vm.TokenViewModel)
                .Where(token => token == null && AddressesViewModel != null)
                .SubscribeInMainThread(_ => AddressesViewModel.Dispose());
        }

        private async Task LoadTransfers(TezosTokenViewModel tokenViewModel, bool reset)
        {
            await LoadTransactionsSemaphore.WaitAsync();

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                _isTransactionsLoading = true;

                if (reset)
                {
                    // reset exists transactions
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Transactions.Clear();
                        _transactionsLoaded = 0;
                    });
                }

                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var newTransactions = await Task.Run(async () =>
                {
                    return (await tezosAccount
                        .LocalStorage
                        .GetTokenTransfersWithMetadataAsync(
                            contractAddress: tokenViewModel.Contract.Address,
                            offset: _transactionsLoaded,
                            limit: TRANSACTIONS_LOAD_LIMIT,
                            sort: CurrentSortDirection != null ? CurrentSortDirection.Value : SortDirection.Desc))
                        .Where(t => t.Transfer.Token.TokenId == tokenViewModel.TokenBalance.TokenId)
                        .ToList();
                });

                _transactionsLoaded += newTransactions.Count;

                var vmsToUpdate = new List<TransactionViewModelBase>();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var selectedTransactionId = SelectedTransaction?.Id;

                    var transactionViewModels = newTransactions
                        .Select(t =>
                        {
                            var vm = new TezosTokenTransferViewModel(t.Transfer, t.Metadata, tezosConfig);

                            if (vm.TransactionMetadata != null)
                                vmsToUpdate.Add(vm);

                            return vm;
                        })
                        .ToList()
                        .ForEachDo(t => t.OnClose = () => ShowRightPopupContent?.Invoke(null));

                    Transactions.AddRange(transactionViewModels);

                    if (selectedTransactionId != null)
                        SelectedTransaction = Transactions.FirstOrDefault(t => t.Id == selectedTransactionId);
                });

                // resolve view models
                if (vmsToUpdate.Any())
                {
                    await ResolveTransactionsMetadataAsync(vmsToUpdate)
                        .ConfigureAwait(false);
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

        //protected override void SubscribeToServices()
        //{
        //    _app.LocalStorage.BalanceChanged += OnBalanceChangedEventHandler;
        //}

        //protected override async void OnBalanceChangedEventHandler(object? sender, BalanceChangedEventArgs args)
        //{
        //    try
        //    {
        //        var isTokenUpdate = args is TokenBalanceChangedEventArgs eventArgs &&
        //            TokenViewModel != null &&
        //            eventArgs.Tokens.Contains((TokenViewModel.Contract.Address, TokenViewModel.TokenBalance.TokenId));

        //        if (!isTokenUpdate)
        //            return;
 
        //        await Dispatcher.UIThread.InvokeAsync(async () => await LoadTransfers(TokenViewModel),
        //            DispatcherPriority.Background);
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Error(e, "Account balance updated event handler error");
        //    }
        //}

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

        protected override void OnReachEndOfScroll()
        {
            _ = LoadTransfers(TokenViewModel, reset: false);
        }

        protected override void LoadAddresses()
        {
            if (TokenViewModel == null)
                return;

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

                var tezosTokensScanner = new TezosTokensWalletScanner(tezosAccount);

                await tezosTokensScanner.UpdateBalanceAsync(
                    tokenContract: TokenViewModel!.Contract.Address,
                    tokenId: (int)TokenViewModel!.TokenBalance.TokenId,
                    cancellationToken: _cancellation.Token);
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

#if DEBUG
        protected override void DesignerMode()
        {
            base.DesignerMode();
        }
#endif
    }
}