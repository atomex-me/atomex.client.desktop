using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;


namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class CollectibleWalletViewModel : WalletViewModel
    {
        private readonly TezosConfig _tezosConfig;
        [Reactive] public TezosTokenViewModel? Collectible { get; set; }

        public string TokenExplorerUri =>
            $"{_tezosConfig.AddressExplorerUri}{Collectible?.Contract.Address}/tokens/{Collectible?.TokenBalance.TokenId}";

        public CollectibleWalletViewModel(IAtomexApp app, Action<ViewModelBase?> showRightPopupContent)
            : base(app: app, showRightPopupContent)
        {
            _tezosConfig = app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            this.WhenAnyValue(vm => vm.Collectible)
                .WhereNotNull()
                .SubscribeInMainThread(tokenViewModel =>
                {
                    LoadAddresses();
                    _ = LoadTransfers(tokenViewModel);
                });
        }

        private async Task LoadTransfers(TezosTokenViewModel collectible)
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
                            .GetTezosTokenTransfersAsync(collectible.Contract.Address,
                                offset: 0,
                                limit: int.MaxValue))
                        .Where(token => token.Token.TokenId == collectible.TokenBalance.TokenId)
                        .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                        .ToList()
                        .ForEachDo(t => t.OnClose = () => ShowRightPopupContent?.Invoke(null))));

                if (selectedTransactionId != null)
                    SelectedTransaction = Transactions.FirstOrDefault(t => t.Id == selectedTransactionId);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransfers for {Contract} canceled", collectible.Contract.Address);
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransfers error for contract {Contract}", collectible.Contract.Address);
            }
            finally
            {
                LoadTransactionsSemaphore.Release();
                Log.Debug("Token transfers loaded for contract {Contract}", collectible.Contract.Address);
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
                    Collectible == null ||
                    args.TokenContract != null && (args.TokenContract != Collectible.Contract.Address ||
                                                   args.TokenId != Collectible.TokenBalance.TokenId))
                {
                    return;
                }

                await Dispatcher.UIThread.InvokeAsync(async () => await LoadTransfers(Collectible),
                    DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        protected override void LoadAddresses()
        {
            if (Collectible == null) return;

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            AddressesViewModel?.Dispose();

            AddressesViewModel = new AddressesViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: Collectible.Contract.Address,
                tokenId: Collectible.TokenBalance.TokenId);
        }

        protected override void OnSendClick()
        {
            if (Collectible == null) return;

            var sendViewModel = new CollectibleSendViewModel(
                app: _app,
                tokenContract: Collectible.Contract.Address,
                tokenId: (int)Collectible.TokenBalance.TokenId,
                tokenType: Collectible.Contract.GetContractType(),
                previewUrl: Collectible.CollectiblePreviewUrl,
                collectibleName: Collectible.TokenBalance.Name,
                from: null);

            App.DialogService.Show(sendViewModel.SelectFromViewModel);
        }

        private ReactiveCommand<string, Unit>? _openInExplorerCommand;

        public ReactiveCommand<string, Unit> OpenInExplorerCommand => _openInExplorerCommand ??=
            ReactiveCommand.Create<string>(address =>
            {
                if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
            });
    }
}