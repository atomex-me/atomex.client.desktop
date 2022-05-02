using System.Security;
using Atomex.Wallet;

namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class WriteDerivedKeyPasswordViewModel : StepViewModel
    {
        public WriteDerivedKeyPasswordViewModel()
        {
            PasswordVM = new PasswordControlViewModel(placeholder: "Password...");
        }

        public PasswordControlViewModel PasswordVM { get; }

        private StepData StepData { get; set; }

        private SecureString Password => PasswordVM.SecurePass;

        public override void Initialize(object o)
        {
            StepData = (StepData) o;
        }

        public override void Back()
        {
            PasswordVM.StringPass = string.Empty;

            base.Back();
        }

        public override void Next()
        {
            var wallet = new HdWallet_OLD(
                mnemonic: StepData.Mnemonic,
                wordList: StepData.Language,
                passPhrase: Password,
                network: StepData.Network)
            {
                PathToWallet = StepData.PathToWallet
            };

            PasswordVM.StringPass = string.Empty;

            RaiseOnNext(wallet);
        }
    }
}