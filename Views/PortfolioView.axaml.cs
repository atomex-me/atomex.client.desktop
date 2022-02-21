using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using OxyPlot;
using OxyPlot.Avalonia;

namespace Atomex.Client.Desktop.Views
{
    public class PortfolioView : UserControl
    {
        public PortfolioView()
        {
            InitializeComponent();

            var plotView = this.FindControl<PlotView>("PlotView");
            plotView?.ActualController.BindMouseEnter(PlotCommands.HoverSnapTrack);

#if DEBUG
            if (!Design.IsDesignMode) return;

            var parentBorder = this.FindControl<Border>("ParentBorder");
            parentBorder.Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x21, 0x39));
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}