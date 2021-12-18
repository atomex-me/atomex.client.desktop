﻿using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading;
using ReactiveUI;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI.Fody.Helpers;
using Serilog;


namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public abstract class SendViewModel : ViewModelBase
    {
        protected IAtomexApp App { get; }

        protected CurrencyConfig Currency { get; set; }

        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public string From { get; set; }
        [Reactive] public decimal SelectedFromAmount { get; set; }
        [Reactive] public string To { get; set; }
        [Reactive] protected decimal Amount { get; set; }
        [ObservableAsProperty] public string AmountString { get; }

        public void SetAmountFromString(string value)
        {
            if (value == AmountString) return;
            var parsed = decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                out var amount);
            {
                if (!parsed) amount = Amount;
                var truncatedValue = amount.TruncateByFormat(CurrencyFormat);
                if (truncatedValue != Amount)
                {
                    Amount = truncatedValue;
                }

                Dispatcher.UIThread.InvokeAsync(() => this.RaisePropertyChanged(nameof(AmountString)));
            }
        }

        [Reactive] protected decimal Fee { get; set; }
        [ObservableAsProperty] public string FeeString { get; }

        public void SetFeeFromString(string value)
        {
            if (value == FeeString) return;
            var parsed = decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                out var fee);
            {
                if (!parsed) fee = Fee;
                var truncatedValue = fee.TruncateByFormat(FeeCurrencyFormat);

                if (truncatedValue != Fee)
                {
                    Fee = Math.Min(truncatedValue, Currency.GetMaximumFee());
                }

                Dispatcher.UIThread.InvokeAsync(() => this.RaisePropertyChanged(nameof(FeeString)));
            }
        }

        [Reactive] public bool UseDefaultFee { get; set; }

        [Reactive] public decimal AmountInBase { get; set; }

        [Reactive] public decimal FeeInBase { get; set; }

        [Reactive] public string Warning { get; set; }

        public string CurrencyCode => CurrencyViewModel.CurrencyCode;

        public string FeeCurrencyCode => CurrencyViewModel.FeeCurrencyCode;

        public string BaseCurrencyCode => CurrencyViewModel.BaseCurrencyCode;

        protected string CurrencyFormat => CurrencyViewModel.CurrencyFormat;

        protected string FeeCurrencyFormat => CurrencyViewModel.FeeCurrencyFormat;

        public string BaseCurrencyFormat => CurrencyViewModel.BaseCurrencyFormat;


        private ReactiveCommand<Unit, Unit> _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??= (_backCommand = ReactiveCommand.Create(() =>
        {
            Desktop.App.DialogService.Close();
        }));

        private ReactiveCommand<Unit, Unit> _selectFromCommand;

        public ReactiveCommand<Unit, Unit> SelectFromCommand => _selectFromCommand ??=
            (_selectFromCommand = ReactiveCommand.Create(() => { }));

        private ReactiveCommand<Unit, Unit> _nextCommand;

        public ReactiveCommand<Unit, Unit> NextCommand =>
            _nextCommand ??= (_nextCommand = ReactiveCommand.Create(OnNextCommand));

        private void OnNextCommand()
        {
            if (string.IsNullOrEmpty(To))
            {
                Warning = Resources.SvEmptyAddressError;
                return;
            }

            if (!Currency.IsValidAddress(To))
            {
                Warning = Resources.SvInvalidAddressError;
                return;
            }

            if (Amount <= 0)
            {
                Warning = Resources.SvAmountLessThanZeroError;
                return;
            }

            if (Fee <= 0)
            {
                Warning = Resources.SvCommissionLessThanZeroError;
                return;
            }

            var isToken = Currency.FeeCurrencyName != Currency.Name;

            var feeAmount = !isToken ? Fee : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                Warning = Resources.SvAvailableFundsError;
                return;
            }

            var confirmationViewModel = new SendConfirmationViewModel
            {
                Currency = Currency,
                To = To,
                Amount = Amount,
                AmountInBase = AmountInBase,
                BaseCurrencyCode = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,
                Fee = Fee,
                UseDeafultFee = UseDefaultFee,
                FeeInBase = FeeInBase,
                CurrencyCode = CurrencyCode,
                CurrencyFormat = CurrencyFormat,

                FeeCurrencyCode = FeeCurrencyCode,
                FeeCurrencyFormat = FeeCurrencyFormat,
                BackView = this,
                SendCallback = Send
            };

            Desktop.App.DialogService.Show(confirmationViewModel);
        }

        public SendViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public SendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));

            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);
            UseDefaultFee = true;

            var updateAmountCommand = ReactiveCommand.CreateFromTask<decimal>(UpdateAmount);
            var updateFeeCommand = ReactiveCommand.CreateFromTask<decimal>(UpdateFee);

            this.WhenAnyValue(
                    vm => vm.To,
                    vm => vm.Amount,
                    vm => vm.Fee
                )
                .Subscribe(_ => Warning = string.Empty);

            this.WhenAnyValue(vm => vm.Amount)
                .InvokeCommand(updateAmountCommand);

            this.WhenAnyValue(vm => vm.Amount)
                .Select(amount => amount.ToString(CurrencyFormat, CultureInfo.InvariantCulture))
                .ToPropertyEx(this, vm => vm.AmountString);

            this.WhenAnyValue(vm => vm.Fee)
                .InvokeCommand(updateFeeCommand);

            this.WhenAnyValue(vm => vm.Fee)
                .Select(fee => fee.ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture))
                .ToPropertyEx(this, vm => vm.FeeString);

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .Where(useDefaultFee => useDefaultFee)
                .Subscribe(_ => updateAmountCommand.Execute(Amount));

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        protected abstract Task UpdateAmount(decimal amount);
        protected abstract Task UpdateFee(decimal fee);

        private ReactiveCommand<Unit, Unit> _maxCommand;

        public ReactiveCommand<Unit, Unit> MaxCommand =>
            _maxCommand ??= (_maxCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Warning = string.Empty;
                await OnMaxClick();
            }));

        protected abstract Task OnMaxClick();

        protected void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (quote?.Bid ?? 0m);
        }

        protected abstract Task<Error> Send(
            SendConfirmationViewModel confirmationViewModel,
            CancellationToken cancellationToken = default);

        protected static string GetShortenedAddress(string address)
        {
            const int length = 4;
            return $"{address[..length]}···{address[^length..]}";
        }

        private void DesignerMode()
        {
            var fromCurrencies = DesignTime.Currencies
                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                .ToList();

            Currency = fromCurrencies[0].Currency;
            CurrencyViewModel = fromCurrencies[0];
            To = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            Amount = 0.00001234m;
            AmountInBase = 10.23m;
            Fee = 0.0001m;
            FeeInBase = 8.43m;
        }
    }
}