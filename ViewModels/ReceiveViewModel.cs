using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using QRCoder;
using ReactiveUI;
using Serilog;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using ReactiveUI.Fody.Helpers;
using Color = System.Drawing.Color;

namespace Atomex.Client.Desktop.ViewModels.ReceiveViewModels
{
    public class ReceiveViewModel : ViewModelBase
    {
        private const int PixelsPerModule = 20;

        private readonly IAtomexApp _app;

        protected CurrencyConfig _currency;

        public CurrencyConfig Currency
        {
            get => _currency;
            set
            {
                _currency = value;
                OnPropertyChanged(nameof(Currency));
#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    // tokenContract: TokenContract
                    FromAddressList = AddressesHelper
                        .GetReceivingAddressesAsync(
                            account: _app.Account,
                            currency: _currency)
                        .WaitForResult()
                        .ToList();
#if DEBUG
                }
#endif
            }
        }

        public CurrencyViewModel CurrencyViewModel { get; set; }

        private List<WalletAddressViewModel> _fromAddressList;

        public List<WalletAddressViewModel> FromAddressList
        {
            get => _fromAddressList;
            protected set
            {
                _fromAddressList = value;
                OnPropertyChanged(nameof(FromAddressList));

                SelectedAddress = GetDefaultAddress();

                SelectedAddressIndex = _fromAddressList.FindIndex(wa => wa.Address == SelectedAddress);
                OnPropertyChanged(nameof(SelectedAddressIndex));
            }
        }

        private int _selectedAddressIndex;

        public int SelectedAddressIndex
        {
            get => _selectedAddressIndex;
            set
            {
                _selectedAddressIndex = value;
                OnPropertyChanged(nameof(SelectedAddressIndex));

                var walletAddressViewModel = FromAddressList.ElementAt(_selectedAddressIndex);
                SelectedAddress = walletAddressViewModel.Address;
                AddressBalance = walletAddressViewModel.AvailableBalance;
                AddressIsFree = walletAddressViewModel.IsFreeAddress;
            }
        }

        private string _selectedAddress;

        public string SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                _selectedAddress = value;
                OnPropertyChanged(nameof(SelectedAddress));

                if (_selectedAddress != null)
                    _ = CreateQrCodeAsync();

                Warning = string.Empty;
            }
        }

        [Reactive] public bool IsCopied { get; set; }
        [Reactive] public decimal AddressBalance { get; set; }
        [Reactive] public bool AddressIsFree { get; set; }

        private string _warning;

        public string Warning
        {
            get => _warning;
            set
            {
                _warning = value;
                OnPropertyChanged(nameof(Warning));
            }
        }

        public IBitmap QrCode { get; private set; }

        public string TokenContract { get; private set; }
        public string TokenType { get; private set; }

        public string TitleText => $"Your receiving {Currency.Name} address";

        public ReceiveViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public ReceiveViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            string tokenContract = null,
            string tokenType = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            TokenContract = tokenContract;
            TokenType = tokenType;
            Currency = currency;
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);
        }

        private async Task CreateQrCodeAsync()
        {
            System.Drawing.Bitmap? qrCodeBitmap = null;

            await Task.Run(() =>
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(_selectedAddress, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrData);
                qrCodeBitmap = qrCode.GetGraphic(PixelsPerModule, Color.White, Color.FromArgb(0, 0, 0, 1), false);
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    qrCodeBitmap?.Save(memory, ImageFormat.Png);
                    memory.Position = 0;

                    QrCode = new Bitmap(memory);
                    this.RaisePropertyChanged(nameof(QrCode));
                }

                qrCodeBitmap?.Dispose();
            });
        }

        protected virtual string GetDefaultAddress()
        {
            if (Currency is TezosConfig or EthereumConfig)
            {
                var activeAddressViewModel = FromAddressList
                    .Where(vm => vm.HasActivity && vm.AvailableBalance > 0)
                    .MaxByOrDefault(vm => vm.AvailableBalance);

                if (activeAddressViewModel != null)
                    return activeAddressViewModel.Address;
            }

            return FromAddressList.FirstOrDefault(vm => vm.IsFreeAddress)?.Address ?? FromAddressList.First().Address;
        }

        private ReactiveCommand<Unit, Unit> _copyCommand;

        public ReactiveCommand<Unit, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                IsCopied = true;
                _ = App.Clipboard.SetTextAsync(SelectedAddress);
                Warning = "Address successfully copied to clipboard.";

                await Task.Delay(TimeSpan.FromSeconds(5));
                IsCopied = false;
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

        private ReactiveCommand<Unit, Unit> _selectAddressCommand;

        public ReactiveCommand<Unit, Unit> SelectAddressCommand =>
            _selectAddressCommand ??= ReactiveCommand.Create(() => { });

        private void DesignerMode()
        {
            Currency = DesignTime.Currencies.First();
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(Currency, subscribeToUpdates: false);

            FromAddressList = new List<WalletAddressViewModel>
            {
                new WalletAddressViewModel
                {
                    Address = "tz3bvNMQ95vfAYtG8193ymshqjSvmxiCUuR5",
                    HasActivity = true,
                    AvailableBalance = 123.456789m,
                    CurrencyFormat = Currency.Format,
                    CurrencyCode = Currency.Name,
                    IsFreeAddress = false,
                    ShowTokenBalance = true,
                    TokenBalance = 100.00000000m,
                    TokenFormat = "F8",
                    TokenCode = "HEH"
                },
                new WalletAddressViewModel
                {
                    Address = "tz1bvntqQ43vfAYtG1233ymshqjsvmxiCUuR1",
                    HasActivity = true,
                    AvailableBalance = 0m,
                    CurrencyFormat = Currency.Format,
                    CurrencyCode = Currency.Name,
                    IsFreeAddress = true,
                    ShowTokenBalance = false,
                    TokenBalance = 0m,
                    TokenFormat = "F8",
                    TokenCode = "HEH"
                }
            };
        }
    }
}