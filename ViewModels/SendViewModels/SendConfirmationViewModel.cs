using System;
using System.Windows.Input;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Controls;
using Atomex.Core;
using ReactiveUI;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class SendConfirmationViewModel : ViewModelBase
    {
        public ViewModelBase BackView;

        public Currency Currency { get; set; }
        public string To { get; set; }
        public string CurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal FeePrice { get; set; }
        public decimal FeeAmount => Currency.GetFeeAmount(Fee, FeePrice);
        public bool UseDeafultFee { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal FeeInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }
        public string FeeCurrencyCode { get; set; }
        public string FeeCurrencyFormat { get; set; }

        private ICommand _backCommand;

        public ICommand BackCommand => _backCommand ??= (_backCommand = ReactiveCommand.Create(() =>
        {
            Desktop.App.DialogService.Show(BackView);
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= (_nextCommand = ReactiveCommand.Create(Send));

#if DEBUG
        public SendConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif

        private async void Send()
        {
            var account = App.AtomexApp.Account;

            try
            {
                App.DialogService.Show(new SendingViewModel());
                
                var error = await account
                    .SendAsync(Currency.Name, To, Amount, Fee, FeePrice, UseDeafultFee);

                if (error != null)
                {
                    App.DialogService.Show(MessageViewModel.Error(
                        text: error.Description,
                        backAction: () => Desktop.App.DialogService.Show(this)));
                    
                    return;
                }

                App.DialogService.Show(MessageViewModel.Success(
                    text: "Sending was successful",
                    nextAction: () => { App.DialogService.CloseDialog(); }));
            }
            catch (Exception e)
            {
                App.DialogService.Show(MessageViewModel.Error(
                    text: "An error has occurred while sending transaction.",
                    backAction: () => App.DialogService.Show(this)));

                Log.Error(e, "Transaction send error.");
            }
        }

        private void DesignerMode()
        {
            To = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            Amount = 0.00001234m;
            AmountInBase = 10.23m;
            Fee = 0.0001m;
            FeePrice = 1m;
            FeeInBase = 8.43m;
            UseDeafultFee = true;
        }
    }
}