using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using System.Windows.Input;

namespace Atomex.Client.Desktop.ViewModels
{
    public class CurrencySelectionViewModel : ViewModelBase
    {
        [Reactive]
        public CurrencyViewModel? CurrencyViewModel { get; set; }

        [Reactive]
        public string Address { get; set; }

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

        [Reactive]
        public string AmountInBaseString { get; set; }

        private ICommand _maxCommand;
        public ICommand MaxCommand => _maxCommand ??= ReactiveCommand.Create(() => { });

        public CurrencySelectionViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }

        private void DesignerMode()
        {
            var btc = DesignTime.Currencies.Get<BitcoinConfig>("BTC");

            CurrencyViewModel  = CurrencyViewModelCreator.CreateViewModel(btc, subscribeToUpdates: false);
            Address            = "mkns...vg1h";
            AmountString       = "12.000516666";
            AmountInBaseString = "3451.43 $";
        }
    }
}