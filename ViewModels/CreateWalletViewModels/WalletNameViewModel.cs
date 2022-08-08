using ReactiveUI;
using System.IO;
using System;
using Atomex.Client.Desktop.Common;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class WalletNameViewModel : StepViewModel
    {
        private StepData StepData { get; set; }
        [Reactive] public string WalletName { get; set; }
        [Reactive] public string Warning { get; set; }
        private string WalletNameTrimmed => WalletName?.Trim() ?? string.Empty;

        public WalletNameViewModel()
        {
            this.WhenAnyValue(vm => vm.WalletName)
                .SubscribeInMainThread(_ => Warning = string.Empty);
        }

        public override void Initialize(object arg)
        {
            StepData = (StepData)arg;
        }

        public override void Next()
        {
            if (string.IsNullOrEmpty(WalletNameTrimmed))
            {
                Warning = Properties.Resources.CwvEmptyWalletName;
                return;
            }

            if (WalletNameTrimmed.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 || WalletNameTrimmed.IndexOf('.') != -1)
            {
                Warning = Properties.Resources.CwvInvalidWalletName;
                return;
            }

            var pathToWallet = $"{WalletInfo.CurrentWalletDirectory}/{WalletNameTrimmed}/{WalletInfo.DefaultWalletFileName}";

            try
            {
                var _ = Path.GetFullPath(pathToWallet);
            }
            catch (Exception)
            {
                Warning = Properties.Resources.CwvInvalidWalletName;
                return;
            }

            if (File.Exists(pathToWallet))
            {
                Warning = Properties.Resources.CwvWalletAlreadyExists;
                return;
            }

            StepData.PathToWallet = pathToWallet;
            RaiseOnNext(StepData);
        }
    }
}