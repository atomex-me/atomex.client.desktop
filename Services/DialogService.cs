using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Services
{
    internal sealed class DialogService<TView> : IDialogService<ViewModelBase> where TView : Window
    {
        private readonly Window _owner;
        private IList<TView> openedViews;
        private ViewModelBase lastOpenedVM;

        public DialogService(Window owner)
        {
            _owner = owner;
            openedViews = new List<TView>();
        }

        public void CloseAll()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var view in openedViews)
                {
                    view.Close();
                }

                openedViews = new List<TView>();
            });
        }

        public void Show(ViewModelBase viewModel)
        {
            var viewLocator = new ViewLocator();
            TView view = (TView) viewLocator.Build(viewModel);
            openedViews.Add(view);
            view.DataContext = viewModel;

            using (var source = new CancellationTokenSource())
            {
                view.ShowDialog(_owner)
                    .ContinueWith(t => source.Cancel(),
                        TaskScheduler.FromCurrentSynchronizationContext());

                var mainWindowSize = _owner.GetObservable(Window.ClientSizeProperty).Skip(1);

                // todo: make static width of dialog during resize until fit main window size.
                mainWindowSize.Subscribe(value =>
                {
                    Console.WriteLine($"ClientSizeProperty Changed; Dialog width: {view.Width}, Height: {view.Height}");
                    view.Width = value.Width / 2;
                    view.Height = value.Height / 2;

                    view.Position =
                        new PixelPoint(
                            Convert.ToInt32(_owner.Position.X + _owner.ClientSize.Width / 2 - view.Width / 2),
                            Convert.ToInt32(_owner.Position.Y + _owner.ClientSize.Height / 2 - view.Height / 2));
                });

                // Dispatcher.UIThread.MainLoop(source.Token);
            }
        }
    }
}