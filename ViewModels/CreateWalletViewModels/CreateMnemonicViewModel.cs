using System.Collections.Generic;
using System.Windows.Input;
using System.Linq;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.Common;
using Atomex.Cryptography;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class CreateMnemonicViewModel : StepViewModel
    {
        private static IEnumerable<KeyValuePair<string, Wordlist>> Languages { get; } =
            new List<KeyValuePair<string, Wordlist>>
            {
                new("English", Wordlist.English),
                new("French", Wordlist.French),
                new("Japanese", Wordlist.Japanese),
                new("Spanish", Wordlist.Spanish),
                new("Portuguese Brazil", Wordlist.PortugueseBrazil),
                new("Chinese Traditional", Wordlist.ChineseTraditional),
                new("Chinese Simplified", Wordlist.ChineseSimplified)
            };

        public IEnumerable<string> GetLanguages => Languages.Select(kv => kv.Key).ToList();

        private static IEnumerable<KeyValuePair<string, int>> WordCountToEntropyLength { get; } =
            new List<KeyValuePair<string, int>>
            {
                new("12", 128),
                new("15", 160),
                new("18", 192),
                new("21", 224),
                new("24", 256)
            };

        public IEnumerable<string> GetWordCountToEntropyLength =>
            WordCountToEntropyLength.Select(kv => kv.Key).ToList();

        private StepData StepData { get; set; }
        [Reactive] public string Mnemonic { get; set; }
        [Reactive] public string Warning { get; set; }
        private Wordlist _language = Wordlist.English;

        public string Language
        {
            get => KeyValExtension.GetKeyByValue(Languages, _language);
            set
            {
                var newValue = KeyValExtension.GetValueByKey(Languages, value);
                if (_language != newValue)
                {
                    _language = newValue;
                    Mnemonic = string.Empty;
                }
            }
        }

        private int _entropyLength = 192;

        public string EntropyLength
        {
            get => KeyValExtension.GetKeyByValue(WordCountToEntropyLength, _entropyLength);
            set
            {
                var newValue = KeyValExtension.GetValueByKey(WordCountToEntropyLength, value);
                if (_entropyLength == newValue) return;
                _entropyLength = newValue;
                Mnemonic = string.Empty;
            }
        }

        private ICommand? _mnemonicCommand;
        public ICommand MnemonicCommand => _mnemonicCommand ??= _mnemonicCommand = ReactiveCommand.Create(() =>
        {
            var entropy = Rand.SecureRandomBytes(_entropyLength / 8);

            Mnemonic = new Mnemonic(_language, entropy).ToString();
            Warning = string.Empty;
        });

        public override void Initialize(object arg)
        {
            StepData = (StepData)arg;
        }

        public override void Back()
        {
            Warning = string.Empty;
            base.Back();
        }

        public override void Next()
        {
            if (string.IsNullOrEmpty(Mnemonic))
            {
                Warning = Resources.CwvCreateMnemonicWarning;
                return;
            }

            StepData.Mnemonic = Mnemonic;
            StepData.Language = _language;

            RaiseOnNext(StepData);
        }
    }
}