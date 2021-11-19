using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using System;
using Avalonia.Controls;
using ReactiveUI;

namespace Atomex.Client.Desktop.Dialogs.Views
{
    public class DialogServiceView : UserControl
    {
        public DialogServiceView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}