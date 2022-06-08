using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace Atomex.Client.Desktop.Controls
{
    public class WalletCurrency : UserControl
    {
        static WalletCurrency()
        {
            AffectsRender<WalletCurrency>(
                IconPathProperty,
                TokenPreviewProperty,
                TotalAmountProperty,
                TotalAmountInBaseProperty,
                DailyChangePercentProperty,
                CurrentQuoteProperty,
                CurrencyFormatProperty,
                CurrencyCodeProperty,
                CurrencyDescriptionProperty,
                BaseCurrencyFormatProperty,
                IsBalanceUpdatingProperty,
                CanExchangeProperty
            );

            // var baseLight = (IStyle)AvaloniaXamlLoader.Load(
            //     new Uri("avares://Avalonia.Themes.Default/Accents/BaseLight.xaml"));
        }

        // public WalletCurrency()
        // {
        //     if (Design.IsDesignMode)
        //     {
        //         Design.SetDesignStyle(this,
        //             new Style(x => x.OfType<WalletCurrency>())
        //             {
        //                 Setters =
        //                 {
        //                     new Setter(BackgroundProperty, new SolidColorBrush(new Color(0, 0xFF, 0, 0)))
        //                 }
        //             });
        //     }
        // }


        public static readonly DirectProperty<WalletCurrency, ICommand> UpdateCommandProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, ICommand>(nameof(UpdateCommand),
                control => control.UpdateCommand, (control, command) => control.UpdateCommand = command);

        private ICommand _updateCommand;

        public ICommand UpdateCommand
        {
            get => _updateCommand;
            set => SetAndRaise(UpdateCommandProperty, ref _updateCommand, value);
        }


        public static readonly DirectProperty<WalletCurrency, ICommand> SendCommandProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, ICommand>(nameof(SendCommand),
                control => control.SendCommand, (control, command) => control.SendCommand = command);

        private ICommand _sendCommand;

        public ICommand SendCommand
        {
            get => _sendCommand;
            set => SetAndRaise(SendCommandProperty, ref _sendCommand, value);
        }


        public static readonly DirectProperty<WalletCurrency, ICommand> ReceiveCommandProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, ICommand>(nameof(ReceiveCommand),
                control => control.ReceiveCommand, (control, command) => control.ReceiveCommand = command);

        private ICommand _receiveCommand;

        public ICommand ReceiveCommand
        {
            get => _receiveCommand;
            set => SetAndRaise(ReceiveCommandProperty, ref _receiveCommand, value);
        }

        public static readonly DirectProperty<WalletCurrency, ICommand> ExchangeCommandProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, ICommand>(nameof(ExchangeCommand),
                control => control.ExchangeCommand, (control, command) => control.ExchangeCommand = command);

        private ICommand _exchangeCommand;

        public ICommand ExchangeCommand
        {
            get => _exchangeCommand;
            set => SetAndRaise(ExchangeCommandProperty, ref _exchangeCommand, value);
        }


        public static readonly DirectProperty<WalletCurrency, ICommand> BuyCommandProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, ICommand>(nameof(BuyCommand),
                control => control.BuyCommand, (control, command) => control.BuyCommand = command);

        private ICommand _buyCommand;

        public ICommand BuyCommand
        {
            get => _buyCommand;
            set => SetAndRaise(BuyCommandProperty, ref _buyCommand, value);
        }

        public static readonly StyledProperty<bool> IsBalanceUpdatingProperty =
            AvaloniaProperty.Register<WalletCurrency, bool>(nameof(IsBalanceUpdating));

        public bool IsBalanceUpdating
        {
            get => GetValue(IsBalanceUpdatingProperty);
            set => SetValue(IsBalanceUpdatingProperty, value);
        }

        public static readonly DirectProperty<WalletCurrency, bool?> CanExchangeProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, bool?>(
                nameof(CanExchange),
                control => control.CanExchange,
                (control, value) => control.CanExchange = value);

        private bool? _canExchangeCommand;

        public bool? CanExchange
        {
            get => _canExchangeCommand;
            set => SetAndRaise(CanExchangeProperty, ref _canExchangeCommand, value);
        }

        public static readonly DirectProperty<WalletCurrency, string?> IconPathProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, string?>(
                nameof(IconPath),
                o => o.IconPath,
                (o, v) => o.IconPath = v);

        private string? _iconPath;

        public string? IconPath
        {
            get => _iconPath;
            set => SetAndRaise(IconPathProperty, ref _iconPath, value);
        }

        public static readonly DirectProperty<WalletCurrency, IBitmap?> TokenPreviewProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, IBitmap?>(
                nameof(TokenPreview),
                o => o.TokenPreview,
                (o, v) => o.TokenPreview = v);

        private IBitmap? _tokenPreview;

        public IBitmap? TokenPreview
        {
            get => _tokenPreview;
            set => SetAndRaise(TokenPreviewProperty, ref _tokenPreview, value);
        }

        public static readonly DirectProperty<WalletCurrency, decimal> TotalAmountProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, decimal>(
                nameof(TotalAmount),
                o => o.TotalAmount,
                (o, v) => o.TotalAmount = v);

        private decimal _totalAmount;

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetAndRaise(TotalAmountProperty, ref _totalAmount, value);
        }

        public static readonly DirectProperty<WalletCurrency, decimal> TotalAmountInBaseProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, decimal>(
                nameof(TotalAmountInBase),
                o => o.TotalAmountInBase,
                (o, v) => o.TotalAmountInBase = v);

        private decimal _totalAmountInBase;

        public decimal TotalAmountInBase
        {
            get => _totalAmountInBase;
            set => SetAndRaise(TotalAmountInBaseProperty, ref _totalAmountInBase, value);
        }

        public static readonly DirectProperty<WalletCurrency, decimal?> DailyChangePercentProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, decimal?>(
                nameof(DailyChangePercent),
                o => o.DailyChangePercent,
                (o, v) => o.DailyChangePercent = v);

        private decimal? _dailyChangePercent;

        public decimal? DailyChangePercent
        {
            get => _dailyChangePercent;
            set => SetAndRaise(DailyChangePercentProperty, ref _dailyChangePercent, value);
        }

        public static readonly DirectProperty<WalletCurrency, decimal> CurrentQuoteProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, decimal>(
                nameof(CurrentQuote),
                o => o.CurrentQuote,
                (o, v) => o.CurrentQuote = v);

        private decimal _currentQuote;

        public decimal CurrentQuote
        {
            get => _currentQuote;
            set => SetAndRaise(CurrentQuoteProperty, ref _currentQuote, value);
        }

        public static readonly DirectProperty<WalletCurrency, string> CurrencyFormatProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, string>(
                nameof(CurrencyFormat),
                o => o.CurrencyFormat,
                (o, v) => o.CurrencyFormat = v);

        private string _currencyFormat;

        public string CurrencyFormat
        {
            get => _currencyFormat;
            set => SetAndRaise(CurrencyFormatProperty, ref _currencyFormat, value);
        }

        public static readonly DirectProperty<WalletCurrency, string> CurrencyCodeProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, string>(
                nameof(CurrencyCode),
                o => o.CurrencyCode,
                (o, v) => o.CurrencyCode = v);

        private string _currencyCode;

        public string CurrencyCode
        {
            get => _currencyCode;
            set => SetAndRaise(CurrencyCodeProperty, ref _currencyCode, value);
        }

        public static readonly DirectProperty<WalletCurrency, string> CurrencyDescriptionProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, string>(
                nameof(CurrencyDescription),
                o => o.CurrencyDescription,
                (o, v) => o.CurrencyDescription = v);

        private string _currencyDescription;

        public string CurrencyDescription
        {
            get => _currencyDescription;
            set => SetAndRaise(CurrencyDescriptionProperty, ref _currencyDescription, value);
        }

        public static readonly DirectProperty<WalletCurrency, string> BaseCurrencyFormatProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, string>(
                nameof(BaseCurrencyFormat),
                o => o.BaseCurrencyFormat,
                (o, v) => o.BaseCurrencyFormat = v);

        private string _baseCurrencyFormat;

        public string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set => SetAndRaise(BaseCurrencyFormatProperty, ref _baseCurrencyFormat, value);
        }
    }
}