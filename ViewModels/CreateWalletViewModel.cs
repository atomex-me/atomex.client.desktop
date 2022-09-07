using System;
using System.Collections.Generic;
using System.Windows.Input;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Wallet.Abstract;
using Atomex.Client.Desktop.ViewModels.CreateWalletViewModels;

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
        private const int CreateMnemonicViewIndex = 2;
        private const int WriteMnemonicViewIndex = 3;
        private const int ConfirmMnemonicViewIndex = 4;
        private const int CreateDerivedKeyPasswordViewIndex = 5;
        private const int WriteDerivedKeyPasswordViewIndex = 6;
        private const int CreateStoragePasswordViewIndex = 7;
        
        public event Action<IAccount, ILocalStorage> OnAccountCreated;
        public event Action OnCanceled;

        private readonly IAtomexApp _app;
        private readonly List<StepViewModel> _viewModels;

        private int[] CreateNewWalletViewIndexes { get; } =
        {
            WalletTypeViewIndex,
            WalletNameViewIndex,
            CreateMnemonicViewIndex,
            ConfirmMnemonicViewIndex,
            CreateDerivedKeyPasswordViewIndex,
            CreateStoragePasswordViewIndex
        };

        private int[] RestoreWalletViewIndexes { get; } =
        {
            WalletTypeViewIndex,
            WalletNameViewIndex,
            WriteMnemonicViewIndex,
            WriteDerivedKeyPasswordViewIndex,
            CreateStoragePasswordViewIndex
        };

        private int[] ViewIndexes { get; }
        [Reactive] public ViewModelBase Content { get; set; }
        [Reactive] public bool CanBack { get; set; }
        [Reactive] public bool CanNext { get; set; }
        [Reactive] public string BackText { get; set; }
        [Reactive] public string NextText { get; set; }
        [Reactive] public int StepsCount { get; set; }
        [Reactive] public bool InProgress { get; set; }

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
                _viewModels[CurrentViewIndex].Step = _step + 1;

                NextText = _step < ViewIndexes.Length - 1
                    ? Properties.Resources.CwvNext
                    : Properties.Resources.CwvFinish;
            }
        }

        private int _currentViewIndex;

        private int CurrentViewIndex
        {
            get => _currentViewIndex;
            set
            {
                _currentViewIndex = value;
                Content = _viewModels[_currentViewIndex];
            }
        }

        public CreateWalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public CreateWalletViewModel(
            IAtomexApp app,
            CreateWalletScenario scenario,
            Action<IAccount, ILocalStorage>? onAccountCreated = null,
            Action? onCanceled = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            _viewModels = new List<StepViewModel>
            {
                new WalletTypeViewModel(),
                new WalletNameViewModel(),
                new CreateMnemonicViewModel(),
                new WriteMnemonicViewModel(),
                new ConfirmMnemonicViewModel(),
                new CreateDerivedKeyPasswordViewModel(),
                new WriteDerivedKeyPasswordViewModel(),
                new CreateStoragePasswordViewModel(_app)
            };

            InProgress = false;

            if (onAccountCreated != null)
                OnAccountCreated += onAccountCreated;

            if (onCanceled != null)
                OnCanceled += onCanceled;

            ViewIndexes = ResolveViewIndexes(scenario);

            foreach (var viewIndex in ViewIndexes)
            {
                var viewModel = _viewModels[viewIndex];

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
                        var (account, localStorage) = ((IAccount, ILocalStorage))arg;

                        OnAccountCreated?.Invoke(account, localStorage);
                        return;
                    }

                    Step++;
                    _viewModels[CurrentViewIndex].Initialize(arg);
                };
                viewModel.ProgressBarShow += () => { InProgress = true; };
                viewModel.ProgressBarHide += () => { InProgress = false; };
            }

            Step = 0;
            StepsCount = ViewIndexes.Length;
            BackText = Properties.Resources.CwvBack;
            NextText = Properties.Resources.CwvNext;
            CanBack = true;
            CanNext = true;
        }

        private ICommand? _backCommand;

        public ICommand BackCommand =>
            _backCommand ??= ReactiveCommand.Create(() => { _viewModels[CurrentViewIndex].Back(); });


        private ICommand? _nextCommand;

        public ICommand NextCommand =>
            _nextCommand ??= ReactiveCommand.Create(() => { _viewModels[CurrentViewIndex].Next(); });

        private int[] ResolveViewIndexes(CreateWalletScenario scenario)
        {
            return scenario switch
            {
                CreateWalletScenario.CreateNew => CreateNewWalletViewIndexes,
                CreateWalletScenario.Restore => RestoreWalletViewIndexes,
                _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
            };
        }

        private void DesignerMode()
        {
            InProgress = false;
        }
    }
}