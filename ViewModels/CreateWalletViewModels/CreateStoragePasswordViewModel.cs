using System;
using System.Security;
using System.Windows.Input;
using Serilog;
using Atomex.Common;
using Atomex.Wallet;
using Atomex.Client.Desktop.Properties;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class CreateStoragePasswordViewModel : StepViewModel
    {
        private IAtomexApp App { get; }
        private HdWallet Wallet { get; set; }

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

        public CreateStoragePasswordViewModel(
            IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            PasswordVM = new PasswordControlViewModel(PasswordChanged, "Password...");
            PasswordConfirmationVM = new PasswordControlViewModel(PasswordChanged, "Password confirmation...");
        }

        private void PasswordChanged()
        {
            Warning = string.Empty;
            PasswordScore = (int) PasswordAdvisor.CheckStrength(PasswordVM.SecurePass);
        }

        public override void Initialize(object o)
        {
            Wallet = (HdWallet) o;
        }

        public override void Back()
        {
            Warning = string.Empty;
            PasswordVM.StringPass = string.Empty;
            PasswordConfirmationVM.StringPass = string.Empty;
            PasswordScore = 0;

            base.Back();
        }

        public override async void Next()
        {
            // todo: rollback password strength cheking;
            // if (PasswordScore < (int) PasswordAdvisor.PasswordScore.Medium)
            // {
            //     Warning = Resources.CwvPasswordInsufficientComplexity;
            //     return;
            // }

            if (PasswordVM.SecurePass.Length > 0 &&
                PasswordConfirmationVM.SecurePass.Length > 0 &&
                !PasswordVM.SecurePass.SecureEqual(PasswordConfirmationVM.SecurePass) ||
                PasswordConfirmationVM.SecurePass.Length == 0)
            {
                Warning = Resources.CwvPasswordsDoNotMatch;
                return;
            }

            try
            {
                RaiseProgressBarShow();

                await Wallet.EncryptAsync(PasswordVM.SecurePass);

                Wallet.SaveToFile(Wallet.PathToWallet, PasswordVM.SecurePass);

                var account = new Account(
                    Wallet,
                    PasswordVM.SecurePass,
                    App.CurrenciesProvider,
                    ClientType.Unknown);

                PasswordVM.StringPass = string.Empty;
                PasswordConfirmationVM.StringPass = string.Empty;
                PasswordScore = 0;

                RaiseOnNext(account);
            }
            catch (Exception e)
            {
                // todo: warning
                Log.Error(e, "Create storage password error");
            }
            finally
            {
                RaiseProgressBarHide();
            }
        }
    }
}