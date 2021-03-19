using System;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WalletsViewModel : ViewModelBase
    {
        protected IAtomexApp App { get; }
        private IMenuSelector MenuSelector { get; }
        private IConversionViewModel ConversionViewModel { get; }
        
        public WalletsViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public WalletsViewModel(
            IAtomexApp app,
            IMenuSelector menuSelector,
            IConversionViewModel conversionViewModel)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            MenuSelector = menuSelector ?? throw new ArgumentNullException(nameof(menuSelector));
            ConversionViewModel = conversionViewModel ?? throw new ArgumentNullException(nameof(conversionViewModel));

            // SubscribeToServices();
        }

        private void DesignerMode()
        {
        }
    }
}