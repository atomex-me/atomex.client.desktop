using Avalonia;
using Avalonia.Controls;


namespace Atomex.Client.Desktop.Controls
{
    public class SortButton : Button
    {
        static SortButton()
        {
            AffectsRender<SortButton>(
                SortIsNotActiveProperty,
                SortIsAscProperty,
                SortIsDescProperty,
                TitleProperty
            );
        }

        public static readonly DirectProperty<SortButton, bool> SortIsNotActiveProperty =
            AvaloniaProperty.RegisterDirect<SortButton, bool>(
                nameof(SortIsNotActiveProperty),
                o => o.SortIsNotActive,
                (o, v) => o.SortIsNotActive = v);


        private bool _sortIsNotActive;

        public bool SortIsNotActive
        {
            get { return _sortIsNotActive; }
            private set { SetAndRaise(SortIsNotActiveProperty, ref _sortIsNotActive, value); }
        }

        public static readonly DirectProperty<SortButton, bool> SortIsAscProperty =
            AvaloniaProperty.RegisterDirect<SortButton, bool>(
                nameof(SortIsAscProperty),
                o => o.SortIsAsc,
                (o, v) => o.SortIsAsc = v);


        private bool _sortIsAsc;

        public bool SortIsAsc
        {
            get { return _sortIsAsc; }
            set
            {
                SetAndRaise(SortIsAscProperty, ref _sortIsAsc, value);
                SetSortIsNotActive();
            }
        }


        public static readonly DirectProperty<SortButton, bool> SortIsDescProperty =
            AvaloniaProperty.RegisterDirect<SortButton, bool>(
                nameof(SortIsDescProperty),
                o => o.SortIsDesc,
                (o, v) => o.SortIsDesc = v);


        private bool _sortIsDesc;

        public bool SortIsDesc
        {
            get { return _sortIsDesc; }
            set
            {
                SetAndRaise(SortIsDescProperty, ref _sortIsDesc, value);
                SetSortIsNotActive();
            }
        }

        public static readonly DirectProperty<SortButton, string> TitleProperty =
            AvaloniaProperty.RegisterDirect<SortButton, string>(
                nameof(TitleProperty),
                o => o.Title);


        private string _title;

        public string Title
        {
            get { return _title; }
            set { SetAndRaise(TitleProperty, ref _title, value); }
        }


        private void SetSortIsNotActive()
        {
            SortIsNotActive = !SortIsAsc && !SortIsDesc;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            SetSortIsNotActive();
        }
    }
}