using System.Collections.Generic;
using System.Windows.Input;
using System.Linq;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.Common;
using Atomex.Cryptography;
using NBitcoin;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels.CreateWalletViewModels
{
    public class CreateMnemonicViewModel : StepViewModel
    {
        private static IEnumerable<KeyValuePair<string, Wordlist>> Languages { get; } =
            new List<KeyValuePair<string, Wordlist>>
            {
                new KeyValuePair<string, Wordlist>("English", Wordlist.English),
                new KeyValuePair<string, Wordlist>("French", Wordlist.French),
                new KeyValuePair<string, Wordlist>("Japanese", Wordlist.Japanese),
                new KeyValuePair<string, Wordlist>("Spanish", Wordlist.Spanish),
                new KeyValuePair<string, Wordlist>("Portuguese Brazil", Wordlist.PortugueseBrazil),
                new KeyValuePair<string, Wordlist>("Chinese Traditional", Wordlist.ChineseTraditional),
                new KeyValuePair<string, Wordlist>("Chinese Simplified", Wordlist.ChineseSimplified)
            };

        public IEnumerable<string> GetLanguages
        {
            get => Languages.Select(kv => kv.Key).ToList();
        }

        private static IEnumerable<KeyValuePair<string, int>> WordCountToEntropyLength { get; } =
            new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("12", 128),
                new KeyValuePair<string, int>("15", 160),
                new KeyValuePair<string, int>("18", 192),
                new KeyValuePair<string, int>("21", 224),
                new KeyValuePair<string, int>("24", 256)
            };

        public IEnumerable<string> GetWordCountToEntropyLength
        {
            get => WordCountToEntropyLength.Select(kv => kv.Key).ToList();
        }

        private StepData StepData { get; set; }

        private string _mnemonic;

        public string Mnemonic
        {
            get => _mnemonic;
            set { this.RaiseAndSetIfChanged(ref _mnemonic, value); }
        }

        private string _warning;

        public string Warning
        {
            get => _warning;
            set { this.RaiseAndSetIfChanged(ref _warning, value); }
        }

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
                if (_entropyLength != newValue)
                {
                    _entropyLength = newValue;
                    Mnemonic = string.Empty;
                }
            }
        }

        private ICommand _mnemonicCommand;

        public ICommand MnemonicCommand => _mnemonicCommand ??= (_mnemonicCommand = ReactiveCommand.Create(() =>
        {
            var entropy = Rand.SecureRandomBytes(_entropyLength / 8);

            Mnemonic = new Mnemonic(_language, entropy).ToString();
            Warning = string.Empty;
        }));

        public override void Initialize(
            object arg)
        {
            StepData = (StepData) arg;
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