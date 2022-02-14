using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        protected void OnPropertyChanged(string name)
        {
            this.RaisePropertyChanged(name);
        }
    }
}