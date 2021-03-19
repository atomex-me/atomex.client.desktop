namespace Atomex.Client.Desktop.ViewModels.Abstract
{
    public interface IMenuSelector
    {
        int SelectedMenuIndex { get; }
        void SelectMenu(int index);       
    }
}