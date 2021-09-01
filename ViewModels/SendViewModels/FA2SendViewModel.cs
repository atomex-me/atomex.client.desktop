using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class FA2SendViewModel : Fa12SendViewModel
    {
        public FA2SendViewModel()
            : base()
        {
        }

        public FA2SendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
        }
    }
}

