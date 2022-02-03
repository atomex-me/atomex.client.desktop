using System;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Globalization;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ConversionCurrencyViewModel : ViewModelBase
    {
        public Action MaxClicked { get; set; }
        public Action SelectCurrencyClicked { get; set; }
        public Action GotInputFocus { get; set; }

        [Reactive] public CurrencyViewModel? CurrencyViewModel { get; set; }
        [Reactive] public string? Address { get; set; }

        public decimal Amount;
        public string AmountString
        {
            get => Amount.ToString(CurrencyViewModel?.CurrencyFormat ?? "0");
            set
            {
                if (!decimal.TryParse(
                    s: value,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.CurrentCulture,
                    result: out var amount))
                {
                    Amount = 0;
                }
                else
                {
                    Amount = amount.TruncateByFormat(CurrencyViewModel?.CurrencyFormat ?? "0");

                    if (Amount > long.MaxValue)
                        Amount = long.MaxValue;
                }

                this.RaisePropertyChanged(nameof(AmountString));
            }
        }

        [Reactive] public decimal AmountInBase { get; set; }
        [ObservableAsProperty] public bool Selected { get; }
        [Reactive] public string UnselectedLabel { get; set; }
        [Reactive] public bool UseMax { get; set; }
        [Reactive] public bool IsAmountValid { get; set; }

        private ICommand _maxCommand;
        public ICommand MaxCommand => _maxCommand ??= ReactiveCommand.Create(() => MaxClicked?.Invoke());

        private ICommand _selectCurrencyCommand;
        public ICommand SelectCurrencyCommand => _selectCurrencyCommand ??= ReactiveCommand.Create(() => SelectCurrencyClicked?.Invoke());

        public ConversionCurrencyViewModel()
        {
            IsAmountValid = true;

            if (Design.IsDesignMode)
                DesignerMode();

            this.WhenAnyValue(vm => vm.CurrencyViewModel)
                .Select(i => i != null)
                .ToPropertyEx(this, vm => vm.Selected);
        }

        public void RaiseGotInputFocus()
        {
            GotInputFocus?.Invoke();
        }

        private void DesignerMode()
        {
            UnselectedLabel    = "Choose From";
            CurrencyViewModel  = CurrencyViewModelCreator.CreateViewModel(
                currencyConfig: DesignTime.Currencies.Get<BitcoinConfig>("BTC"),
                subscribeToUpdates: false);
            Address            = "13V2gzjUL9DiHZLy1WFk9q6pZ3yBsb4TzP".TruncateAddress();
            AmountString       = "12.000516666";
            AmountInBase       = 3451.43m;
            UseMax             = true;
            IsAmountValid      = true;
        }
    }
}