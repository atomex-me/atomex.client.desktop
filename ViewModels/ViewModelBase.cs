using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        private readonly string[] _asyncProperties =
        {
            "AmountString",
            "FeeString",
            "FeePriceString",
            "GasString",
            "DGSelectedIndex"
        };

        protected void OnPropertyChanged(string name)
        {
            if (_asyncProperties.IndexOf(name) >= 0)
            {
                Task.Run(() => { this.RaisePropertyChanged(name); }).Wait();
                return;
            }

            this.RaisePropertyChanged(name);
        }
    }
}
