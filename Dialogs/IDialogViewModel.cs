using System;

namespace Atomex.Client.Desktop.Dialogs
{
    public interface IDialogViewModel
    {
        Action? OnClose { get; set; }
    }
}