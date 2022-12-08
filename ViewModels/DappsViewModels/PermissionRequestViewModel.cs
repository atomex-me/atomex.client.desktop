using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using Avalonia.Controls;
using Beacon.Sdk.Beacon.Permission;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class PermissionRequestViewModel : ViewModelBase
    {
        public SelectAddressViewModel SelectAddressViewModel { get; }
        public string DappName { get; set; }
        public List<PermissionScope> Permissions { get; set; }
        public List<string> PermissionStrings => BeaconHelper.GetPermissionStrings(Permissions);

        public string SubTitle =>
            $"{DappName} wants to connect to your account. The app is requesting the following permissions:";

        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        public PermissionRequestViewModel(IAccount account, TezosConfig tezos)
        {
            OnAllowCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);

            SelectAddressViewModel = new SelectAddressViewModel(account, tezos, SelectAddressMode.Connect)
            {
                BackAction = () => { App.DialogService.Show(this); },
                ConfirmAction = _ => { App.DialogService.Show(this); }
            };

            var a = 5;
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Func<WalletAddressViewModel, Task> OnAllow { get; init; }
        public Func<Task> OnReject { get; init; }

        private ReactiveCommand<Unit, Unit>? _onAllowCommand;

        public ReactiveCommand<Unit, Unit> OnAllowCommand =>
            _onAllowCommand ??=
                ReactiveCommand.CreateFromTask(async () => await OnAllow(SelectAddressViewModel.SelectedAddress!));


        private ReactiveCommand<Unit, Unit>? _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());


        private ReactiveCommand<Unit, Unit>? _selectAddressCommand;

        public ReactiveCommand<Unit, Unit> SelectAddressCommand => _selectAddressCommand ??=
            ReactiveCommand.Create(() => { App.DialogService.Show(SelectAddressViewModel); });

#if DEBUG
        private void DesignerMode()
        {
            DappName = "objkt.com";
            Permissions = new List<PermissionScope>
            {
                PermissionScope.sign,
                PermissionScope.operation_request,
            };
        }
#endif
    }
}