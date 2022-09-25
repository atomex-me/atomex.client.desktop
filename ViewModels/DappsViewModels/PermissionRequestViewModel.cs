using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using Avalonia.Controls;
using Beacon.Sdk.Beacon.Permission;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class PermissionRequestViewModel : ViewModelBase
    {
        public string DappName { get; set; }
        public string Address { get; set; }
        public List<PermissionScope> Permissions { get; set; }
        public List<string> PermissionStrings => BeaconHelper.GetPermissionStrings(Permissions);
        
        public string SubTitle =>
            $"{DappName} wants to connect to your account. The app is requesting the following permissions:";
        
        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }
        
        public PermissionRequestViewModel()
        {
            OnAllowCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);
            
            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Func<Task> OnAllow { get; set; }
        public Func<Task> OnReject { get; set; }

        private ReactiveCommand<Unit, Unit>? _onAllowCommand;

        public ReactiveCommand<Unit, Unit> OnAllowCommand =>
            _onAllowCommand ??= ReactiveCommand.CreateFromTask(async () => await OnAllow());

        private ReactiveCommand<Unit, Unit>? _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());

#if DEBUG
        private void DesignerMode()
        {
            DappName = "objkt.com";
            Address = "tz1Mrt2GJcKBCAWdwWK6mRwhpqt9XGGH6tLb";
            Permissions = new List<PermissionScope>
            {
                PermissionScope.sign,
                PermissionScope.operation_request,
            };
        }
#endif
    }
}