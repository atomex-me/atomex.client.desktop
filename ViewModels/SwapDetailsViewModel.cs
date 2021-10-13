using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Core;
using DynamicData;
using ReactiveUI;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels
{
    public class SwapDetailsViewModel : ViewModelBase
    {
        public Action? OnClose { get; set; }
        public string SwapId { get; set; }
        public decimal Price { get; set; }
        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public string FromAmountFormat => FromCurrencyViewModel.CurrencyFormat;
        public string ToAmountFormat => ToCurrencyViewModel.CurrencyFormat;
        public string FromCurrencyCode => FromCurrencyViewModel.CurrencyCode;
        public string ToCurrencyCode => ToCurrencyViewModel.CurrencyCode;


        private ICommand? _closeCommand;
        public ICommand CloseCommand => _closeCommand ??= _closeCommand = ReactiveCommand.Create(() =>
        {
            OnClose?.Invoke();
        });
    }
}