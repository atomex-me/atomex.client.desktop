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
            PassVM1 = new PasswordControlViewModel(PasswordChanged, "Password...");
            PassVM2 = new PasswordControlViewModel(PasswordChanged, "Password confirmation...");
        }

        private PasswordControlViewModel _passVM1;

        public PasswordControlViewModel PassVM1
        {
            get => _passVM1;
            set
            {
                _passVM1 = value;
                this.RaisePropertyChanged(nameof(PassVM1));
            }
        }

        private PasswordControlViewModel _passVM2;

        public PasswordControlViewModel PassVM2
        {
            get => _passVM2;
            set
            {
                _passVM2 = value;
                this.RaisePropertyChanged(nameof(PassVM2));
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
            PasswordScore = (int) PasswordAdvisor.CheckStrength(PassVM1.SecurePass);
        }

        public override void Initialize(
            object o)
        {
            StepData = (StepData) o;
        }

        public override void Back()
        {
            Warning = string.Empty;
            PassVM1.StringPass = string.Empty;
            PassVM2.StringPass = string.Empty;
            PasswordScore = 0;

            base.Back();
        }

        public override void Next()
        {
            // password is optional
            if (PassVM1.SecurePass.Length > 0)
            {
                if (PasswordScore < (int) PasswordAdvisor.PasswordScore.Medium)
                {
                    Warning = Properties.Resources.CwvPasswordInsufficientComplexity;
                    return;
                }

                if (PassVM2.SecurePass.Length > 0 && !PassVM1.SecurePass.SecureEqual(PassVM2.SecurePass) ||
                    PassVM2.SecurePass.Length == 0)
                {
                    Warning = Properties.Resources.CwvPasswordsDoNotMatch;
                    return;
                }
            }

            var wallet = new HdWallet(
                mnemonic: StepData.Mnemonic,
                wordList: StepData.Language,
                passPhrase: PassVM1.SecurePass,
                network: StepData.Network)
            {
                PathToWallet = StepData.PathToWallet
            };

            Warning = string.Empty;
            PassVM1.StringPass = string.Empty;
            PassVM2.StringPass = string.Empty;
            PasswordScore = 0;

            RaiseOnNext(wallet);
        }
    }
}