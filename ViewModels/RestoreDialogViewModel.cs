using System;
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

        public RestoreDialogViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            cancellation = new CancellationTokenSource();

            ScanAllCurrenciesAsync();
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

        private async void ScanAllCurrenciesAsync()
        {
            var currencies = App.Account.Currencies.ToList();
            var hdWalletScanner = new HdWalletScanner(App.Account);

            try
            {
                await Task.Run(() =>
                        Task.WhenAll(currencies.Select(currency =>
                            hdWalletScanner.ScanAsync(currency.Name, cancellationToken: cancellation.Token))),
                    cancellation.Token);
                
                Log.Information("Scan All Currencies done");
            }
            catch (OperationCanceledException)
            {
                Log.Error("Scan all currencies cancelled exception");
            }
            catch (Exception)
            {
                Log.Error("Scan all currencies exception");
            }

            if (Desktop.App.DialogService.CurrentlyShowed(this))
                Desktop.App.DialogService.CloseDialog();
            
        }
    }
}