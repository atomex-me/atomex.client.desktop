using System;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views.WalletViews
{
    public partial class TezosTokensView : UserControl
    {
        public TezosTokensView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Popup_OnClosed(object? sender, EventArgs e)
        {
            if (DataContext is not TezosTokenViewModel tezosTokenViewModel) return;
            tezosTokenViewModel.IsPopupOpened = false;
        }
    }
}