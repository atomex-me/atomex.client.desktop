using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Atomex.Client.Desktop.ViewModels;


namespace Atomex.Client.Desktop.Views
{
    public class PasswordControlView : UserControl
    {
        public PasswordControlView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var textBox = this.FindControl<TextBox>("PasswordControl");

            textBox.AttachedToVisualTree += (sender, args) =>
            {
                if (DataContext is PasswordControlViewModel { IsFocused: true }) textBox.Focus();
            };

            // this.PropertyChanged += (s, e) =>
            // {
            //     if (e.Property == Control.DataContextProperty)
            //     {
            //         PasswordControlViewModel dataContext = (PasswordControlViewModel)e.NewValue;
            //
            //         textBox.GetObservable(TextBox.SelectionStartProperty).Subscribe(position =>
            //         {
            //             if (dataContext != null)
            //             {
            //                 dataContext.SelectionStart = position;
            //             }
            //         });
            //
            //         textBox.GetObservable(TextBox.SelectionEndProperty).Subscribe(position =>
            //         {
            //             if (dataContext != null)
            //             {
            //                 dataContext.SelectionEnd = position;
            //             }
            //         });
            //     }
            // };
        }
    }
}