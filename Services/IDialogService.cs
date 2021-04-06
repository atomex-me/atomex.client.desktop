using System.Collections.Generic;
using Avalonia.Controls;


namespace Atomex.Client.Desktop.Services
{
    public interface IDialogService<TViewModel>
    {
        void Show(TViewModel viewModel);
        void ShowPrevious();

        bool CloseDialog();
    }
}
