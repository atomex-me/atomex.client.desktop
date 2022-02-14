using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views.SendViews
{
    public class SelectAddressView : UserControl
    {
        public SelectAddressView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CopyButtonOnClick(object? sender, RoutedEventArgs e)
        {
            // var copyTextBlock = this.FindControl<TextBlock>("CopyTextBlock");
            // var initialText = copyTextBlock.Text;
            // copyTextBlock.Text = "Successfully copied!";
            // await Task.Delay(TimeSpan.FromSeconds(5));
            // copyTextBlock.Text = initialText;
        }
    }
}