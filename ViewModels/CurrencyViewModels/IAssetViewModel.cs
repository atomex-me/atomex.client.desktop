using Avalonia.Media.Imaging;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public interface IAssetViewModel
    {
        string? PreviewUrl { get; }
        // todo: Remove IBitmap after implementing own image backend.
        IBitmap? BitmapIcon { get; }
        string? IconPath { get; }
        string? DisabledIconPath { get; }
        string CurrencyName { get; }
        string CurrencyCode { get; }
        string CurrencyDescription { get; }
        string CurrencyFormat { get; }
        string? BaseCurrencyFormat { get; }
        decimal TotalAmount { get; }
        decimal TotalAmountInBase { get; }
    }
}