using System;
using System.Collections.Generic;
using Avalonia.Controls;


namespace Atomex.Client.Desktop.Services
{
    public interface IDialogService<TViewModel>
    {
        void Show(TViewModel viewModel, Action? closeAction = null, double? customHeight = null);
        void ShowPrevious();

        bool CloseDialog();
    }
}
