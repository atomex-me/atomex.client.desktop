using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using DynamicData;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        protected void OnPropertyChanged(string name)
        {
            this.RaisePropertyChanged(name);
        }
    }
}