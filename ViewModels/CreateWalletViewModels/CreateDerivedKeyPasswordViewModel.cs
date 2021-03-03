using System;
using System.Security;
using Atomex.Client.Desktop.Helpers;
using ReactiveUI;
using Atomex.Common;
using Atomex.Wallet;


namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class CreateDerivedKeyPasswordViewModel : StepViewModel
    {
        public CreateDerivedKeyPasswordViewModel()
        {
            PasswordVM = new PasswordControlViewModel(PasswordChanged, "Password...");
            PasswordConfirmationVM = new PasswordControlViewModel(PasswordChanged, "Password confirmation...");
        }

        private PasswordControlViewModel _passwordVM;

        public PasswordControlViewModel PasswordVM
        {
            get => _passwordVM;
            set
            {
                _passwordVM = value;
                this.RaisePropertyChanged(nameof(PasswordVM));
            }
        }

        private PasswordControlViewModel _passwordConfirmationVM;

        public PasswordControlViewModel PasswordConfirmationVM
        {
            get => _passwordConfirmationVM;
            set
            {
                _passwordConfirmationVM = value;
                this.RaisePropertyChanged(nameof(PasswordConfirmationVM));
            }
        }


        private StepData StepData { get; set; }

        private string _warning;

        public string Warning
        {
            get => _warning;
            set
            {
                _warning = value;
                this.RaisePropertyChanged(nameof(Warning));
            }
        }

        private int _passwordScore;

        public int PasswordScore
        {
            get => _passwordScore;
            set
            {
                _passwordScore = value;
                this.RaisePropertyChanged(nameof(PasswordScore));
            }
        }

        private void PasswordChanged()
        {
            Warning = string.Empty;
            PasswordScore = (int) PasswordAdvisor.CheckStrength(PasswordVM.SecurePass);
        }

        public override void Initialize(
            object o)
        {
            StepData = (StepData) o;
        }

        public override void Back()
        {
            Warning = string.Empty;
            PasswordVM.StringPass = string.Empty;
            PasswordConfirmationVM.StringPass = string.Empty;
            PasswordScore = 0;

            base.Back();
        }

        public override void Next()
        {
            // password is optional
            if (PasswordVM.SecurePass.Length > 0)
            {
                if (PasswordScore < (int) PasswordAdvisor.PasswordScore.Medium)
                {
                    Warning = Properties.Resources.CwvPasswordInsufficientComplexity;
                    return;
                }

                if (PasswordConfirmationVM.SecurePass.Length > 0 &&
                    !PasswordVM.SecurePass.SecureEqual(PasswordConfirmationVM.SecurePass) ||
                    PasswordConfirmationVM.SecurePass.Length == 0)
                {
                    Warning = Properties.Resources.CwvPasswordsDoNotMatch;
                    return;
                }
            }

            var wallet = new HdWallet(
                mnemonic: StepData.Mnemonic,
                wordList: StepData.Language,
                passPhrase: PasswordVM.SecurePass,
                network: StepData.Network)
            {
                PathToWallet = StepData.PathToWallet
            };

            Warning = string.Empty;
            PasswordVM.StringPass = string.Empty;
            PasswordConfirmationVM.StringPass = string.Empty;
            PasswordScore = 0;

            RaiseOnNext(wallet);
        }
    }
}