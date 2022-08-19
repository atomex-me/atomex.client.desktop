using System;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class ConnectDappViewModel : ViewModelBase
    {
        public Action OnBack;
        public Action<string> OnConnect;
        [Reactive] public string? AddressToConnect { get; set; }
        [Reactive] public string QrCodeString { get; set; }

        private ReactiveCommand<Unit, Unit>? _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand =>
            _backCommand ??= _backCommand = ReactiveCommand.Create(() => { OnBack?.Invoke(); });

        private ReactiveCommand<Unit, Unit>? _connectCommand;

        public ReactiveCommand<Unit, Unit> ConnectCommand =>
            _connectCommand ??= _connectCommand = ReactiveCommand.Create(() => { OnConnect?.Invoke(QrCodeString); });

        private ReactiveCommand<string, Unit>? _copyCommand;

        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>(data =>
        {
            try
            {
                App.Clipboard.SetTextAsync(data);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });
    }
}