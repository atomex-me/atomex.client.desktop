using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class ConnectDappViewModel : ViewModelBase, IDisposable
    {
        public Func<string, Task> OnConnect;
        [Reactive] public string? QrCodeString { get; set; }
        [ObservableAsProperty] public bool IsSending { get; }

        public ConnectDappViewModel()
        {
            ConnectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);
        }

        private ReactiveCommand<Unit, Unit>? _connectCommand;

        public ReactiveCommand<Unit, Unit> ConnectCommand => _connectCommand ??= _connectCommand =
            ReactiveCommand.CreateFromTask(async () => { await OnConnect(QrCodeString!); });

        public void Dispose()
        {
            QrCodeString = null;
        }
    }
}