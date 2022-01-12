using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class SelectToViewModel : ViewModelBase
    {
        public Action BackAction { get; set; }
        [Reactive] public List<WalletAddressViewModel> FromAddressList { get; set; }
        [Reactive] public string SRT { get; set; }

        public SelectToViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public SelectToViewModel(IAccount account, CurrencyConfig currency)
        {
            // get all addresses with tokens (if exists)
            var tokenAddresses = Currencies.HasTokens(currency.Name)
                ? (account.GetCurrencyAccount(currency.Name) as IHasTokens)
                ?.GetUnspentTokenAddressesAsync()
                .WaitForResult() ?? new List<WalletAddress>()
                : new List<WalletAddress>();

            // get all active addresses
            var activeAddresses = account
                .GetUnspentAddressesAsync(currency.Name)
                .WaitForResult()
                .ToList();

            // get free external address
            var freeAddress = account
                .GetFreeExternalAddressAsync(currency.Name)
                .WaitForResult();

            FromAddressList = activeAddresses
                .Concat(tokenAddresses)
                .Concat(new [] { freeAddress })
                .GroupBy(w => w.Address)
                .Select(g =>
                {
                    // main address
                    var address = g.FirstOrDefault(w => w.Currency == currency.Name);

                    var isFreeAddress = address?.Address == freeAddress.Address;

                    var hasTokens = g.Any(w => w.Currency != currency.Name);

                    return new WalletAddressViewModel
                    {
                        Address = g.Key,
                        HasActivity = address?.HasActivity ?? hasTokens,
                        AvailableBalance = address?.AvailableBalance() ?? 0m,
                        CurrencyFormat = currency.Format,
                        CurrencyCode = currency.Name,
                        IsFreeAddress = isFreeAddress
                    };
                }).ToList();

            var a = 5;
        }

        private ReactiveCommand<Unit, Unit> _backCommand;
        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??=
            (_backCommand = ReactiveCommand.Create(() => { BackAction?.Invoke(); }));

        private void DesignerMode()
        {
        }
    }
}