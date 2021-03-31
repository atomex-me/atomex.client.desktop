using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atomex.Core;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using QRCoder;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Input;
using Serilog;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels.ReceiveViewModels
{
    public class ReceiveViewModel : ViewModelBase
    {
        private const int PixelsPerModule = 20;

        protected IAtomexApp App { get; }

        private List<CurrencyViewModel> _fromCurrencies;

        public List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            set
            {
                _fromCurrencies = value;
                this.RaisePropertyChanged(nameof(FromCurrencies));
            }
        }


        private int _currencyIndex;

        public int CurrencyIndex
        {
            get => _currencyIndex;
            set
            {
                _currencyIndex = value;
                this.RaisePropertyChanged(nameof(CurrencyIndex));

                Currency = FromCurrencies.ElementAt(_currencyIndex).Currency;
            }
        }

        protected Currency _currency;

        public virtual Currency Currency
        {
            get => _currency;
            set
            {
                _currency = value;
#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    var activeAddresses = App.Account
                        .GetUnspentAddressesAsync(_currency.Name)
                        .WaitForResult();

                    var freeAddress = App.Account
                        .GetFreeExternalAddressAsync(_currency.Name)
                        .WaitForResult();

                    var receiveAddresses = activeAddresses
                        .Select(wa => new WalletAddressViewModel(wa, _currency.Format))
                        .ToList();

                    if (activeAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
                        receiveAddresses.AddEx(new WalletAddressViewModel(freeAddress, _currency.Format,
                            isFreeAddress: true));

                    FromAddressList = receiveAddresses;
#if DEBUG
                }
#endif
            }
        }

        private List<WalletAddressViewModel> _fromAddressList;

        public List<WalletAddressViewModel> FromAddressList
        {
            get => _fromAddressList;
            protected set
            {
                _fromAddressList = value;
                this.RaisePropertyChanged(nameof(FromAddressList));

                SelectedAddress = GetDefaultAddress();

                _selectedAddressIndex = _fromAddressList.FindIndex(fal => fal.WalletAddress == SelectedAddress);
                this.RaisePropertyChanged(nameof(SelectedAddressIndex));
            }
        }

        private int _selectedAddressIndex;

        public int SelectedAddressIndex
        {
            get => _selectedAddressIndex;
            set
            {
                _selectedAddressIndex = value;
                this.RaisePropertyChanged(nameof(SelectedAddressIndex));

                SelectedAddress = FromAddressList.ElementAt(_selectedAddressIndex).WalletAddress;
            }
        }

        private WalletAddress _selectedAddress;

        public WalletAddress SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                _selectedAddress = value;
                // this.RaisePropertyChanged(nameof(SelectedAddress));

                if (_selectedAddress != null)
                    _ = CreateQrCodeAsync();

                Warning = string.Empty;
            }
        }

        private string _warning;

        public string Warning
        {
            get => _warning;
            set
            {
                _warning = value;
                this.RaisePropertyChanged(nameof(Warning));
            }
        }

        public IBitmap QrCode { get; private set; }

        public ReceiveViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public ReceiveViewModel(IAtomexApp app)
            : this(app, null)
        {
        }

        public ReceiveViewModel(IAtomexApp app, Currency currency)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            FromCurrencies = app.Account.Currencies
                .Where(c => c.IsTransactionsAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            var currencyVM = FromCurrencies
                .FirstOrDefault(c => c.Currency.Name == currency.Name);

            CurrencyIndex = FromCurrencies.IndexOf(currencyVM);
            
            Console.WriteLine("Creating RECEIVE VM");
        }

        private async Task CreateQrCodeAsync()
        {
            System.Drawing.Bitmap qrCodeBitmap = null;

            await Task.Run(() =>
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(_selectedAddress.Address, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrData);
                qrCodeBitmap = qrCode.GetGraphic(PixelsPerModule);
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                using (MemoryStream memory = new())
                {
                    qrCodeBitmap?.Save(memory, ImageFormat.Png);
                    memory.Position = 0;

                    QrCode = new Bitmap(memory);
                    this.RaisePropertyChanged(nameof(QrCode));
                }

                qrCodeBitmap?.Dispose();
            });
        }

        public IBitmap GetBitmap(string uri)
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var bitmap = new Bitmap(assets.Open(new Uri(uri)));
            return bitmap;
        }

        protected virtual WalletAddress GetDefaultAddress()
        {
            if (Currency is Tezos || Currency is Ethereum)
            {
                var activeAddressViewModel = FromAddressList
                    .FirstOrDefault(vm => vm.WalletAddress.HasActivity && vm.WalletAddress.AvailableBalance() > 0);

                if (activeAddressViewModel != null)
                    return activeAddressViewModel.WalletAddress;
            }

            return FromAddressList.First(vm => vm.IsFreeAddress).WalletAddress;
        }

        private ICommand _copyCommand;

        public ICommand CopyCommand => _copyCommand ??= (_copyCommand = ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                var clipboard = AvaloniaLocator.Current.GetService<IClipboard>();
                clipboard?.SetTextAsync(SelectedAddress.Address);

                Warning = "Address successfully copied to clipboard.";
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        }));

        private void DesignerMode()
        {
            // FromCurrencies = DesignTime.Currencies
            //     .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
            //     .ToList();

            Currency = FromCurrencies.First().Currency;
            // Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM";
        }
    }
}