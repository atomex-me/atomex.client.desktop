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
                CurrencyViewModelProperty,
                IsBalanceUpdatingProperty
            );
        }
        
        public static readonly DirectProperty<WalletCurrency, CurrencyViewModel> CurrencyViewModelProperty =
            AvaloniaProperty.RegisterDirect<WalletCurrency, CurrencyViewModel>(
                nameof(CurrencyViewModel),
                o => o.CurrencyViewModel,
                (o, v) => o.CurrencyViewModel = v);

        private CurrencyViewModel _currencyViewModel;
        public CurrencyViewModel CurrencyViewModel
        {
            get { return _currencyViewModel; }
            set { SetAndRaise(CurrencyViewModelProperty, ref _currencyViewModel, value); }
        }
        

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
    }
}