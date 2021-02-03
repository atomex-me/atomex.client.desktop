namespace Atomex.Client.Desktop.Services
{
    internal interface IDialogService<TViewModel>
    {
        void Show(TViewModel viewModel);
    }
}
