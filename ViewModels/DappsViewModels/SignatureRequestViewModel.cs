using System;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class SignatureRequestViewModel : ViewModelBase
    {
        public string DappName { get; set; }
        public string Payload { get; set; }
        public Func<Task> OnSign { get; set; }
        public Func<Task> OnReject { get; set; }

        [ObservableAsProperty] public bool IsSigning { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        public SignatureRequestViewModel()
        {
            OnSignCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSigning);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);

#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        private ReactiveCommand<Unit, Unit>? _onSignCommand;
        public ReactiveCommand<Unit, Unit> OnSignCommand =>
            _onSignCommand ??= ReactiveCommand.CreateFromTask(async () => await OnSign());

        private ReactiveCommand<Unit, Unit>? _onRejectCommand;
        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());


#if DEBUG
        private void DesignerMode()
        {
            DappName = "objkt.com";
            Payload =
                "sahdau746wsahdau746w3c7b346vwq73bcw3v4tbawdystcv64wx6wc45arw3sahdau746w3c7b346vwq73bcw3v4tbawdystcv64wx6wc45arw3crw3yjt436uwv47bt5uwccrw3yjt436uwv47bt5uwc346vwq73bcw3v4tbawdystcv64wx6wc45arw3 crw3yjt436uwv47bt5uwc";
        }
#endif
    }
}