using System;
using Atomex.ViewModels;

namespace Atomex.Client.Desktop.ViewModels.Abstract
{
    public abstract class ISelectAddressViewModel : ViewModelBase
    {
        Action BackAction { get; set; }
        Action<WalletAddressViewModel> ConfirmAction { get; set; }
    }
}