using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        public TezosSendViewModel()
            : base()
        {
        }

        public TezosSendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
        }
    }
}