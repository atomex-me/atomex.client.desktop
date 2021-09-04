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
using Atomex.Wallet.Abstract;
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
                    // get all addresses with tokens (if exists)
                    var tokenAddresses = Currencies.HasTokens(_currency.Name)
                        ? _app.Account
                            .GetCurrencyAccount<ILegacyCurrencyAccount>(_currency.Name)
                            .GetUnspentTokenAddressesAsync()
                            .WaitForResult()
                        : new List<WalletAddress>();

                    // get all active addresses
                    var activeAddresses = _app.Account
                        .GetUnspentAddressesAsync(_currency.Name)
                        .WaitForResult()
                        .ToList();

                    // get free external address
                    var freeAddress = _app.Account
                        .GetFreeExternalAddressAsync(_currency.Name)
                        .WaitForResult();

                    FromAddressList = activeAddresses
                        .Concat(tokenAddresses)
                        .Concat(new WalletAddress[] { freeAddress })
                        .GroupBy(w => w.Address)
                        .Select(g =>
                        {
                            // main address
                            var address = g.FirstOrDefault(w => w.Currency == _currency.Name);

                            var isFreeAddress = address?.Address == freeAddress.Address;

                            var hasTokens = g.Any(w => w.Currency != _currency.Name);

                            var tokenAddresses = TokenContract != null
                                ? g.Where(w => w.TokenBalance?.Contract == TokenContract)
                                : Enumerable.Empty<WalletAddress>();

                            var hasSeveralTokens = tokenAddresses.Count() > 1;
                            
                            var tokenAddress = tokenAddresses.FirstOrDefault();

                            var tokenBalance = hasSeveralTokens
                                ? tokenAddresses.Count()
                                : tokenAddress?.Balance ?? 0m;
       
                            var showTokenBalance = hasSeveralTokens
                                ? tokenBalance != 0
                                : TokenContract != null && tokenAddress?.TokenBalance?.Symbol != null;

                            var tokenCode = hasSeveralTokens
                                ? "TOKENS"
                                : tokenAddress?.TokenBalance?.Symbol ?? "";

                            return new WalletAddressViewModel
                            {
                                Address          = g.Key,
                                HasActivity      = address?.HasActivity ?? hasTokens,
                                AvailableBalance = address?.AvailableBalance() ?? 0m,
                                CurrencyFormat   = _currency.Format,
                                CurrencyCode     = _currency.Name,
                                IsFreeAddress    = isFreeAddress,
                                ShowTokenBalance = showTokenBalance,
                                TokenBalance     = tokenBalance,
                                TokenFormat      = "F8",
                                TokenCode        = tokenCode
                            };
                        })
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
                
                _selectedAddressIndex = _fromAddressList.FindIndex(wa => wa.Address == SelectedAddress);
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

                SelectedAddress = FromAddressList.ElementAt(_selectedAddressIndex).Address;
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
                   _ =  CreateQrCodeAsync();

                Warning = string.Empty;
            }
        }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        public IBitmap QrCode { get; private set; }

        public string TokenContract { get; private set; }

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
            string tokenContract = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            TokenContract = tokenContract;
            Currency = currency;
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);
        }

        private async Task CreateQrCodeAsync()
        {
            System.Drawing.Bitmap qrCodeBitmap = null;

            await Task.Run(() =>
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(_selectedAddress, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrData);
                qrCodeBitmap = qrCode.GetGraphic(PixelsPerModule);
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
            if (Currency is TezosConfig || Currency is EthereumConfig)
            {
                var activeAddressViewModel = FromAddressList
                    .Where(vm => vm.HasActivity && vm.AvailableBalance > 0)
                    .MaxByOrDefault(vm => vm.AvailableBalance);

                if (activeAddressViewModel != null)
                    return activeAddressViewModel.Address;
            }

            return FromAddressList.First(vm => vm.IsFreeAddress).Address;
        }

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                App.Clipboard.SetTextAsync(SelectedAddress);

                Warning = "Address successfully copied to clipboard.";
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

        private void DesignerMode()
        {
            Currency = DesignTime.Currencies.First();
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(Currency, subscribeToUpdates: false);

            FromAddressList = new List<WalletAddressViewModel>
            {
                new WalletAddressViewModel
                {
                    Address          = "tz3bvNMQ95vfAYtG8193ymshqjSvmxiCUuR5",
                    HasActivity      = true,
                    AvailableBalance = 123.456789m,
                    CurrencyFormat   = Currency.Format,
                    CurrencyCode     = Currency.Name,
                    IsFreeAddress    = false,
                    ShowTokenBalance = true,
                    TokenBalance     = 100.00000000m,
                    TokenFormat      = "F8",
                    TokenCode        = "HEH"
                },
                new WalletAddressViewModel
                {
                    Address          = "tz1bvntqQ43vfAYtG1233ymshqjsvmxiCUuR1",
                    HasActivity      = true,
                    AvailableBalance = 0m,
                    CurrencyFormat   = Currency.Format,
                    CurrencyCode     = Currency.Name,
                    IsFreeAddress    = true,
                    ShowTokenBalance = false,
                    TokenBalance     = 0m,
                    TokenFormat      = "F8",
                    TokenCode        = "HEH"
                }
            };
        }
    }
}