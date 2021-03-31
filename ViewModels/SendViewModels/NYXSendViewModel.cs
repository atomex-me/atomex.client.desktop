using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class NYXSendViewModel : Fa12SendViewModel
    {
        public NYXSendViewModel()
            : base()
        {
        }

        public NYXSendViewModel(
            IAtomexApp app,
            Currency currency)
            : base(app, currency)
        {
        }
    }
}

