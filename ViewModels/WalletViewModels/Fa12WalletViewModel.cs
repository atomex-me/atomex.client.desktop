using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using Serilog;

using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class Fa12WalletViewModel : WalletViewModel
    {
        public Fa12Config Currency => (Fa12Config)CurrencyViewModel.Currency;

        public Fa12WalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Fa12WalletViewModel(
            IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            Action<string>? setWertCurrency,
            Action<ViewModelBase?> showRightPopupContent,
            CurrencyConfig currency) : base(app, showRightPopupContent, currency, setConversionTab, setWertCurrency)
        {
        }

        //protected override void SubscribeToServices()
        //{
        //    _app.LocalStorage.BalanceChanged += OnBalanceChangedEventHandler;
        //}

        //protected override async void OnBalanceChangedEventHandler(object? sender, BalanceChangedEventArgs args)
        //{
            //try
            //{
            //    var tezosTokenConfig = (TezosTokenConfig)Currency;

            //    var isTokenUpdate = args is TokenBalanceChangedEventArgs eventArgs &&
            //        eventArgs.Tokens.Contains((tezosTokenConfig.TokenContractAddress, tezosTokenConfig.TokenId));

            //    if (!isTokenUpdate)
            //        return;

            //    // update transactions list
            //    await LoadTransactionsAsync();
            //}
            //catch (Exception e)
            //{
            //    Log.Error(e, "Account balance updated event handler error");
            //}
        //}

        protected sealed override async Task LoadTransactionsAsync(bool reset)
        {
            await LoadTransactionsSemaphore.WaitAsync();

            Log.Debug("LoadTransactionsAsync for FA12 {@currency}", Currency.Name);

            try
            {
                if (_app.Account == null)
                    return;

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

                var newTransactions = await Task.Run(async () =>
                {
                    return (await _app.Account
                        .GetCurrencyAccount<Fa12Account>(Currency.Name)
                        .LocalStorage
                        .GetTokenTransfersWithMetadataAsync(
                            contractAddress: Currency.TokenContractAddress,
                            offset: _transactionsLoaded,
                            limit: TRANSACTIONS_LOAD_LIMIT,
                            sort: CurrentSortDirection != null ? CurrentSortDirection.Value : SortDirection.Desc)
                        .ConfigureAwait(false))
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
                            var vm = new TezosTokenTransferViewModel(t.Transfer, t.Metadata, Currency);

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
                Log.Debug("LoadTransactionsAsync canceled for {@currency}", Currency?.Name);
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionsAsync error for {@currency}", Currency?.Name);
            }
            finally
            {
                LoadTransactionsSemaphore.Release();
            }
        }

        protected override void OnReceiveClick()
        {
            var tezosConfig = _app.Account.Currencies.GetByName(TezosConfig.Xtz);

            var receiveViewModel = new ReceiveViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: Currency.TokenContractAddress,
                tokenType: "FA12");

            App.DialogService.Show(receiveViewModel);
        }

        protected override async Task OnUpdateClick()
        {
            _cancellation = new CancellationTokenSource();

            try
            {
                await _app.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name)
                    .UpdateBalanceAsync(_cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Fa12WalletViewModel OnUpdate error");
                // todo: message to user!?
            }
        }

        protected override void LoadAddresses()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            AddressesViewModel = new AddressesViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: Currency.TokenContractAddress);
        }

#if DEBUG
        protected virtual void DesignerMode()
        {
        }
#endif
    }
}