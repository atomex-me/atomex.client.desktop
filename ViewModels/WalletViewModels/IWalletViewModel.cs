using Atomex.Client.Desktop.ViewModels.TransactionViewModels;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public interface IWalletViewModel
    {
        string Header { get; }
        TransactionViewModelBase? SelectedTransaction { get; set; }
    }
}