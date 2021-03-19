using System;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ConversionViewModel : ViewModelBase, IConversionViewModel
    {
        protected IAtomexApp App { get; }

        public ConversionViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public ConversionViewModel(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            // SubscribeToServices();
        }

        private void DesignerMode()
        {
        }

        protected Currency _fromCurrency;

        public virtual Currency FromCurrency
        {
            get => _fromCurrency;
            set { }
        }

        protected Currency _toCurrency;

        public virtual Currency ToCurrency
        {
            get => _toCurrency;
            set { }
        }
    }
}