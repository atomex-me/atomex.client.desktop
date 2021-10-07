using System;
using System.Windows.Input;
using Atomex.Core;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class SwapDetailsViewModel : ViewModelBase
    {
        public Action? OnClose { get; set; }
        public Swap Swap { get; set; }
        public string SwapId => Swap.Id.ToString();
        
        
        private ICommand? _closeCommand;
        public ICommand CloseCommand => _closeCommand ??= (_closeCommand = ReactiveCommand.Create(() =>
        {
            OnClose?.Invoke();
        }));
    }
}