using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Serilog;
using ReactiveUI;
using Avalonia.Threading;
using Avalonia.Media;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.ReceiveViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
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
        private ObservableCollection<TezosTokenTransferViewModel> _transactions;
        public ObservableCollection<TezosTokenTransferViewModel> Transactions
        {
            get => _transactions;
            set { _transactions = value; OnPropertyChanged(nameof(Transactions)); }
        }
        
        public Fa12Config Currency => CurrencyViewModel.Currency as Fa12Config;
        
        public Fa12WalletViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public Fa12WalletViewModel(
            IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            CurrencyConfig currency) : base(app, setConversionTab, currency)
        {
        }

        protected override void SubscribeToServices()
        {
            App.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        protected sealed override async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for {@currency}.", Currency.Name);

            try
            {
                if (App.Account == null)
                    return;

                var transactions = (await App.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name)
                    .DataRepository
                    .GetTezosTokenTransfersAsync(Currency.TokenContractAddress)
                    .ConfigureAwait(false))
                    .ToList();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Transactions = new ObservableCollection<TezosTokenTransferViewModel>(
                        transactions.Select(t => new TezosTokenTransferViewModel(t, Currency))
                            .ToList()
                            .SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));
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
            var tezosConfig = App.Account.Currencies.GetByName(TezosConfig.Xtz);
            var receiveViewModel = new ReceiveViewModel(App, tezosConfig, Currency.TokenContractAddress);
            Desktop.App.DialogService.Show(receiveViewModel);
        }

        protected override async void OnUpdateClick()
        {
            if (IsBalanceUpdating)
                return;

            IsBalanceUpdating = true;

            _cancellation = new CancellationTokenSource();

            try
            {
                await App.Account
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
            var tezosConfig = App.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var addressesViewModel = new AddressesViewModel(
                app: App,
                currency: tezosConfig,
                tokenContract: Currency.TokenContractAddress);

            Desktop.App.DialogService.Show(addressesViewModel);
        }

        protected virtual void DesignerMode()
        {
        }
    }
}