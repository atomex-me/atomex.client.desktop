using System;
using System.Windows.Input;
using Atomex.Wallet.Abstract;
using Atomex.Client.Desktop.Common;
using System.Collections.Generic;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public enum CreateWalletScenario
    {
        CreateNew,
        Restore
    }

    public class CreateWalletViewModel : ViewModelBase
    {
        private const int WalletTypeViewIndex = 0;
        private const int WalletNameViewIndex = 1;
        private const int CreateMnemonicViewIIndex = 2;
        private const int WriteMnemonicViewIIndex = 3;
        private const int CreateDerivedKeyPasswordViewIIndex = 4;
        private const int WriteDerivedKeyPasswordViewIIndex = 5;
        private const int CreateStoragePasswordViewIIndex = 6;

        public event Action<IAccount> OnAccountCreated;
        public event Action OnCanceled;

        private IAtomexApp App { get; }

        public List<StepViewModel> ViewModels { get; }

        private int[] CreateNewWalletViewIndexes { get; } =
        {
            WalletTypeViewIndex,
            WalletNameViewIndex,
            CreateMnemonicViewIIndex,
            CreateDerivedKeyPasswordViewIIndex,
            CreateStoragePasswordViewIIndex
        };

        private int[] RestoreWalletViewIndexes { get; } =
        {
            WalletTypeViewIndex,
            WalletNameViewIndex,
            WriteMnemonicViewIIndex,
            WriteDerivedKeyPasswordViewIIndex,
            CreateStoragePasswordViewIIndex
        };

        private int[] ViewIndexes { get; }

        private int _currentViewIndex;
        public int CurrentViewIndex
        {
            get => _currentViewIndex;
            set
            {
                _currentViewIndex = value;
                Content = ViewModels[_currentViewIndex];
            }
        }

        private ViewModelBase _content;

        public ViewModelBase Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        private bool _canBack;

        public bool CanBack
        {
            get => _canBack;
            set => this.RaiseAndSetIfChanged(ref _canBack, value);
        }

        private bool _canNext;

        public bool CanNext
        {
            get => _canNext;
            set => this.RaiseAndSetIfChanged(ref _canNext, value);
        }

        private string _backText = Properties.Resources.CwvBack;

        public string BackText
        {
            get => _backText;
            set => this.RaiseAndSetIfChanged(ref _backText, value);
        }

        private string _nextText = Properties.Resources.CwvNext;

        public string NextText
        {
            get => _nextText;
            set => this.RaiseAndSetIfChanged(ref _nextText, value);
        }

        private int _step;

        public int Step
        {
            get => _step;
            set
            {
                this.RaiseAndSetIfChanged(ref _step, value);

                if (_step < 0 || _step >= ViewIndexes.Length)
                    return;

                CurrentViewIndex = ViewIndexes[_step];
                ViewModels[CurrentViewIndex].Step = _step + 1;

                NextText = _step < ViewIndexes.Length - 1
                    ? Properties.Resources.CwvNext
                    : Properties.Resources.CwvFinish;
            }
        }

        private int _stepsCount;

        public int StepsCount
        {
            get => _stepsCount;
            set => this.RaiseAndSetIfChanged(ref _stepsCount, value);
        }

        private bool _inProgress;

        public bool InProgress
        {
            get => _inProgress;
            set => this.RaiseAndSetIfChanged(ref _inProgress, value);
        }

        public CreateWalletViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public CreateWalletViewModel(
            IAtomexApp app,
            CreateWalletScenario scenario,
            Action<IAccount> onAccountCreated = null,
            Action onCanceled = null)
        {
            // todo: uncomment this
            // App = app ?? throw new ArgumentNullException(nameof(app));

            ViewModels = new List<StepViewModel>
            {
                new WalletTypeViewModel(),
                new WalletNameViewModel(),
                new CreateMnemonicViewModel(),
                new WriteMnemonicViewModel(),
                new CreateDerivedKeyPasswordViewModel(),
                new WriteDerivedKeyPasswordViewModel(),
                new CreateStoragePasswordViewModel(App)
            };

            InProgress = false;

            if (onAccountCreated != null)
                OnAccountCreated += onAccountCreated;

            if (onCanceled != null)
                OnCanceled += onCanceled;

            ViewIndexes = ResolveViewIndexes(scenario);

            foreach (var viewIndex in ViewIndexes)
            {
                var viewModel = ViewModels[viewIndex];

                viewModel.OnBack += arg =>
                {
                    if (Step == 0)
                    {
                        OnCanceled?.Invoke();
                        return;
                    }

                    Step--;
                };
                viewModel.OnNext += arg =>
                {
                    if (Step == ViewIndexes.Length - 1)
                    {
                        OnAccountCreated?.Invoke((IAccount) arg);
                        return;
                    }

                    Step++;
                    ViewModels[CurrentViewIndex].Initialize(arg);
                };
                viewModel.ProgressBarShow += () => { InProgress = true; };
                viewModel.ProgressBarHide += () => { InProgress = false; };
            }

            Step = 0;
            StepsCount = ViewIndexes.Length;
            CanBack = true;
            CanNext = true;
        }

        private ICommand _backCommand;
        // _backCommand ??
        public ICommand BackCommand =>
            _backCommand = ReactiveCommand.Create(() => { ViewModels[CurrentViewIndex].Back(); });

        
        private ICommand _nextCommand;
        // _nextCommand ??
        public ICommand NextCommand =>
            _nextCommand = ReactiveCommand.Create(() => { ViewModels[CurrentViewIndex].Next(); });

        private int[] ResolveViewIndexes(
            CreateWalletScenario scenario)
        {
            if (scenario == CreateWalletScenario.CreateNew)
                return CreateNewWalletViewIndexes;

            if (scenario == CreateWalletScenario.Restore)
                return RestoreWalletViewIndexes;

            throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        private void DesignerMode()
        {
            InProgress = false;
        }
    }
}