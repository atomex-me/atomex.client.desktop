using ReactiveUI;
using System.Windows.Input;
using System.IO;
using System;
using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class WalletNameViewModel : StepViewModel
    {
        private StepData StepData { get; set; }

        private string _walletName;

        public string WalletName
        {
            get => _walletName;
            set
            {
                Warning = string.Empty;
                this.RaiseAndSetIfChanged(ref _walletName, value);
            }
        }


        private string _warning;

        public string Warning
        {
            get => _warning;
            set { this.RaiseAndSetIfChanged(ref _warning, value); }
        }

        private ICommand _textChangedCommand;

        public override void Initialize(
            object arg)
        {
            StepData = (StepData) arg;
        }

        public override void Next()
        {
            if (string.IsNullOrEmpty(WalletName))
            {
                Warning = Properties.Resources.CwvEmptyWalletName;
                return;
            }

            if (WalletName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 ||
                WalletName.IndexOf('.') != -1)
            {
                Warning = Properties.Resources.CwvInvalidWalletName;
                return;
            }

            var pathToWallet = $"{WalletInfo.CurrentWalletDirectory}/{WalletName}/{WalletInfo.DefaultWalletFileName}";

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