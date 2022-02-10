using Serilog;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using QRCoder;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Core;
using Atomex.ViewModels;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ReceiveViewModel : ViewModelBase
    {
        private const int PixelsPerModule = 20;

        [Reactive] public CurrencyConfig Currency { get; set; }
        [Reactive] public WalletAddressViewModel SelectedAddress { get; set; }
        [Reactive] public bool IsCopied { get; set; }
        [Reactive] public IBitmap QrCode { get; private set; }
        public string TokenContract { get; private set; }
        public string TokenType { get; private set; }
        public string TitleText => $"Your receiving {Currency.Name} address";

        public SelectAddressViewModel SelectAddressViewModel { get; set; }

        public ReceiveViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public ReceiveViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            string tokenContract = null,
            string tokenType = null)
        {
            var createQrCodeCommand = ReactiveCommand.CreateFromTask(CreateQrCodeAsync);

            this.WhenAnyValue(vm => vm.SelectedAddress)
                .WhereNotNull()
                .Select(_ => Unit.Default)
                .InvokeCommand(createQrCodeCommand);

            TokenContract = tokenContract;
            TokenType = tokenType;
            Currency = currency;

            SelectAddressViewModel = new SelectAddressViewModel(app.Account, Currency)
            {
                BackAction = () => { App.DialogService.Show(this); },
                ConfirmAction = walletAddressViewModel =>
                {
                    if (!string.IsNullOrEmpty(walletAddressViewModel.Address))
                        SelectedAddress = walletAddressViewModel;

                    App.DialogService.Show(this);
                }
            };

            SelectedAddress = SelectAddressViewModel.SelectDefaultAddress()!;
        }

        private async Task CreateQrCodeAsync()
        {
            System.Drawing.Bitmap? qrCodeBitmap = null;

            await Task.Run(() =>
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(SelectedAddress.Address, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrData);
                qrCodeBitmap = qrCode.GetGraphic(PixelsPerModule, Color.White, Color.FromArgb(0, 0, 0, 0), false);
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                using (MemoryStream memory = new())
                {
                    qrCodeBitmap?.Save(memory, ImageFormat.Png);
                    memory.Position = 0;

                    QrCode = new Bitmap(memory);
                }

                qrCodeBitmap?.Dispose();
            });
        }

        private ReactiveCommand<Unit, Unit> _copyCommand;

        public ReactiveCommand<Unit, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                _ = App.Clipboard.SetTextAsync(SelectedAddress.Address);
                IsCopied = true;
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
            _selectAddressCommand ??= ReactiveCommand.Create(() => { App.DialogService.Show(SelectAddressViewModel); });

        private void DesignerMode()
        {
            Currency = DesignTime.Currencies.First();

            SelectedAddress = new WalletAddressViewModel
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
            };
        }
    }
}