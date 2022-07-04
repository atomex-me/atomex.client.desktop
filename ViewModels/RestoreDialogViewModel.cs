using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class RestoreDialogViewModel : ViewModelBase
    {
        public Action? OnRestored;
        private readonly IAtomexApp _app;
        private string _restoringEntityTitle = "wallet";

        public string Title => string.Format(CultureInfo.CurrentCulture,
            "Restoring {0} data from blockchain, it will take some time. You can cancel restoring or hide this window with close button, restoring will continue in the background.",
            _restoringEntityTitle);

        public RestoreDialogViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public async void ScanCurrenciesAsync(string[]? currenciesArr = null)
        {
            var cancellation = new CancellationTokenSource();
            var currencies = _app.Account.Currencies.ToList();
            var hdWalletScanner = new HdWalletScanner(_app.Account);

            if (currenciesArr != null)
            {
                currencies = currencies
                    .Where(curr => currenciesArr.Contains(curr.Name)).ToList();

                _restoringEntityTitle = string.Join(", ", currencies.Select(c => c.Name).ToArray());
            }

            var restoreModalVm = MessageViewModel.Message(
                title: "Restoring",
                text: Title,
                nextAction: () =>
                {
                    App.DialogService.Close();
                    cancellation.Cancel();
                },
                buttonTitle: "Cancel",
                withProgressBar: true
            );

            App.DialogService.Show(restoreModalVm);

            Log.Information($"Starting Scan {_restoringEntityTitle}");

            try
            {
                var primaryCurrencies = currencies.Where(curr => !curr.IsToken);
                var tokenCurrencies = currencies.Where(curr => curr.IsToken);

                await Task.Run(() =>
                        Task.WhenAll(primaryCurrencies
                            .Select(currency =>
                                hdWalletScanner.ScanAsync(currency.Name, cancellationToken: cancellation.Token))),
                    cancellation.Token);

                if (currenciesArr == null || Array.IndexOf(currenciesArr, TezosConfig.Xtz) != -1)
                {
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    var tezosTokensScanner = new TezosTokensScanner(tezosAccount);

                    await tezosTokensScanner.UpdateBalanceAsync(
                        cancellationToken: default);
                }

                await Task.Run(() =>
                        Task.WhenAll(tokenCurrencies
                            .Where(currency => currency is not TezosConfig)
                            .Select(currency =>
                                hdWalletScanner.ScanAsync(currency.Name, cancellationToken: cancellation.Token))),
                    cancellation.Token);

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

            finally
            {
                if (App.DialogService.IsCurrentlyShowing(restoreModalVm))
                    App.DialogService.Close();

                OnRestored?.Invoke();
            }
        }

        public async void ScanTezosTokens()
        {
            var cancellation = new CancellationTokenSource();

            var restoreModalVm = MessageViewModel.Message(
                title: "Scanning",
                text: "Tezos tokens scanning, please wait",
                nextAction: () =>
                {
                    App.DialogService.Close();
                    cancellation.Cancel();
                },
                buttonTitle: "Cancel",
                withProgressBar: true
            );

            try
            {
                App.DialogService.Show(restoreModalVm);

                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tezosTokensScanner = new TezosTokensScanner(tezosAccount);

                await tezosTokensScanner.UpdateBalanceAsync(
                    cancellationToken: cancellation.Token);
            }
            catch (Exception e)
            {
                Log.Error("Error during scanning Tezos tokens {Error}", e);
            }
            finally
            {
                if (App.DialogService.IsCurrentlyShowing(restoreModalVm))
                    App.DialogService.Close();
            }
        }
    }
}