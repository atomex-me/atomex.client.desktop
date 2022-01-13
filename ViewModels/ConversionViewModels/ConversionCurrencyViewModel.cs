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

        [Reactive] public CurrencyViewModel? CurrencyViewModel { get; set; }

        [Reactive] public string Address { get; set; }

        private decimal _amount;
        public string AmountString
        {
            get => _amount.ToString(CurrencyViewModel?.CurrencyFormat ?? "0");
            set
            {
                if (!decimal.TryParse(
                    s: value,
                    style: NumberStyles.AllowDecimalPoint,
                    provider: CultureInfo.CurrentCulture,
                    result: out var amount))
                {
                    _amount = 0;
                }
                else
                {
                    _amount = amount.TruncateByFormat(CurrencyViewModel?.CurrencyFormat ?? "0");

                    if (_amount > long.MaxValue)
                        _amount = long.MaxValue;
                }

                this.RaisePropertyChanged(nameof(AmountString));
            }
        }

        [Reactive] public string AmountInBaseString { get; set; }

        private ICommand _maxCommand;
        public ICommand MaxCommand => _maxCommand ??= ReactiveCommand.Create(() => MaxClicked?.Invoke());

        private ICommand _selectCurrencyCommand;
        public ICommand SelectCurrencyCommand => _selectCurrencyCommand ??= ReactiveCommand.Create(() => SelectCurrencyClicked?.Invoke());

        [ObservableAsProperty] public bool Selected { get; }

        [Reactive] public string UnselectedLabel { get; set; }

        public ConversionCurrencyViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();

            this.WhenAnyValue(vm => vm.CurrencyViewModel)
                .Select(i => i != null)
                .ToPropertyEx(this, vm => vm.Selected);
        }

        private void DesignerMode()
        {
            UnselectedLabel    = "Choose From";
            CurrencyViewModel  = CurrencyViewModelCreator.CreateViewModel(
                currencyConfig: DesignTime.Currencies.Get<BitcoinConfig>("BTC"),
                subscribeToUpdates: false);
            Address            = "13V2gzjUL9DiHZLy1WFk9q6pZ3yBsb4TzP".TruncateAddress();
            AmountString       = "12.000516666";
            AmountInBaseString = "$3451.43";
        }
    }
}