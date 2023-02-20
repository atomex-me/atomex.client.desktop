using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class ConnectDappViewModel : ViewModelBase
    {
        public Action OnBack;
        public Func<string, Task> OnConnect;
        [Reactive] public string? AddressToConnect { get; set; }
        [Reactive] public string? QrCodeString { get; set; }
        [ObservableAsProperty] public bool IsSending { get; }

        public ConnectDappViewModel()
        {
            ConnectCommand.Merge(BackCommand)
                .SubscribeInMainThread(_ =>
                {
                    QrCodeString = null;
                });
            
            ConnectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);
        }

        private ReactiveCommand<Unit, Unit>? _backCommand;
        public ReactiveCommand<Unit, Unit> BackCommand =>
            _backCommand ??= _backCommand = ReactiveCommand.Create(() => { OnBack?.Invoke(); });

        private ReactiveCommand<Unit, Unit>? _connectCommand;
        public ReactiveCommand<Unit, Unit> ConnectCommand =>
            _connectCommand ??= _connectCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (QrCodeString != null && AddressToConnect != null)
                    await OnConnect(QrCodeString);
            });

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