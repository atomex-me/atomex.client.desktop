using Avalonia.Media.Imaging;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public interface IAssetViewModel
    {
        string? IconPath { get; }
        IBitmap? BitmapIcon { get; }
        string DisabledIconPath { get; }
        string CurrencyCode { get; }
        string CurrencyDescription { get; }
        string CurrencyFormat { get; }
        string BaseCurrencyFormat { get; }
        decimal TotalAmount { get; }
        decimal TotalAmountInBase { get; }
        decimal CurrentQuote { get; }
        decimal? DailyChangePercent { get; }
        bool CanExchange { get; }
    }
}