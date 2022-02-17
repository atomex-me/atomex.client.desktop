using System;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Globalization;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ConversionCurrencyViewModel : ViewModelBase
    {
        public Action MaxClicked { get; set; }
        public Action SelectCurrencyClicked { get; set; }
        public Action GotInputFocus { get; set; }

        [Reactive] public CurrencyViewModel? CurrencyViewModel { get; set; }
        [ObservableAsProperty] public string CurrencyFormat { get; }
        [Reactive] public string? Address { get; set; }
        [Reactive] public decimal Amount { get; set; }
        [ObservableAsProperty] public string AmountString { get; }

        public void SetAmountFromString(string value)
        {
            if (value == AmountString)
                return;

            var parsed = decimal.TryParse(
                s: value,
                style: NumberStyles.AllowDecimalPoint,
                provider: CultureInfo.CurrentCulture,
                result: out var amount);

            if (!parsed)
                amount = Amount;

            var truncatedValue = amount.TruncateByFormat(CurrencyFormat);

            if (truncatedValue != Amount)
                Amount = truncatedValue;

            Dispatcher.UIThread.InvokeAsync(() => this.RaisePropertyChanged(nameof(AmountString)));
        }

        [Reactive] public decimal AmountInBase { get; set; }
        [ObservableAsProperty] public bool Selected { get; }
        [Reactive] public string UnselectedLabel { get; set; }
        [Reactive] public bool UseMax { get; set; }
        [Reactive] public bool IsAmountValid { get; set; }

        private ICommand _maxCommand;
        public ICommand MaxCommand => _maxCommand ??= ReactiveCommand.Create(() => MaxClicked?.Invoke());

        private ICommand _selectCurrencyCommand;
        public ICommand SelectCurrencyCommand =>
            _selectCurrencyCommand ??= ReactiveCommand.Create(() => SelectCurrencyClicked?.Invoke());

        public ConversionCurrencyViewModel()
        {
            IsAmountValid = true;

            if (Design.IsDesignMode)
                DesignerMode();

            this.WhenAnyValue(vm => vm.CurrencyViewModel)
                .Select(i => i != null)
                .ToPropertyExInMainThread(this, vm => vm.Selected);

            this.WhenAnyValue(vm => vm.CurrencyViewModel)
                .Select(vm => vm?.CurrencyFormat ?? "0")
                .ToPropertyExInMainThread(this, vm => vm.CurrencyFormat);

            this.WhenAnyValue(vm => vm.Amount,
                              vm => vm.CurrencyFormat,
                              (amount, currencyFormat) => {
                                  return amount.ToString(currencyFormat, CultureInfo.CurrentCulture);
                              })
                .ToPropertyExInMainThread(this, vm => vm.AmountString);
        }

        public void RaiseGotInputFocus()
        {
            GotInputFocus?.Invoke();
        }

        private void DesignerMode()
        {
            UnselectedLabel = "Choose From";
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(
                currencyConfig: DesignTime.Currencies.Get<BitcoinConfig>("BTC"),
                subscribeToUpdates: false);
            Address = "13V2gzjUL9DiHZLy1WFk9q6pZ3yBsb4TzP".TruncateAddress();
            Amount = 12.000516666m;
            AmountInBase = 3451.43m;
            UseMax = true;
            IsAmountValid = true;
        }
    }
}