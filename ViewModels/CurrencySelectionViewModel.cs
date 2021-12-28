using System;
using System.Windows.Input;
using System.Globalization;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;

namespace Atomex.Client.Desktop.ViewModels
{
    public class CurrencySelectionViewModel : ViewModelBase
    {
        public event EventHandler MaxClicked;
        public event EventHandler ChooseCurrencyClicked;

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
        public ICommand MaxCommand => _maxCommand ??= ReactiveCommand.Create(() => MaxClicked?.Invoke(this, EventArgs.Empty));

        private ICommand _chooseCurrencyCommand;
        public ICommand ChooseCurrencyCommand => _chooseCurrencyCommand ??= ReactiveCommand.Create(() => ChooseCurrencyClicked?.Invoke(this, EventArgs.Empty));

        [Reactive] public bool Selected { get; set; }

        [Reactive] public string UnselectedLabel { get; set; }

        public CurrencySelectionViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }

        private void DesignerMode()
        {
            var btc = DesignTime.Currencies.Get<BitcoinConfig>("BTC");

            Selected           = true;
            UnselectedLabel    = "Choose From";

            CurrencyViewModel  = CurrencyViewModelCreator.CreateViewModel(btc, subscribeToUpdates: false);
            Address            = "13V2gzjUL9DiHZLy1WFk9q6pZ3yBsb4TzP".TruncateAddress();
            AmountString       = "12.000516666";
            AmountInBaseString = "$3451.43";
        }
    }
}