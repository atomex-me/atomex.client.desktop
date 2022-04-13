using System;

using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;

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
            
            // App.AtomexClientChanged += OnAtomexClientChangedEventHandler;
            // SubscribeToServices();
        }
        
        //private void OnAtomexClientChangedEventHandler(object sender, AtomexClientChangedEventArgs args)
        //{
        //    var atomexClient = args.AtomexClient;
        //    if (atomexClient?.Account == null) return;

        //    var btc = App.Account.Currencies.GetByName("BTC");

        //    Content = new BitcoinBasedSendViewModel(App, btc);
        //}

        [Reactive]
        public ViewModelBase Content { get; set; }

        private void DesignerMode()
        {
        }
    }
}