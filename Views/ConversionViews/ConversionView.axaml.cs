using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Views
{
    public class ConversionView : UserControl
    {
        private readonly CompositeDisposable _disposables = new();
        private CompositeDisposable? _scrollViewerDisposables;
        private double _verticalHeightMax = 0.0;

        public ConversionView()
        {
            InitializeComponent();

            var listBoxSwaps = this.FindControl<ListBox>("ListBoxSwaps");

            listBoxSwaps?.GetObservable(ListBox.ScrollProperty)
                .OfType<ScrollViewer>()
                .Take(1)
                .Subscribe(sv =>
                {
                    _scrollViewerDisposables?.Dispose();
                    _scrollViewerDisposables = new CompositeDisposable();

                    sv.GetObservable(ScrollViewer.VerticalScrollBarMaximumProperty)
                        .Subscribe(newMax => _verticalHeightMax = newMax)
                        .DisposeWith(_scrollViewerDisposables);

                    sv.GetObservable(ScrollViewer.OffsetProperty)
                        .Subscribe(offset =>
                        {
                            //if (offset.Y <= double.Epsilon)
                            //{
                            //    Console.WriteLine("At Top");
                            //}

                            var delta = Math.Abs(_verticalHeightMax - offset.Y);

                            if (delta <= double.Epsilon)
                            {
                                //Console.WriteLine("At Bottom");
                                var viewModel = DataContext as ConversionViewModel;

                                viewModel?.ReachEndOfScroll();
                            }
                        })
                        .DisposeWith(_disposables);

                })
                .DisposeWith(_disposables);

#if DEBUG
            if (Design.IsDesignMode)
            {
                var designGrid = this.FindControl<Grid>("DesignGrid");
                designGrid.Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x21, 0x39));
            }
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}