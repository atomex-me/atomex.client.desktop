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
        private ViewModelBase _lastDialog;
        private Action _closeAction;

        private double? _customHeight;

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

        public bool CloseDialog()
        {
            var result = IsDialogOpened;
            if (IsDialogOpened)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _lastDialog = _dialogServiceViewModel.Content;
                    _dialogServiceView.Close();
                    IsDialogOpened = false;
                    _closeAction?.Invoke();
                    BuildDialogServiceView();
                });
            }

            return result;
        }

        public void ShowPrevious()
        {
            if (_lastDialog != null)
            {
                Show(_lastDialog, _closeAction, _customHeight);
            }
        }

        public bool CurrentlyShowed(ViewModelBase viewModel)
        {
            var res = _dialogServiceViewModel.Content?.GetType() == viewModel.GetType() && IsDialogOpened;
            return res;
        }

        public void Show(ViewModelBase viewModel, Action? closeAction = null, double? customHeight = null)
        {
            using var source = new CancellationTokenSource();

            _lastDialog = _dialogServiceViewModel.Content;

            _dialogServiceViewModel.Content = viewModel;
            _closeAction = closeAction;
            _customHeight = customHeight;
            AdjustDialogWindowSize(viewModel, customHeight);

            if (IsDialogOpened) return;

            _dialogServiceView.ShowDialog(_owner)
                .ContinueWith(t => source.Cancel(),
                    TaskScheduler.FromCurrentSynchronizationContext());
            IsDialogOpened = true;

            // Dispatcher.UIThread.MainLoop(source.Token);
        }

        private void AdjustDialogWindowSize(ViewModelBase dialogVM, double? customHeight)
        {
            var contentView = _viewLocator.Build(dialogVM);

            double viewHeight = contentView.Height;
            if (contentView.Name == "MessageView" && viewHeight is Double.NaN)
            {
                viewHeight = 200;
            }


            var contentHeight = customHeight ?? viewHeight;
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