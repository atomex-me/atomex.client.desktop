using System;
using System.Collections.Generic;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Controls;


namespace Atomex.Client.Desktop.Services
{
    public interface IDialogService<TViewModel>
    {
        void Show(TViewModel viewModel, Action? closeAction = null, double? customHeight = null);
        bool CurrentlyShowed(ViewModelBase viewModel);
        void ShowPrevious();

        bool CloseDialog();
    }
}
