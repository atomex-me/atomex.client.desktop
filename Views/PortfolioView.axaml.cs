using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}