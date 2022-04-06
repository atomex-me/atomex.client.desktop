using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using Avalonia.Threading;

using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.TezosTokens;
using Atomex.Core;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using ReactiveUI.Fody.Helpers;

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
            Action<string> setWertCurrency,
            CurrencyConfig currency) : base(app, setConversionTab, setWertCurrency, currency)
        {
        }

        protected override void SubscribeToServices()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        protected sealed override async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for {@currency}.", Currency.Name);

            try
            {
                if (_app.Account == null)
                    return;

                var transactions = (await _app.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name)
                    .DataRepository
                    .GetTezosTokenTransfersAsync(Currency.TokenContractAddress)
                    .ConfigureAwait(false))
                    .ToList();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Transactions = new ObservableCollection<ITransactionViewModel>(
                        transactions.Select(t => new TezosTokenTransferViewModel(t, Currency))
                            .ToList());

                    SortTransactions();
                },
                DispatcherPriority.Background);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransactionsAsync canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionsAsync error for {@currency}.", Currency?.Name);
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

        protected override async void OnUpdateClick()
        {
            if (IsBalanceUpdating)
                return;

            IsBalanceUpdating = true;

            _cancellation = new CancellationTokenSource();

            try
            {
                await _app.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name)
                    .UpdateBalanceAsync(_cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "Fa12WalletViewModel.OnUpdateClick error.");
                // todo: message to user!?
            }

            IsBalanceUpdating = false;
        }

        protected override void OnAddressesClick()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var addressesViewModel = new AddressesViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: Currency.TokenContractAddress);

            App.DialogService.Show(addressesViewModel);
        }

#if DEBUG
        protected virtual void DesignerMode()
        {
        }
#endif
    }
}