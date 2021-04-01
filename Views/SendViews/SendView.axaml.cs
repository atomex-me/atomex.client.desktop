using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views.SendViews
{
    public class SendView : UserControl
    {
        public SendView()
        {
            InitializeComponent();
            
            // var AmountTextBox = this.FindControl<TextBox>("AmountTextBox");
            //
            // AmountTextBox.AddHandler(TextInputEvent, ViewBlock_TextInput, RoutingStrategies.Tunnel);
            // AmountTextBox.AddHandler(TextInputEvent, ViewBlock_TextInput, RoutingStrategies.Tunnel);
            //
            // void ViewBlock_TextInput(object sender, TextInputEventArgs e)
            // {
            //     Console.WriteLine("AmountInput");
            //     e.Handled = true;
            //     return;
            // }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}