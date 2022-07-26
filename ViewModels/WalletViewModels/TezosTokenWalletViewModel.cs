using System;
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
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokenWalletViewModel : WalletViewModel
    {
        [Reactive] public TezosTokenViewModel? TokenViewModel { get; set; }

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
                    _ = LoadTransfers(tokenViewModel);
                });
            
            this.WhenAnyValue(vm => vm.TokenViewModel)
                .Where(token => token == null && AddressesViewModel != null)
                .SubscribeInMainThread(_ => AddressesViewModel.Dispose());
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
                if (!args.IsTokenUpdate ||
                    TokenViewModel == null ||
                    args.TokenContract != null && (args.TokenContract != TokenViewModel.Contract.Address || args.TokenId != TokenViewModel.TokenBalance.TokenId))
                {
                    return;
                }

                await Dispatcher.UIThread.InvokeAsync(async () => await LoadTransfers(TokenViewModel),
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