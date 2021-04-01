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
        private string[] AsyncProperties = new[]
        {
            "AmountString",
            "FeeString"
        };
        
        // public void RaisePropertyChangedAsync(string name)
        // {
        //     Task.Run(() => { this.RaisePropertyChanged(name); }).Wait();
        // }

        public void OnPropertyChanged(string name)
        {
            if (AsyncProperties.IndexOf(name) >= 0)
            {
                Task.Run(() => { this.RaisePropertyChanged(name); }).Wait();
                return;
            }

            this.RaisePropertyChanged(name);
        }
    }
}
