namespace Atomex.Client.Desktop.Services
{
    public interface IDialogService<TViewModel>
    {
        void Show(TViewModel viewModel);

        void CloseAll();
    }
}
