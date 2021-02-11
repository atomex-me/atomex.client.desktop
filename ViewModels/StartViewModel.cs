using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.ViewModels
{
    public class StartViewModel : ViewModelBase
    {
        public StartViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }


        public bool HasWallets { get; set; }

        public void MyWalletsCommand()
        {
        }

        public void CreateNewCommand()
        {
        }

        public void RestoreByMnemonicCommand()
        {
        }

        public void TwitterCommand()
        {
        }

        public void GithubCommand()
        {
        }

        public void TelegramCommand()
        {
        }

        private void DesignerMode()
        {
            HasWallets = true;
        }
    }
}