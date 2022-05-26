using System;
using Atomex.Client.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views
{
    public partial class NotificationsView : UserControl
    {
        public NotificationsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Popup_OnClosed(object? sender, EventArgs e)
        {
            if (DataContext is not NotificationsViewModel portfolioViewModel) return;
            portfolioViewModel.IsOpened = false;
        }
    }
}