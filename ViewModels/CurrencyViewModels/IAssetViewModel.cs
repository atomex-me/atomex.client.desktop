namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public interface IAssetViewModel
    {
        string? PreviewUrl { get; }
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