using System;
using System.Collections.Generic;
using System.Text;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => $"Welcome to Avalonia! {net.ToString()}";
        private Network net = Network.MainNet;
    }
}
