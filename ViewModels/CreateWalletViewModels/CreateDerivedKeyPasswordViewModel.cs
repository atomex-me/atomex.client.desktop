using System;
using System.Security;
using Atomex.Client.Desktop.Helpers;
using ReactiveUI;


namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class CreateDerivedKeyPasswordViewModel : StepViewModel
    {
        public CreateDerivedKeyPasswordViewModel()
        {
            PassVM1 = new PasswordControlViewModel(ComparePasswords);
            PassVM2 = new PasswordControlViewModel(ComparePasswords);
        }

        private void ComparePasswords()
        {
            PasswordsEqual = PassVM1.SecurePass.IsEqualTo(PassVM2.SecurePass);
        }

        private bool _passwordsEqual;
        public bool PasswordsEqual
        {
            get => _passwordsEqual;
            set { this.RaiseAndSetIfChanged(ref _passwordsEqual, value); }
        }

        public PasswordControlViewModel PassVM1 { get; set; }
        public PasswordControlViewModel PassVM2 { get; set; }
    }
}