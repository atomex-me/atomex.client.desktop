using System;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Services;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public string TextStr => "dota 3";
        protected IAtomexApp App { get; }
        
        public SettingsViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public SettingsViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            
            App.AtomexClientChanged += OnTerminalChangedEventHandler;
            // SubscribeToServices();
        }
        
        private void OnTerminalChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        {       
            var terminal = args.AtomexClient;
            if (terminal?.Account == null) return;

            var btc = App.Account.Currencies.GetByName("BTC");

            Content = new BitcoinBasedSendViewModel(App, btc);
            var a = 5;
        }

        [Reactive]
        public ViewModelBase Content { get; set; }

        private void DesignerMode()
        {
        }
    }
}