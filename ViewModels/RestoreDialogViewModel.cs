using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using ReactiveUI;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class RestoreDialogViewModel : ViewModelBase
    {
        public Action? OnRestored;
        private readonly IAtomexApp _app;
        private readonly CancellationTokenSource _cancellation;

        private string _restoringEntityTitle = "wallet";

        public string Title => string.Format(CultureInfo.InvariantCulture,
            "Restoring {0} data from blockchain, please wait...", _restoringEntityTitle);

        public RestoreDialogViewModel(IAtomexApp app, string[] currenies = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _cancellation = new CancellationTokenSource();

            ScanCurrenciesAsync(currenies);
        }


        private ICommand _cancelCommand;

        public ICommand CancelCommand => _cancelCommand ??= (_cancelCommand = ReactiveCommand.Create(() =>
        {
            _cancellation.Cancel();
        }));

        private ICommand _hideCommand;

        public ICommand HideCommand => _hideCommand ??= (_hideCommand = ReactiveCommand.Create(() =>
        {
            App.DialogService.Close();
        }));

        private async void ScanCurrenciesAsync(string[] currenciesArr)
        {
            var currencies = _app.Account.Currencies.ToList();
            var hdWalletScanner = new HdWalletScanner(_app.Account);

            if (currenciesArr != null)
            {
                currencies = currencies
                    .Where(curr => currenciesArr.Contains(curr.Name)).ToList();

                _restoringEntityTitle = string.Join(", ", currencies.Select(c => c.Name).ToArray());
                OnPropertyChanged(nameof(Title));
            }

            Log.Information($"Starting Scan {_restoringEntityTitle}");

            try
            {
                var primaryCurrencies = currencies.Where(curr => !curr.IsToken);
                var tokenCurrencies = currencies.Where(curr => curr.IsToken);
                
                await Task.Run(() =>
                        Task.WhenAll(primaryCurrencies
                            .Where(currency => currency.Name != TezosConfig.Xtz)
                            .Select(currency =>
                                hdWalletScanner.ScanAsync(currency.Name, cancellationToken: _cancellation.Token))),
                    _cancellation.Token);

                if (currenciesArr == null || Array.IndexOf(currenciesArr, TezosConfig.Xtz) != -1)
                {
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    var tezosTokensScanner = new TezosTokensScanner(tezosAccount);
                    
                    await tezosTokensScanner.ScanAsync(
                        skipUsed: false,
                        cancellationToken: default);

                    // reload balances for all tezos tokens account
                    foreach (var currency in _app.Account.Currencies)
                    {
                        if (Currencies.IsTezosToken(currency.Name))
                            _app.Account
                                .GetCurrencyAccount<TezosTokenAccount>(currency.Name)
                                .ReloadBalances();
                    }
                }
                
                await Task.Run(() =>
                        Task.WhenAll(tokenCurrencies
                            .Where(currency => currency is not TezosConfig)
                            .Select(currency =>
                                hdWalletScanner.ScanAsync(currency.Name, cancellationToken: _cancellation.Token))),
                    _cancellation.Token);

                OnRestored?.Invoke();
                Log.Information($"Scan {_restoringEntityTitle} done");
            }
            catch (OperationCanceledException)
            {
                Log.Error($"Scan {_restoringEntityTitle} cancelled exception");
            }
            catch (Exception)
            {
                Log.Error($"Scan {_restoringEntityTitle} exception");
            }

            if (!App.DialogService.IsCurrentlyShowing(this)) return;
            App.DialogService.Close();
        }
    }
}