using System.Windows.Input;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Avalonia;
using Avalonia.Controls;


namespace Atomex.Client.Desktop.Controls
{
    public class WalletCurrency : UserControl
    {
        static WalletCurrency()
        {
            AffectsRender<WalletCurrency>(
                AssetViewModelProperty,
                IsBalanceUpdatingProperty
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

        public static readonly DirectProperty<WalletCurrency, bool> IsBalanceUpdatingProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, bool>(nameof(IsBalanceUpdating),
                control => control.IsBalanceUpdating, (control, value) => control.IsBalanceUpdating = value);

        private bool _IsBalanceUpdating;

        public bool IsBalanceUpdating
        {
            get => _IsBalanceUpdating;
            set => SetAndRaise(IsBalanceUpdatingProperty, ref _IsBalanceUpdating, value);
        }

        public static readonly DirectProperty<WalletCurrency, IAssetViewModel> AssetViewModelProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, IAssetViewModel>(
                nameof(AssetViewModel),
                o => o.AssetViewModel,
                (o, v) => o.AssetViewModel = v);

        private IAssetViewModel _assetViewModel;

        public IAssetViewModel AssetViewModel
        {
            get => _assetViewModel;
            set => SetAndRaise(AssetViewModelProperty, ref _assetViewModel, value);
        }
    }
}