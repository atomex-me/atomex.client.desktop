﻿using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public enum SendStage
    {
        Edit,
        Confirmation,
        AdditionalConfirmation
    }

    public abstract class SendViewModel : ViewModelBase
    {
        protected readonly IAtomexApp _app;
        protected CurrencyConfig Currency;
        public ViewModelBase SelectFromViewModel { get; set; }
        protected SelectAddressViewModel SelectToViewModel { get; set; }

        [Reactive] public SendStage Stage { get; set; }
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public string From { get; set; }
        [Reactive] public decimal SelectedFromBalance { get; set; }
        [Reactive] public string To { get; set; }
        [Reactive] protected decimal Amount { get; set; }
        [ObservableAsProperty] public string FromBeautified { get; }
        [ObservableAsProperty] public string AmountString { get; }
        [ObservableAsProperty] public string TotalAmountString { get; }

        public void SetAmountFromString(string value)
        {
            if (value == AmountString)
                return;

            var parsed = decimal.TryParse(
                value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.CurrentCulture,
                out var amount);

            if (!parsed)
                amount = Amount;

            var truncatedValue = amount.TruncateByFormat(CurrencyFormat);

            if (truncatedValue != Amount)
                Amount = truncatedValue;

            Dispatcher.UIThread.InvokeAsync(() => this.RaisePropertyChanged(nameof(AmountString)));
        }

        protected virtual decimal FeeAmount => Fee;
        [Reactive] protected decimal Fee { get; set; }
        [ObservableAsProperty] public string FeeString { get; }

        public void SetFeeFromString(string value)
        {
            if (value == FeeString)
                return;

            var parsed = decimal.TryParse(
                value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.CurrentCulture,
                out var fee);

            if (!parsed)
                fee = Fee;

            var truncatedValue = fee.TruncateByFormat(FeeCurrencyFormat);

            if (truncatedValue != Fee)
                Fee = truncatedValue;

            Dispatcher.UIThread.InvokeAsync(() => this.RaisePropertyChanged(nameof(FeeString)));
        }

        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [Reactive] public string? Warning { get; set; }
        [Reactive] public string? WarningToolTip { get; set; }
        [Reactive] public MessageType WarningType { get; set; }
        [Reactive] public decimal RecommendedMaxAmount { get; set; }
        [Reactive] public string? RecommendedMaxAmountWarning { get; set; }
        [Reactive] public string? RecommendedMaxAmountWarningToolTip { get; set; }
        [Reactive] public MessageType RecommendedMaxAmountWarningType { get; set; }
        public bool ShowAdditionalConfirmation { get; set; }
        [Reactive] public bool UseRecommendedAmount { get; set; }
        [Reactive] public bool UseEnteredAmount { get; set; }
        [Reactive] public bool CanSend { get; set; }
        public decimal AmountToSend => UseRecommendedAmount && (RecommendedMaxAmount < Amount)
            ? RecommendedMaxAmount
            : Amount;

        [Reactive] public string SendRecommendedAmountMenu { get; set; }
        [Reactive] public string SendEnteredAmountMenu { get; set; }

        public string CurrencyCode => CurrencyViewModel.CurrencyCode;
        public string FeeCurrencyCode => CurrencyViewModel.FeeCurrencyCode;
        public string BaseCurrencyCode => CurrencyViewModel.BaseCurrencyCode;
        protected string CurrencyFormat => CurrencyViewModel.CurrencyFormat;
        protected string FeeCurrencyFormat => CurrencyViewModel.FeeCurrencyFormat;
        public string BaseCurrencyFormat => CurrencyViewModel.BaseCurrencyFormat;

        private ReactiveCommand<Unit, Unit> _backCommand;
        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??= (_backCommand = ReactiveCommand.Create(() =>
        {
            App.DialogService.Close();
        }));

        private ReactiveCommand<Unit, Unit> _undoConfirmStageCommand;
        public ReactiveCommand<Unit, Unit> UndoConfirmStageCommand => _undoConfirmStageCommand ??=
            (_undoConfirmStageCommand = ReactiveCommand.Create(() =>
            {
                Stage = Stage == SendStage.AdditionalConfirmation
                    ? SendStage.Confirmation
                    : SendStage.Edit;
            }));

        private ReactiveCommand<Unit, Unit> _selectFromCommand;
        public ReactiveCommand<Unit, Unit> SelectFromCommand => _selectFromCommand ??=
            (_selectFromCommand = ReactiveCommand.Create(FromClick));

        private ReactiveCommand<Unit, Unit> _selectToCommand;
        public ReactiveCommand<Unit, Unit> SelectToCommand => _selectToCommand ??=
            (_selectToCommand = ReactiveCommand.Create(ToClick));

        protected abstract void FromClick();

        protected abstract void ToClick();

        private ReactiveCommand<Unit, Unit> _nextCommand;
        public ReactiveCommand<Unit, Unit> NextCommand =>
            _nextCommand ??= (_nextCommand = ReactiveCommand.CreateFromTask(OnNextCommand));

        private async Task OnNextCommand()
        {
            var feeAmount = !Currency.IsToken ? FeeAmount : 0;

            if (string.IsNullOrEmpty(To))
            {
                Warning = Resources.SvEmptyAddressError;
            }

            else if (!Currency.IsValidAddress(To))
            {
                Warning = Resources.SvInvalidAddressError;
            }

            else if (Amount <= 0)
            {
                Warning = Resources.SvAmountLessThanZeroError;
            }

            else if (FeeAmount <= 0)
            {
                Warning = Resources.SvCommissionLessThanZeroError;
            }

            else if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                Warning = Resources.SvAvailableFundsError;
            }

            if (!string.IsNullOrEmpty(Warning))
                return;

            if ((Stage == SendStage.Confirmation && !ShowAdditionalConfirmation) ||
                 Stage == SendStage.AdditionalConfirmation)
            {
                try
                {
                    App.DialogService.Show(
                        MessageViewModel.Message(title: "Sending, please wait", withProgressBar: true));

                    var error = await Send();

                    if (error != null)
                    {
                        App.DialogService.Show(MessageViewModel.Error(
                            text: error.Description,
                            backAction: () => App.DialogService.Show(this)));

                        return;
                    }

                    App.DialogService.Show(MessageViewModel.Success(
                        text: "Sending was successful",
                        nextAction: () => { App.DialogService.Close(); }));
                }
                catch (Exception e)
                {
                    App.DialogService.Show(MessageViewModel.Error(
                        text: "An error has occurred while sending transaction",
                        backAction: () => App.DialogService.Show(this)));

                    Log.Error(e, "Transaction send error.");
                }
                finally
                {
                    Stage = SendStage.Edit;
                }
            }
            else if (Stage == SendStage.Confirmation && ShowAdditionalConfirmation)
            {
                Stage = SendStage.AdditionalConfirmation;
            }
            else
            {
                Stage = SendStage.Confirmation;
            }
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
            _app = app ?? throw new ArgumentNullException(nameof(app));
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));

            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);
            UseDefaultFee = true;

            var updateAmountCommand = ReactiveCommand.CreateFromTask(UpdateAmount);
            var updateFeeCommand = ReactiveCommand.CreateFromTask(UpdateFee);

            this.WhenAnyValue(
                    vm => vm.From,
                    vm => vm.To,
                    vm => vm.Amount,
                    vm => vm.Fee)
                .Subscribe(_ => Warning = string.Empty);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee,
                    (amount, fee) => Currency.IsToken ? amount : amount + fee)
                .Select(totalAmount => totalAmount.ToString(CurrencyFormat, CultureInfo.CurrentCulture))
                .ToPropertyEx(this, vm => vm.TotalAmountString);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee)
                .Subscribe(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.From,
                    vm => vm.To,
                    (amount, from, to) => from)
                .WhereNotNull()
                .Select(_ => Unit.Default)
                .InvokeCommand(updateAmountCommand);

            this.WhenAnyValue(vm => vm.From)
                .Select(s => s.TruncateAddress())
                .ToPropertyEx(this, vm => vm.FromBeautified);

            this.WhenAnyValue(vm => vm.Amount)
                .Select(amount => amount
                    .TruncateDecimal(Currency.Digits < 9 ? Currency.Digits : 9)
                    .ToString(CurrencyFormat, CultureInfo.CurrentCulture))
                .ToPropertyEx(this, vm => vm.AmountString);

            this.WhenAnyValue(vm => vm.Fee)
                .Where(_ => !string.IsNullOrEmpty(From))
                .Select(_ => Unit.Default)
                .InvokeCommand(updateFeeCommand);

            this.WhenAnyValue(vm => vm.Fee)
                .Select(fee => fee.ToString(FeeCurrencyFormat, CultureInfo.CurrentCulture))
                .ToPropertyEx(this, vm => vm.FeeString);

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .Where(useDefaultFee => useDefaultFee && !string.IsNullOrEmpty(From))
                .Select(_ => Unit.Default)
                .InvokeCommand(updateAmountCommand);

            var canSendObservable1 = this.WhenAnyValue(
                vm => vm.Amount,
                vm => vm.Warning,
                vm => vm.WarningType,
                vm => vm.RecommendedMaxAmountWarning,
                vm => vm.RecommendedMaxAmountWarningType);

            var canSendObservable2 = this.WhenAnyValue(
                vm => vm.UseRecommendedAmount,
                vm => vm.UseEnteredAmount,
                vm => vm.Stage);

            canSendObservable1.CombineLatest(canSendObservable2)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (Stage == SendStage.Edit)
                    {
                        CanSend = To != null &&
                                  Amount > 0 &&
                                  (string.IsNullOrEmpty(Warning) || (Warning != null && WarningType != MessageType.Error)) &&
                                  (string.IsNullOrEmpty(RecommendedMaxAmountWarning) || (RecommendedMaxAmountWarning != null && RecommendedMaxAmountWarningType != MessageType.Error));
                    }
                    else if (Stage == SendStage.Confirmation)
                    {
                        CanSend = true;
                    }
                    else
                    {
                        CanSend = UseRecommendedAmount || UseEnteredAmount;
                    }
                });

            this.WhenAnyValue(vm => vm.Stage)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(s =>
                {
                    UseRecommendedAmount = false;
                    UseEnteredAmount = false;
                });

            this.WhenAnyValue(vm => vm.RecommendedMaxAmount)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(a =>
                {
                    SendRecommendedAmountMenu = string.Format(
                        Resources.SendRecommendedAmountMenu,
                        RecommendedMaxAmount,
                        Currency.Name);
                });

            this.WhenAnyValue(vm => vm.Amount)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(a =>
                {
                    SendEnteredAmountMenu = string.Format(
                        Resources.SendEnteredAmountMenu,
                        Amount,
                        Currency.Name);
                });

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        protected abstract Task UpdateAmount();

        protected virtual Task UpdateFee()
        {
            return Task.CompletedTask;
        }

        private ReactiveCommand<Unit, Unit> _maxCommand;
        public ReactiveCommand<Unit, Unit> MaxCommand =>
            _maxCommand ??= (_maxCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                Warning = string.Empty;
                await OnMaxClick();
            }));

        protected abstract Task OnMaxClick();

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase    = Fee * (quote?.Bid ?? 0m);
        }

        protected abstract Task<Error> Send(CancellationToken cancellationToken = default);

#if DEBUG
        protected void DesignerMode()
        {
            var fromCurrencies = DesignTime.Currencies
                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                .ToList();

            Currency          = fromCurrencies[0].Currency;
            CurrencyViewModel = fromCurrencies[0];
            To                = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            Amount            = 0.00001234m;
            AmountInBase      = 10.23m;
            Fee               = 0.0001m;
            FeeInBase         = 8.43m;

            Warning        = "Insufficient funds";
            WarningToolTip = "";
            WarningType    = MessageType.Error;

            RecommendedMaxAmountWarning        = "We recommend to send no more than 0.073 ETH";
            RecommendedMaxAmountWarningToolTip = "You have tokens that require ETH. Sending this will not leave enough ETH to send or exchange your tokens. We recommend to send no more than 0.073 ETH";
            RecommendedMaxAmountWarningType    = MessageType.Warning;

            Stage = SendStage.Confirmation;
            SendRecommendedAmountMenu = string.Format(Resources.SendRecommendedAmountMenu, 0.073, "ETH");
            SendEnteredAmountMenu = string.Format(Resources.SendEnteredAmountMenu, 0.073, "ETH");
        }
#endif
    }
}