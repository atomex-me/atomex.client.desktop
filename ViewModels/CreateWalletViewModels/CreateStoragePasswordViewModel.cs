using System;
using System.IO;
using System.Threading.Tasks;

using ReactiveUI;
using Serilog;

using Atomex.Client.Desktop.Properties;
using Atomex.Common;
using Atomex.Wallet;
using Atomex.LiteDb;

namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class CreateStoragePasswordViewModel : StepViewModel
    {
        private readonly IAtomexApp _app;
        private HdWallet _wallet;

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

        public CreateStoragePasswordViewModel()
        {
        }

        public CreateStoragePasswordViewModel(
            IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            PasswordVM = new PasswordControlViewModel(PasswordChanged, "Password...");
            PasswordConfirmationVM = new PasswordControlViewModel(PasswordChanged, "Password confirmation...");
        }

        private void PasswordChanged()
        {
            Warning = string.Empty;
            PasswordScore = (int)PasswordAdvisor.CheckStrength(PasswordVM.SecurePass);
        }

        public override void Initialize(object o)
        {
            _wallet = (HdWallet)o;
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
            // todo: rollback password strength checking;
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

                var (account, localStorage) = await Task.Run(async () =>
                {
                    await _wallet.EncryptAsync(PasswordVM.SecurePass);

                    var saved = _wallet.SaveToFile(_wallet.PathToWallet, PasswordVM.SecurePass);

                    if (!saved)
                    {
                        Warning = "Can't save wallet file to file system. Try to use different wallet name.";
                        throw new IOException(Warning);
                    }

                    var localStorage = new LiteDbCachedLocalStorage(
                        pathToDb: Path.Combine(Path.GetDirectoryName(_wallet.PathToWallet), Account.DefaultDataFileName),
                        password: PasswordVM.SecurePass,
                        currencies: _app.CurrenciesProvider.GetCurrencies(_wallet.Network),
                        network: _wallet.Network);

                    var account = new Account(
                        wallet: _wallet,
                        localStorage: localStorage,
                        currenciesProvider: _app.CurrenciesProvider);

                    PasswordVM.StringPass = string.Empty;
                    PasswordConfirmationVM.StringPass = string.Empty;
                    PasswordScore = 0;

                    return (account, localStorage);
                });

                RaiseOnNext((account, localStorage));
            }
            catch (Exception e)
            {
                // todo: warning
                RaiseProgressBarHide();
                Log.Error(e, "Create storage password error");
            }
        }
    }
}