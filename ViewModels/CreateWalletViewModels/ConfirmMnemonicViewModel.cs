using System;
using System.Linq;
using System.Reactive;

using Avalonia.Collections;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class ConfirmMnemonicViewModel : StepViewModel
    {
        private StepData StepData { get; set; }
        private string MnemonicString { get; set; }
        [Reactive] public string Warning { get; set; }
        [Reactive] public AvaloniaList<string> ConfirmedMnemonicWords { get; set; }
        [Reactive] public AvaloniaList<string> RandomizedMnemonicWords { get; set; }

        public ConfirmMnemonicViewModel()
        {
            this.WhenAnyValue(vm => vm.RandomizedMnemonicWords)
                .SubscribeInMainThread(_ => Warning = string.Empty);
        }

        public override void Initialize(object arg)
        {
            StepData = (StepData)arg;
            Warning = string.Empty;
            if (StepData.Mnemonic.Equals(MnemonicString)) return;

            MnemonicString = StepData.Mnemonic;
            var random = new Random();
            RandomizedMnemonicWords =
                new AvaloniaList<string>(StepData.Mnemonic.Split(" ").OrderBy(_ => random.Next()));
            ConfirmedMnemonicWords = new AvaloniaList<string>();
        }

        public override void Next()
        {
            if (!string.Join(" ", ConfirmedMnemonicWords).Equals(StepData.Mnemonic))
            {
                Warning = "Incorrect words order, please try again";
                return;
            }

            RaiseOnNext(StepData);
        }

        private ReactiveCommand<string, Unit>? _addWordCommand;
        public ReactiveCommand<string, Unit> AddWordCommand => _addWordCommand ??= _addWordCommand =
            ReactiveCommand.Create<string>(word =>
            {
                RandomizedMnemonicWords = new AvaloniaList<string>(RandomizedMnemonicWords
                    .Where(w => w != word));
                ConfirmedMnemonicWords.Add(word);
            });

        private ReactiveCommand<string, Unit>? _removeWordCommand;
        public ReactiveCommand<string, Unit> RemoveWordCommand => _removeWordCommand ??= _removeWordCommand =
            ReactiveCommand.Create<string>(word =>
            {
                ConfirmedMnemonicWords = new AvaloniaList<string>(ConfirmedMnemonicWords
                    .Where(w => w != word));
                RandomizedMnemonicWords.Add(word);
            });
    }
}