using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;


namespace Atomex.Client.Desktop.Controls
{
    public class StepIndicatorPanel : UserControl
    {
        static StepIndicatorPanel()
        {
            AffectsRender<StepIndicatorPanel>(
                CurrentStepProperty,
                BackgroundProperty,
                ForegroundProperty,
                StepBackgroundProperty,
                CompletedStepBackgroundProperty,
                StepsCountProperty,
                StepEllipseRadiusProperty,
                CompletedStepEllipseRadiusProperty,
                StepLineWidthProperty,
                CompletedStepLineWidthProperty,
                FontSizeProperty,
                FontStyleProperty,
                FontWeightProperty
            );
        }

        public static readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, IBrush>(nameof(Background), Brushes.Transparent);

        public static readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, IBrush>(nameof(Foreground), Brushes.White);

        public static readonly StyledProperty<IBrush> StepBackgroundProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, IBrush>("StepBackground",
                new SolidColorBrush(Color.FromRgb(0x1e, 0x56, 0xaf)));

        public static readonly StyledProperty<IBrush> CompletedStepBackgroundProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, IBrush>("CompletedStepBackground",
                new SolidColorBrush(Color.FromRgb(0x1a, 0x40, 0x70)));

        public static readonly StyledProperty<int> StepsCountProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, int>("StepsCount", 0);

        public static readonly StyledProperty<int> CurrentStepProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, int>("CurrentStep", 0);

        public static readonly StyledProperty<double> StepEllipseRadiusProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, double>("StepEllipseRadius", 20.0);

        public static readonly StyledProperty<double> CompletedStepEllipseRadiusProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, double>("CompletedStepEllipseRadius", 17.0);

        public static readonly StyledProperty<double> StepLineWidthProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, double>("StepLineWidth", 8.0);

        public static readonly StyledProperty<double> CompletedStepLineWidthProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, double>("CompletedStepLineWidth", 4.0);

        // public static DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily",
        //     typeof(FontFamily),
        //     typeof(StepIndicatorPanel),
        //     new FrameworkPropertyMetadata(
        //         new FontFamily(new Uri("pack://application:,,,/resources/"), "./#Roboto Medium"),
        //         FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly StyledProperty<double> FontSizeProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, double>("FontSize", 23.0);

        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, FontStyle>("FontStyle", FontStyle.Normal);

        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.Register<StepIndicatorPanel, FontWeight>("FontWeight", FontWeight.Normal);

        public IBrush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public IBrush StepBackground
        {
            get => GetValue(StepBackgroundProperty);
            set => SetValue(StepBackgroundProperty, value);
        }

        public IBrush CompletedStepBackground
        {
            get => GetValue(CompletedStepBackgroundProperty);
            set => SetValue(CompletedStepBackgroundProperty, value);
        }

        public int StepsCount
        {
            get => GetValue(StepsCountProperty);
            set => SetValue(StepsCountProperty, value);
        }

        private int _currentStep;

        public int CurrentStep
        {
            get => GetValue(CurrentStepProperty);
            set => SetValue(CurrentStepProperty, value);
        }

        public double StepEllipseRadius
        {
            get => GetValue(StepEllipseRadiusProperty);
            set => SetValue(StepEllipseRadiusProperty, value);
        }

        public double CompletedStepEllipseRadius
        {
            get => GetValue(CompletedStepEllipseRadiusProperty);
            set => SetValue(CompletedStepEllipseRadiusProperty, value);
        }

        public double StepLineWidth
        {
            get => GetValue(StepLineWidthProperty);
            set => SetValue(StepLineWidthProperty, value);
        }

        public double CompletedStepLineWidth
        {
            get => GetValue(CompletedStepLineWidthProperty);
            set => SetValue(CompletedStepLineWidthProperty, value);
        }

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }


        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var bounds = new Rect(0, 0, Width, Height);
            var centerX = bounds.X + bounds.Width / 2;
            var centerY = bounds.Y + bounds.Height / 2;

            var lineAvailableWidth = bounds.X + bounds.Width - StepEllipseRadius * 2;
            var lineWidth = StepsCount > 1 ? lineAvailableWidth / (StepsCount - 1) : 0;
            var startX = StepsCount > 1 ? bounds.X + StepEllipseRadius : centerX;

            // draw background
            context.DrawRectangle(Background, null, bounds);

            // draw background line
            if (StepsCount > 1)
                context.DrawLine(
                    pen: new Pen(StepBackground, StepLineWidth),
                    new Point(bounds.X + StepEllipseRadius, centerY),
                    new Point(bounds.Right - StepEllipseRadius, centerY));

            // draw steps
            for (var i = 0; i < StepsCount; ++i)
            {
                var stepPoint = new Point(startX + lineWidth * i, centerY);

                context.DrawGeometry(
                    brush: StepBackground,
                    pen: null,
                    new EllipseGeometry(
                        new Rect(stepPoint.X - StepEllipseRadius, stepPoint.Y - StepEllipseRadius,
                            StepEllipseRadius * 2, StepEllipseRadius * 2)
                    )
                );
            }

            // draw completed steps
            for (var i = 0; i <= CurrentStep && i < StepsCount; ++i)
            {
                var stepPoint = new Point(startX + lineWidth * i, centerY);
                var prevStepPoint = new Point(startX + lineWidth * (i > 0 ? i - 1 : 0), centerY);

                context.DrawGeometry(
                    brush: CompletedStepBackground,
                    pen: null,
                    new EllipseGeometry(
                        new Rect(stepPoint.X - CompletedStepEllipseRadius, stepPoint.Y - CompletedStepEllipseRadius,
                            CompletedStepEllipseRadius * 2, CompletedStepEllipseRadius * 2)
                    )
                );

                context.DrawLine(
                    pen: new Pen(CompletedStepBackground, CompletedStepLineWidth),
                    prevStepPoint,
                    stepPoint);
            }

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight);

            // draw labels
            for (var i = 0; i < StepsCount; ++i)
            {
                FormattedText stepText = new FormattedText()
                {
                    Typeface = typeface,
                    Text = (i + 1).ToString(),
                    FontSize = FontSize,
                    TextAlignment = TextAlignment.Center,
                    Constraint = new Size(CompletedStepEllipseRadius * 2, CompletedStepEllipseRadius * 2)
                };

                var stepPoint = new Point(startX + lineWidth * i - stepText.Constraint.Width / 2,
                    centerY - stepText.Constraint.Height / 2 + (StepEllipseRadius - CompletedStepEllipseRadius));

                var drawBrush = Brushes.White;

                context.DrawText(drawBrush, stepPoint, stepText);
            }
        }
    }
}