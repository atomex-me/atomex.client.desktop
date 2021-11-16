using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Wallet;
using ReactiveUI;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class RestoreDialogViewModel : ViewModelBase
    {
        private IAtomexApp App;
        private CancellationTokenSource cancellation;
        
        private string restoringEntityTitle = "wallet";
        public string Title => string.Format(CultureInfo.InvariantCulture, 
            "Restoring {0} data from blockchain, please wait...", restoringEntityTitle);

        public RestoreDialogViewModel(IAtomexApp app, string[] currenies = null)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            cancellation = new CancellationTokenSource();

            ScanCurrenciesAsync(currenies);
        }


        private ICommand _cancelCommand;

        public ICommand CancelCommand => _cancelCommand ??= (_cancelCommand = ReactiveCommand.Create(() =>
        {
            cancellation.Cancel();
        }));
        
        private ICommand _hideCommand;

        public ICommand HideCommand => _hideCommand ??= (_hideCommand = ReactiveCommand.Create(() =>
        {
            Desktop.App.DialogService.CloseDialog();
        }));

        private async void ScanCurrenciesAsync(string[] currenciesArr)
        {
            var currencies = App.Account.Currencies.ToList();
            var hdWalletScanner = new HdWalletScanner(App.Account);
            
            if (currenciesArr != null)
            {
                currencies = currencies
                    .Where(curr => currenciesArr.Contains(curr.Name)).ToList();
                
                restoringEntityTitle = string.Join(", ", currencies.Select(c => c.Name).ToArray());
                OnPropertyChanged(nameof(Title));
            }
            
            Log.Information($"Starting Scan {restoringEntityTitle}");

            try
            {
                var primaryCurrencies = currencies.Where(curr => !curr.IsToken);
                var tokenCurrencies = currencies.Where(curr => curr.IsToken);
                
                await Task.Run(() => 
                        Task.WhenAll(primaryCurrencies.Select(currency =>
                            hdWalletScanner.ScanAsync(currency.Name, cancellationToken: cancellation.Token))),
                    cancellation.Token);

                await Task.Run(() => 
                        Task.WhenAll(tokenCurrencies.Select(currency =>
                            hdWalletScanner.ScanAsync(currency.Name, cancellationToken: cancellation.Token))),
                    cancellation.Token);

                Log.Information($"Scan {restoringEntityTitle} done");
            }
            catch (OperationCanceledException)
            {
                Log.Error($"Scan {restoringEntityTitle} cancelled exception");
            }
            catch (Exception)
            {
                Log.Error($"Scan {restoringEntityTitle} exception");
            }
            
            Desktop.App.DialogService.CloseDialog();
        }
    }
}