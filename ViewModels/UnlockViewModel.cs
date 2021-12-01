using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Client.Desktop.Common;
using ReactiveUI;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class UnlockViewModel : ViewModelBase
    {
        public Action? Unlocked;
        public event EventHandler<ErrorEventArgs> Error;
        public PasswordControlViewModel PasswordVM { get; set; }
        public string WalletName { get; set; }

        private bool _inProgress;

        public bool InProgress
        {
            get => _inProgress;
            set
            {
                _inProgress = value;
                this.RaisePropertyChanged(nameof(InProgress));
            }
        }

        private bool _invalidPassword;

        public bool InvalidPassword
        {
            get => _invalidPassword;
            set
            {
                _invalidPassword = value;
                this.RaisePropertyChanged(nameof(InvalidPassword));
            }
        }

        private ICommand _unlockCommand;
        public ICommand UnlockCommand => _unlockCommand ??= (_unlockCommand = ReactiveCommand.Create(OnUnlockClick));

        private readonly Action<SecureString> _unlockAction;

        public UnlockViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public UnlockViewModel(
            string walletName,
            Action<SecureString> unlockAction,
            Action goBack)
        {
            WalletName = $"\"{walletName}\"";
            _unlockAction = unlockAction;
            GoBack += goBack;

            PasswordVM = new PasswordControlViewModel(
                () => { InvalidPassword = false; }, placeholder: "Password...", isSmall: true)
            {
                IsFocused = true
            };
        }

        private async void OnUnlockClick()
        {
            InvalidPassword = false;
            InProgress = true;

            try
            {
                await Task.Run(() => { _unlockAction(PasswordVM.SecurePass); });
            }
            catch (CryptographicException e)
            {
                Log.Error("Invalid password error");

                InvalidPassword = true;
                InProgress = false;

                Error?.Invoke(this, new ErrorEventArgs(e));
                return;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unlocking error");

                InProgress = false;

                Error?.Invoke(this, new ErrorEventArgs(e));
                return;
            }
            finally
            {
                InProgress = false;
            }

            PasswordVM.StringPass = string.Empty;

            Unlocked?.Invoke();
        }

        private Action GoBack;

        public void GoBackCommand()
        {
            GoBack?.Invoke();
        }

        private void DesignerMode()
        {
            InProgress = true;
        }
    }
}