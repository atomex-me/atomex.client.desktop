using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Atomex.Client.Desktop.Helpers
{
    public class DataGridHelper : Visual
    {
        static DataGridHelper()
        {
            AffectsRender<DataGridHelper>(
                SortInfoProperty
            );
        }

        public static readonly StyledProperty<string> SortInfoProperty =
            AvaloniaProperty.Register<DataGridHelper, string>("SortInfo", string.Empty);


        public static object GetSortInfo(DataGrid dg) => dg.GetValue(SortInfoProperty);

        public static void SetSortInfo(DataGrid dg, string value) => dg.SetValue(SortInfoProperty, value);
    }
}