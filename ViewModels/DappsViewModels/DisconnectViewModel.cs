using System;
using System.Reactive;
using System.Threading.Tasks;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class DisconnectViewModel : ViewModelBase
    {
        public string DappName { get; set; }
        public string SubTitle => $"Are you sure you want to disconnect {DappName}?";
        public Func<Task> OnDisconnect { get; set; }
        [ObservableAsProperty] public bool IsDisconnecting { get; }

        public DisconnectViewModel()
        {
            OnDisconnectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsDisconnecting);
        }

        private ReactiveCommand<Unit, Unit>? _onDisconnectCommand;
        public ReactiveCommand<Unit, Unit> OnDisconnectCommand =>
            _onDisconnectCommand ??= ReactiveCommand.CreateFromTask(async () =>
            {
                await OnDisconnect();
                App.DialogService.Close();
            });

        private ReactiveCommand<Unit, Unit>? _onCancelCommand;
        public ReactiveCommand<Unit, Unit> OnCancelCommand =>
            _onCancelCommand ??= ReactiveCommand.Create(() => { App.DialogService.Close(); });
    }
}