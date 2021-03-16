using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using System.Collections.Generic;

namespace Atomex.Client.Desktop.Views
{
    public class DialogView : UserControl
    {
        public DialogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}