using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia.Controls;
using Beacon.Sdk.Beacon.Permission;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Dialogs;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class PermissionRequestViewModel : ViewModelBase, IDialogViewModel
    {
        public SelectAddressViewModel SelectAddressViewModel { get; }
        public string DappName { get; set; }
        public List<PermissionScope> Permissions { get; set; }
        public List<string> PermissionStrings => BeaconHelper.GetPermissionStrings(Permissions);

        public string SubTitle =>
            $"{DappName} wants to connect to your account. The app is requesting the following permissions:";

        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }
        public Action? OnClose { get; set; }

        public PermissionRequestViewModel(
            IAccount account,
            ILocalStorage localStorage,
            TezosConfig tezos,
            Action onClose)
        {
            OnAllowCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);

            SelectAddressViewModel = new SelectAddressViewModel(account, localStorage, tezos, SelectAddressMode.Connect)
            {
                BackAction = () => { App.DialogService.Show(this); },
                ConfirmAction = _ => { App.DialogService.Show(this); },
                OnClose = onClose
            };

            OnClose = onClose;
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