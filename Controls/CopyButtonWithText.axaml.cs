using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;


namespace Atomex.Client.Desktop.Controls
{
    public class CopyButtonWithText : Button
    {
        static CopyButtonWithText()
        {
            AffectsRender<CopyButtonWithText>(
                IsCopiedProperty
            );
        }

        public static readonly StyledProperty<bool> IsCopiedProperty =
            AvaloniaProperty.Register<CopyButtonWithText, bool>(nameof(IsCopied));

        public bool IsCopied
        {
            get => GetValue(IsCopiedProperty);
            set => SetValue(IsCopiedProperty, value);
        }

        protected override void OnClick()
        {
            base.OnClick();

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                IsCopied = true;
                await Task.Delay(3000);
                IsCopied = false;
            });
        }
    }
}