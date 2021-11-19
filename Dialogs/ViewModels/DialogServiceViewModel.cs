using ReactiveUI;
using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Dialogs.ViewModels
{
    internal sealed class DialogServiceViewModel : ViewModelBase
    {
        public DialogServiceViewModel()
        {
        }

        private ViewModelBase _content;
        public ViewModelBase Content
        {
            get => _content;
            set
            {
                _content = value;
                this.RaisePropertyChanged(nameof(Content));
            }
        }
    }
}