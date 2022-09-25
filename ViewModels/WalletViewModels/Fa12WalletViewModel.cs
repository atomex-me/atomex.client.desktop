using System;
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
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class Fa12WalletViewModel : WalletViewModel
    {
        public Fa12Config Currency => CurrencyViewModel.Currency as Fa12Config;

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

        protected override void SubscribeToServices()
        {
            _app.LocalStorage.BalanceChanged += OnBalanceChangedEventHandler;
        }

        protected override async void OnBalanceChangedEventHandler(object? sender, BalanceChangedEventArgs args)
        {
            try
            {
                var tezosTokenConfig = (TezosTokenConfig)Currency;

                if (args is not TokenBalanceChangedEventArgs eventArgs ||
                    eventArgs.TokenContract != null && (eventArgs.TokenContract != tezosTokenConfig.TokenContractAddress || eventArgs.TokenId != tezosTokenConfig.TokenId))
                {
                    return;
                }

                // update transactions list
                await LoadTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        protected sealed override async Task LoadTransactionsAsync()
        {
            await LoadTransactionsSemaphore.WaitAsync();

            Log.Debug("LoadTransactionsAsync for FA12 {@currency}.", Currency.Name);
            try
            {
                if (_app.Account == null)
                    return;

                IsTransactionsLoading = true;

                var transactions = (await _app.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name)
                    .LocalStorage
                    .GetTokenTransfersAsync(Currency.TokenContractAddress, offset: 0, limit: int.MaxValue)
                    .ConfigureAwait(false))
                    .ToList();

                await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var selectedTransactionId = SelectedTransaction?.Id;

                        Transactions = SortTransactions(
                            transactions
                                .Select(t => new TezosTokenTransferViewModel(t, Currency))
                                .ToList()
                                .ForEachDo(t => t.OnClose = () => ShowRightPopupContent?.Invoke(null)));

                        if (selectedTransactionId != null)
                            SelectedTransaction = Transactions.FirstOrDefault(t => t.Id == selectedTransactionId);
                    }
                );
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransactionsAsync canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionsAsync error for {@currency}.", Currency?.Name);
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