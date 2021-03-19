using System;
using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
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
            
            // SubscribeToServices();
        }

        private void DesignerMode()
        {
        }
    }
}