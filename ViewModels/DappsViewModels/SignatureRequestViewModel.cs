using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Dialogs;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Hex = Atomex.Common.Hex;

namespace Atomex.Client.Desktop.ViewModels.DappsViewModels
{
    public class SignatureRequestViewModel : ViewModelBase, IDialogViewModel
    {
        private const int PayloadServiceBytesNum = 6;
        public string DappName { get; set; }
        [Reactive] public string BytesPayload { get; set; }
        [ObservableAsProperty] public string RawPayload { get; }
        public Func<Task> OnSign { get; set; }
        public Func<Task> OnReject { get; set; }

        [ObservableAsProperty] public bool IsSigning { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }
        public Action? OnClose { get; set; }

        public SignatureRequestViewModel()
        {
            OnSignCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSigning);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);

            this.WhenAnyValue(vm => vm.BytesPayload)
                .WhereNotNull()
                .Select(bytesPayload =>
                {
                    try
                    {
                        var parsedBytes = Hex.FromString(bytesPayload[(PayloadServiceBytesNum * 2)..]);
                        return System.Text.Encoding.UTF8.GetString(parsedBytes);
                    }
                    catch (Exception)
                    {
                        return "Can't parse income payload to sign, check it out on bytes tab.";
                    }
                })
                .ToPropertyExInMainThread(this, vm => vm.RawPayload);

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
            BytesPayload =
                "sahdau746wsahdau746w3c7b346vwq73bcw3v4tbawdystcv64wx6wc45arw3sahdau746w3c7b346vwq73bcw3v4tbawdystcv64wx6wc45arw3crw3yjt436uwv47bt5uwccrw3yjt436uwv47bt5uwc346vwq73bcw3v4tbawdystcv64wx6wc45arw3 crw3yjt436uwv47bt5uwc";
        }
#endif
    }
}