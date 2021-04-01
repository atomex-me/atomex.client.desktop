using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Dialogs.ViewModels;
using Atomex.Client.Desktop.Dialogs.Views;
using Avalonia;
using Avalonia.Controls;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Services
{
    internal sealed class DialogService<TView> : IDialogService<ViewModelBase> where TView : Window
    {
        private readonly Window _owner;

        private bool IsDialogOpened;
        private DialogServiceViewModel _dialogServiceViewModel;
        private TView _dialogServiceView;
        private ViewLocator _viewLocator;
        private ViewModelBase lastDialog;

        private double DEFAULT_WIDTH = 630;
        private double DEFAULT_HEIGHT = 400;
        private double DEFAULT_PARENT_WINDOW_MARGIN = 12;

        public DialogService(Window owner)
        {
            _owner = owner;

            _viewLocator = new ViewLocator();
            _dialogServiceViewModel = new DialogServiceViewModel();

            BuildDialogServiceView();

            var mainWindowSize = _owner.GetObservable(Window.ClientSizeProperty).Skip(1);
            // todo: make static width of dialog during resize until fit main window size.
            mainWindowSize.Subscribe(value =>
            {
                // _dialogServiceView.Width = value.Width / 2;
                // _dialogServiceView.Height = value.Height / 2;

                CenterDialogWindow();
            });
        }

        private void BuildDialogServiceView()
        {
            _dialogServiceView = (TView) _viewLocator.Build(_dialogServiceViewModel);
            _dialogServiceView.DataContext = _dialogServiceViewModel;
        }

        public void CloseDialog()
        {
            if (IsDialogOpened)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _dialogServiceView.Close();
                    IsDialogOpened = false;
                    BuildDialogServiceView();
                });
            }
        }

        public void Show(ViewModelBase viewModel)
        {
            using var source = new CancellationTokenSource();
            _dialogServiceViewModel.Content = viewModel;
            AdjustDialogWindowSize(viewModel);

            if (IsDialogOpened) return;

            _dialogServiceView.ShowDialog(_owner)
                .ContinueWith(t => source.Cancel(),
                    TaskScheduler.FromCurrentSynchronizationContext());
            IsDialogOpened = true;

            // Dispatcher.UIThread.MainLoop(source.Token);
        }

        private void AdjustDialogWindowSize(ViewModelBase dialogVM)
        {
            var contentView = _viewLocator.Build(dialogVM);

            var contentHeight = contentView.Height;
            var contentWidth = contentView.Width;

            _dialogServiceView.Height =
                (contentHeight > 0 ? contentHeight : DEFAULT_HEIGHT) + DEFAULT_PARENT_WINDOW_MARGIN * 2;
            _dialogServiceView.Width =
                (contentWidth > 0 ? contentWidth : DEFAULT_WIDTH) + DEFAULT_PARENT_WINDOW_MARGIN * 2;

            CenterDialogWindow();
        }

        private void CenterDialogWindow()
        {
            if (IsDialogOpened)
            {
                _dialogServiceView.Position =
                    new PixelPoint(
                        Convert.ToInt32(_owner.Position.X + _owner.ClientSize.Width / 2 - _dialogServiceView.Width / 2),
                        Convert.ToInt32(
                            _owner.Position.Y + _owner.ClientSize.Height / 2 - _dialogServiceView.Height / 2));
            }
        }
    }
}