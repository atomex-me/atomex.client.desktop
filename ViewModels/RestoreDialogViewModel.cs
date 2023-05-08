using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Serilog;

using Atomex.Wallet;
using Atomex.LiteDb;

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

        public async Task ScanAsync(LiteDbMigrationResult migrationChanges, CancellationToken cancellationToken = default)
        {
            var cancellation = new CancellationTokenSource();

            var changesGroupsByCurrency = migrationChanges
                .GroupBy(c => c.Currency)
                .Select(g =>
                {
                    var currency = _app.Account.Currencies.FirstOrDefault(c => c.Name == g.Key);

                    return new
                    {
                        Name = g.Key,
                        Entities = g.Select(c => c.EntityType).ToArray(),
                        IsToken = currency?.IsToken ?? false
                    };
                })
                .ToList();

            _restoringEntityTitle = string.Join(", ", changesGroupsByCurrency
                .Select(c => c.Name)
                .Distinct()
                .ToArray());

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

            Log.Information($"Starting scan {_restoringEntityTitle}");

            try
            {
                var walletScanner = new WalletScanner(_app.Account, App.LoggerFactory.CreateLogger<WalletScanner>());

                var primaryCurrencies = changesGroupsByCurrency
                    .Where(c => !c.IsToken)
                    .ToList();

                await Task.Run(async () =>
                {
                    var tasks = primaryCurrencies
                        .Select(changes =>
                        {
                            if (changes.Entities.Contains(MigrationEntityType.Addresses))
                            {
                                return walletScanner.ScanAsync(changes.Name, cancellationToken: cancellation.Token);
                            }
                            else if (changes.Entities.Contains(MigrationEntityType.Transactions))
                            {
                                return walletScanner.UpdateBalanceAsync(changes.Name, cancellationToken: cancellation.Token);
                            }

                            return Task.CompletedTask;  
                        })
                        .ToList();

                    await Task.WhenAll(tasks)
                        .ConfigureAwait(false);
                },
                cancellation.Token);

                var tokenCurrencies = changesGroupsByCurrency
                    .Where(c => c.IsToken)
                    .ToList();

                await Task.Run(async () =>
                {
                    var tasks = tokenCurrencies
                        .Select(changes =>
                        { 
                            if (changes.Entities.Contains(MigrationEntityType.Addresses))
                            {
                                return walletScanner.ScanAsync(changes.Name, cancellationToken: cancellation.Token);
                            }
                            else if (changes.Entities.Contains(MigrationEntityType.Transactions))
                            {
                                return walletScanner.UpdateBalanceAsync(changes.Name, cancellationToken: cancellation.Token);
                            }

                            return Task.CompletedTask;
                        })
                        .ToList();

                    await Task
                        .WhenAll(tasks)
                        .ConfigureAwait(false);

                }, cancellation.Token);

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
    }
}