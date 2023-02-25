using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;

namespace Atomex.Client.Desktop.Controls
{
    public class TransactionsList : UserControl
    {
        static TransactionsList()
        {
            AffectsRender<TransactionsList>(
                TransactionsProperty,
                SelectedTransactionProperty,
                CurrentSortFieldProperty,
                CurrentSortDirectionProperty
            );
        }

        public static readonly DirectProperty<TransactionsList, ObservableCollection<TransactionViewModelBase>>
            TransactionsProperty =
                AvaloniaProperty.RegisterDirect<TransactionsList, ObservableCollection<TransactionViewModelBase>>(
                    nameof(Transactions),
                    o => o.Transactions,
                    (o, v) => o.Transactions = v);

        private ObservableCollection<TransactionViewModelBase> _transactions;
        public ObservableCollection<TransactionViewModelBase> Transactions
        {
            get => _transactions;
            set => SetAndRaise(TransactionsProperty, ref _transactions, value);
        }

        public static readonly DirectProperty<TransactionsList, TransactionViewModelBase> SelectedTransactionProperty =
            AvaloniaProperty.RegisterDirect<TransactionsList, TransactionViewModelBase>(
                nameof(SelectedTransaction),
                o => o.SelectedTransaction,
                (o, v) => o.SelectedTransaction = v,
                defaultBindingMode: BindingMode.TwoWay);

        private TransactionViewModelBase _selectedTransaction;
        public TransactionViewModelBase SelectedTransaction
        {
            get => _selectedTransaction;
            set => SetAndRaise(SelectedTransactionProperty, ref _selectedTransaction, value);
        }

        public static readonly DirectProperty<TransactionsList, ICommand> SetSortTypeCommandProperty =
            AvaloniaProperty.RegisterDirect<TransactionsList, ICommand>(
                nameof(SetSortTypeCommand),
                control => control.SetSortTypeCommand,
                (control, command) => control.SetSortTypeCommand = command);

        private ICommand _setSortTypeCommandCommand;
        public ICommand SetSortTypeCommand
        {
            get => _setSortTypeCommandCommand;
            set => SetAndRaise(SetSortTypeCommandProperty, ref _setSortTypeCommandCommand, value);
        }

        public static readonly DirectProperty<TransactionsList, TxSortField?> CurrentSortFieldProperty =
            AvaloniaProperty.RegisterDirect<TransactionsList, TxSortField?>(
                nameof(CurrentSortField),
                o => o.CurrentSortField,
                (o, v) => o.CurrentSortField = v);

        private TxSortField? _currentSortField;
        public TxSortField? CurrentSortField
        {
            get => _currentSortField;
            set => SetAndRaise(CurrentSortFieldProperty, ref _currentSortField, value);
        }

        public static readonly DirectProperty<TransactionsList, SortDirection?> CurrentSortDirectionProperty =
            AvaloniaProperty.RegisterDirect<TransactionsList, SortDirection?>(
                nameof(CurrentSortDirection),
                o => o.CurrentSortDirection,
                (o, v) => o.CurrentSortDirection = v);

        private SortDirection? _currentSortDirection;
        public SortDirection? CurrentSortDirection
        {
            get => _currentSortDirection;
            set => SetAndRaise(CurrentSortDirectionProperty, ref _currentSortDirection, value);
        }

        public static readonly DirectProperty<TransactionsList, ICommand> ReachEndOfScrollCommandProperty =
            AvaloniaProperty.RegisterDirect<TransactionsList, ICommand>(
                name: nameof(ReachEndOfScrollCommand),
                getter: control => control.ReachEndOfScrollCommand,
                setter: (control, command) => control.ReachEndOfScrollCommand = command);

        private ICommand _reachEndOfScrollCommand;
        public ICommand ReachEndOfScrollCommand
        {
            get => _reachEndOfScrollCommand;
            set => SetAndRaise(ReachEndOfScrollCommandProperty, ref _reachEndOfScrollCommand, value);
        }

        private readonly CompositeDisposable _disposables = new();
        private CompositeDisposable? _scrollViewerDisposables;
        private double _verticalHeightMax = 0.0;

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            var listBox = this.FindDescendantOfType<ListBox>();

            listBox?.GetObservable(ListBox.ScrollProperty)
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
                                ReachEndOfScrollCommand?.Execute(null);
                            }
                        })
                        .DisposeWith(_disposables);

                })
                .DisposeWith(_disposables);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            _scrollViewerDisposables?.Dispose();
            _disposables.Dispose();
        }
    }
}