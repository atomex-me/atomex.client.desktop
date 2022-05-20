using System;
using System.IO;
using System.Reactive;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Client.Desktop.Common;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.ViewModels
{
    public class UnlockViewModel : ViewModelBase
    {
        public Action? Unlocked;
        private readonly Action<SecureString> _unlockAction;
        private readonly Action _goBackAction;
        public event EventHandler<ErrorEventArgs> Error;
        [Reactive] public bool InProgress { get; set; }
        [Reactive] public bool InvalidPassword { get; set; }
        
        public PasswordControlViewModel PasswordVM { get; set; }
        public string WalletName { get; set; }

        private ReactiveCommand<Unit, Unit> _unlockCommand;
        public ReactiveCommand<Unit, Unit> UnlockCommand => _unlockCommand ??= (_unlockCommand = ReactiveCommand.Create(OnUnlockClick));

        public UnlockViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public UnlockViewModel(
            string walletName,
            Action<SecureString> unlockAction,
            Action goBack,
            Action? onUnlock = null)
        {
            WalletName = $"\"{walletName}\"";
            _unlockAction = unlockAction;
            _goBackAction += goBack;
            Unlocked += onUnlock;

            PasswordVM = new PasswordControlViewModel(
                onPasswordChanged: () => { InvalidPassword = false; },
                placeholder: "Password...",
                isSmall: true)
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

        public void GoBackCommand()
        {
            _goBackAction?.Invoke();
        }

        private void DesignerMode()
        {
            InProgress = true;
        }
    }
}