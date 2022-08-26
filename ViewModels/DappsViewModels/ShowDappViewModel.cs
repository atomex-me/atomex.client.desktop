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
    public class ShowDappViewModel : ViewModelBase
    {
        public string DappName { get; set; }
        public string DappId { get; set; }
        public string Address { get; set; }
        public List<PermissionScope> Permissions { get; set; }
        public List<string> PermissionStrings => BeaconHelper.GetPermissionStrings(Permissions);
        public Action OnDisconnect { get; set; }

        private ReactiveCommand<Unit, Unit>? _onDisconnectCommand;

        public ReactiveCommand<Unit, Unit> OnDisconnectCommand =>
            _onDisconnectCommand ??= ReactiveCommand.Create(() => OnDisconnect?.Invoke());

        public ShowDappViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

#if DEBUG
        private void DesignerMode()
        {
            DappName = "objkt.com";
            Address = "tz1Mrt2GJcKBCAWdwWK6mRwhpqt9XGGH6tLb";
            DappId = "kfonvikro3ng";
            Permissions = new List<PermissionScope>
            {
                PermissionScope.sign,
                PermissionScope.operation_request,
            };
        }
#endif
    }
}