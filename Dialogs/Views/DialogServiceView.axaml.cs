using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using System;
using ReactiveUI;

namespace Atomex.Client.Desktop.Dialogs.Views
{
    public class DialogServiceView : ReactiveWindow<ReactiveObject>
    {
        public DialogServiceView()
        {
            InitializeComponent();
#if AVALONIA_DIAGNOSTICS
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}