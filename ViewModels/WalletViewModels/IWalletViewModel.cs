using Avalonia.Media;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public interface IWalletViewModel
    {
        IBrush Background { get; }
        string Header { get; }
        bool IsSelected { get; set; }
        IBrush OpacityMask { get; }
    }
}