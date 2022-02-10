using System;
using System.Collections.Generic;
using ReactiveUI;
using System.Threading.Tasks;
using System.Security;
using System.Linq;
using Atomex.Common;
using System.Threading;

namespace Atomex.Client.Desktop.ViewModels
{
    public class TextboxState
    {
        public TextboxState(int selectionStart, int selectionEnd, int caretIndex, string value)
        {
            SelectionStart = selectionStart;
            SelectionEnd = selectionEnd;
            CaretIndex = caretIndex;
            Value = value;
        }

        public int SelectionStart { get; }
        public int SelectionEnd { get; }
        public int CaretIndex { get; }
        public string Value { get; }
    }

    public class PasswordControlViewModel : ViewModelBase
    {
        public PasswordControlViewModel()
        {
        }

        public PasswordControlViewModel(
            Action onPasswordChanged = null,
            string placeholder = "Password",
            bool isSmall = false
        )
        {
            if (onPasswordChanged != null)
            {
                _onPasswordChanged += onPasswordChanged;
            }

            Placeholder = placeholder;
            IsSmall = isSmall;
        }

        private readonly Action _onPasswordChanged;
        public string Placeholder { get; set; }
        public bool IsSmall { get; set; }

        public bool IsFocused { get; set; }
        public int SelectionStart { get; set; }
        public int SelectionEnd { get; set; }
        public int CaretIndex { get; set; }

        private string _stringPass = string.Empty;

        public string StringPass
        {
            get => _stringPass;
            set
            {
                _stringPass = value;
                _onPasswordChanged?.Invoke();
            }
        }

        // public SecureString SecurePass = new SecureString();
        public SecureString SecurePass => StringPass.ToSecureString();

        // private List<TextboxState> valueQueue = new List<TextboxState>();
        // private bool _handling = false;

        // private async Task HandleValue(TextboxState state)
        // {
        //     _handling = true;
        //     await Task.Run(async () =>
        //     {
        //         await Task.Delay(1, new CancellationTokenSource().Token);
        //
        //
        //         var diffLen = state.Value.Length - StringPass.Length;
        //
        //         // delete all text
        //         if (state.Value.Length == 0)
        //         {
        //             SecurePass.Clear();
        //             StringPass = string.Empty;
        //
        //             OnPasswordChanged?.Invoke();
        //             this.RaisePropertyChanged(nameof(StringPass));
        //             return;
        //         }
        //
        //         if (diffLen == -1)
        //         {
        //             var startIndex = state.CaretIndex - 1;
        //             if (state.SelectionStart != state.SelectionEnd)
        //             {
        //                 startIndex = state.CaretIndex;
        //             }
        //
        //             SecurePass.RemoveAt(startIndex);
        //         }
        //
        //         if (diffLen < -1)
        //         {
        //             for (var i = 0; i < Math.Abs(diffLen); i++)
        //             {
        //                 SecurePass.RemoveAt(state.CaretIndex);
        //             }
        //         }
        //
        //         if (diffLen == 1 && state.CaretIndex == StringPass.Length)
        //         {
        //             var lastStringChar = state.Value.Last();
        //             SecurePass.AppendChar(lastStringChar);
        //             CaretIndex = state.Value.Length;
        //         }
        //
        //         if (diffLen == 1 && state.CaretIndex < StringPass.Length)
        //         {
        //             var lastStringChar = state.Value[state.CaretIndex];
        //             SecurePass.InsertAt(state.CaretIndex, lastStringChar);
        //         }
        //
        //         var randomStringPass = RandomString(state.Value.Length);
        //         StringPass = randomStringPass;
        //
        //         OnPasswordChanged?.Invoke();
        //
        //         this.RaisePropertyChanged(nameof(StringPass));
        //         this.RaisePropertyChanged(nameof(CaretIndex));
        //     });
        //
        //     if (valueQueue.Count > 0)
        //     {
        //         var val = valueQueue[0];
        //         valueQueue.RemoveAt(0);
        //         await HandleValue(val);
        //     }
        //
        //     _handling = false;
        // }

        // private Random random = new Random();
        // private string RandomString(int length)
        // {
        //     const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        //     return new string(Enumerable.Repeat(chars, length)
        //         .Select(s => s[random.Next(s.Length)]).ToArray());
        // }
    }
}