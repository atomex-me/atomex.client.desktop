using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Atomex.Client.Desktop.Controls
{
    public class PasswordStrengthMeter : UserControl
    {
        static PasswordStrengthMeter()
        {
            AffectsRender<PasswordStrengthMeter>(
                BackgroundProperty,
                ForegroundProperty,
                BlankBackgroundProperty,
                TooShortBackgroundProperty,
                WeakBackgroundProperty,
                MediumBackgroundProperty,
                StrongBackgroundProperty,
                VeryStrongBackgroundProperty,
                PasswordScoreProperty,
                CornerRadiusProperty,
                ShowCaptionProperty,
                FontFamilyProperty,
                FontSizeProperty,
                FontStyleProperty,
                FontWeightProperty
            );
        }

        public const int MaxPasswordScore = 5;
        public static Color WeakColor = Color.FromRgb(0xd9, 0x53, 0x4f);
        public static Color MediumColor = Color.FromRgb(0xf0, 0xad, 0x4e);
        public static Color StrongColor = Color.FromRgb(0x5c, 0xb8, 0x5c);

        public static readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, IBrush>(nameof(Background), Brushes.Transparent);

        public static readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, IBrush>(nameof(Foreground), Brushes.White);

        public static readonly StyledProperty<IBrush> BlankBackgroundProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, IBrush>(nameof(BlankBackground), Brushes.DarkGray);

        public static readonly StyledProperty<IBrush> TooShortBackgroundProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, IBrush>("TooShortBackground",
                new SolidColorBrush(WeakColor));

        public static readonly StyledProperty<IBrush> WeakBackgroundProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, IBrush>("WeakBackground", new SolidColorBrush(WeakColor));

        public static readonly StyledProperty<IBrush> MediumBackgroundProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, IBrush>("MediumBackground",
                new SolidColorBrush(MediumColor));

        public static readonly StyledProperty<IBrush> StrongBackgroundProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, IBrush>("StrongBackground",
                new SolidColorBrush(StrongColor));

        public static readonly StyledProperty<IBrush> VeryStrongBackgroundProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, IBrush>("VeryStrongBackground",
                new SolidColorBrush(StrongColor));

        public static readonly StyledProperty<int> PasswordScoreProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, int>("PasswordScore", 0);

        public static readonly StyledProperty<double> CornerRadiusProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, double>("CornerRadius", 5.0);

        public static readonly StyledProperty<bool> ShowCaptionProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, bool>("ShowCaption", true);

        // public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        //     AvaloniaProperty.Register<PasswordStrengthMeter, FontFamily>(nameof(FontFamily),
        //         new FontFamily("avares://Atomex.Client.Desktop/Resources/Fonts#Roboto"));

        public static readonly StyledProperty<double> FontSizeProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, double>(nameof(FontSize), 11.0);

        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, FontStyle>(nameof(FontStyle), FontStyle.Normal);

        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.Register<PasswordStrengthMeter, FontWeight>(nameof(FontWeight), FontWeight.Normal);


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

        public IBrush BlankBackground
        {
            get => GetValue(BlankBackgroundProperty);
            set => SetValue(BlankBackgroundProperty, value);
        }

        public IBrush TooShortBackground
        {
            get => GetValue(TooShortBackgroundProperty);
            set => SetValue(TooShortBackgroundProperty, value);
        }

        public IBrush WeakBackground
        {
            get => GetValue(WeakBackgroundProperty);
            set => SetValue(WeakBackgroundProperty, value);
        }

        public IBrush MediumBackground
        {
            get => GetValue(MediumBackgroundProperty);
            set => SetValue(MediumBackgroundProperty, value);
        }

        public IBrush StrongBackground
        {
            get => GetValue(StrongBackgroundProperty);
            set => SetValue(StrongBackgroundProperty, value);
        }

        public IBrush VeryStrongBackground
        {
            get => GetValue(VeryStrongBackgroundProperty);
            set => SetValue(VeryStrongBackgroundProperty, value);
        }

        public int PasswordScore
        {
            get => GetValue(PasswordScoreProperty);
            set => SetValue(PasswordScoreProperty, value);
        }

        public double CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public bool ShowCaption
        {
            get => GetValue(ShowCaptionProperty);
            set => SetValue(ShowCaptionProperty, value);
        }

        // public FontFamily FontFamily
        // {
        //     get => GetValue(FontFamilyProperty);
        //     set => SetValue(FontFamilyProperty, value);
        // }

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        // public FontStyle FontStyle
        // {
        //     get => GetValue(FontStyleProperty);
        //     set => SetValue(FontStyleProperty, value);
        // }
        //
        // public FontWeight FontWeight
        // {
        //     get => GetValue(FontWeightProperty);
        //     set => SetValue(FontWeightProperty, value);
        // }


        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var bounds = new Rect(0, 0, Width, Height);

            context.DrawRectangle(Background, null, bounds);
            context.DrawRectangle(BlankBackground, null, bounds, CornerRadius, CornerRadius);

            var passwordScore = PasswordScore >= 0
                ? PasswordScore < MaxPasswordScore
                    ? PasswordScore
                    : MaxPasswordScore
                : 0;

            context.DrawRectangle(
                brush: ScoreToBrush(passwordScore),
                pen: null,
                rect: new Rect(0, 0, passwordScore * Width / 5, Height),
                radiusX: CornerRadius,
                radiusY: CornerRadius
            );

            if (ShowCaption)
            {
                FormattedText formattedText = new FormattedText()
                {
                    Typeface = new Typeface(FontFamily, FontStyle, FontWeight),
                    Text = ScoreToString(passwordScore),
                    FontSize = FontSize,
                    TextAlignment = TextAlignment.Center,
                    
                    Constraint = new Size(50, FontSize + 2)
                };

                context.DrawText(
                    text: formattedText,
                    origin: new Point(bounds.X + bounds.Width / 2 - formattedText.Constraint.Width / 2,
                        bounds.Y + bounds.Height / 2 - formattedText.Constraint.Height / 2),
                    foreground: Foreground
                );
            }
        }

        private IBrush ScoreToBrush(int passwordScore)
        {
            if (passwordScore <= 0)
                return BlankBackground;
            if (passwordScore == 1)
                return TooShortBackground;
            if (passwordScore == 2)
                return WeakBackground;
            if (passwordScore == 3)
                return MediumBackground;
            if (passwordScore == 4)
                return StrongBackground;

            return VeryStrongBackground;
        }

        // todo: localization
        private static string ScoreToString(int passwordScore)
        {
            switch (passwordScore)
            {
                case 0:
                    return "Blank";
                case 1:
                    return "Too Short";
                case 2:
                    return "Weak";
                case 3:
                    return "Medium";
                case 4:
                    return "Strong";
                case 5:
                    return "Very Strong";
                default:
                    return "Blank";
            }
        }
    }
}