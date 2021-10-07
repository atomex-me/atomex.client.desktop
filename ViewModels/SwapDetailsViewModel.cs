namespace Atomex.Client.Desktop.ViewModels
{
    public class SwapDetailsViewModel : ViewModelBase
    {
        private string _swapId;
        public string SwapId
        {
            get => _swapId;
            set
            {
                _swapId = value;
                OnPropertyChanged(nameof(SwapId));
            }
        }
    }
}