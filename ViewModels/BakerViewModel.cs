using Avalonia.Media.Imaging;

namespace Atomex.Client.Desktop.ViewModels
{
    public class BakerViewModel : ViewModelBase
    {
        public string Logo { get; set; }

        public IBitmap BitmapLogo => App.ImageService.GetImage(Logo);
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Fee { get; set; }
        public decimal MinDelegation { get; set; }
        public decimal StakingAvailable { get; set; }

        public bool IsFull => StakingAvailable <= 0;
        public bool IsMinDelegation => MinDelegation > 0;
    }
}
