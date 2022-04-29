
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Atomex.Client.Desktop.Controls
{
    public class CopyButton : IconButton
    {
        static CopyButton()
        {
            AffectsRender<CopyButton>(
                ToolTextProperty,
                DoneToolTextProperty,
                IsDoneProperty
            );
        }
        

        public static readonly DirectProperty<CopyButton, string> DoneToolTextProperty =
            AvaloniaProperty.RegisterDirect<CopyButton, string>(
                nameof(DoneToolText),
                o => o.DoneToolText,
                (o, v) => o.DoneToolText = v);

        
        private string _doneToolText;

        public string DoneToolText
        {
            get { return _doneToolText; }
            set { SetAndRaise(DoneToolTextProperty, ref _doneToolText, value); }
        }


        public static readonly DirectProperty<CopyButton, bool> IsDoneProperty =
            AvaloniaProperty.RegisterDirect<CopyButton, bool>(
                nameof(IsDone),
                o => o.IsDone,
                (o, v) => o.IsDone = v);

        private bool _isDone;

        public bool IsDone
        {
            get { return _isDone; }
            set { SetAndRaise(IsDoneProperty, ref _isDone, value); }
        }

        protected override void OnClick()
        {
            base.OnClick();

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                this.FindDescendantOfType<Border>().Classes.Add(nameof(IsDone));
                IsDone = true;
                await Task.Delay(3000);
                IsDone = false;
                this.FindDescendantOfType<Border>().Classes.Remove(nameof(IsDone));
            });
        }
    }
}