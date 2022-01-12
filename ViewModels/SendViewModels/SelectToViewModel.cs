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
        public Action<string> ConfirmAction { get; set; }
        [Reactive] private List<WalletAddressViewModel> FromAddressList { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public string SortTypeString { get; set; }
        [Reactive] private bool SortIsAscending { get; set; }
        [Reactive] private bool SortByDate { get; set; }

        public SelectToViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public SelectToViewModel(IAccount account, CurrencyConfig currency)
        {
            this.WhenAnyValue(vm => vm.SortByDate, vm => vm.SortIsAscending)
                .Subscribe(value =>
                {
                    var (item1, item2) = value;

                    SortTypeString = item1 ? "Sort by date" : "Sort by balance";
                });

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
                .Concat(new[] { freeAddress })
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
        }

        private ReactiveCommand<Unit, Unit> _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??=
            (_backCommand = ReactiveCommand.Create(() => { BackAction?.Invoke(); }));

        private ReactiveCommand<Unit, Unit> _changeSortTypeCommand;

        public ReactiveCommand<Unit, Unit> ChangeSortTypeCommand => _changeSortTypeCommand ??=
            (_changeSortTypeCommand = ReactiveCommand.Create(() => { SortByDate = !SortByDate; }));

        private ReactiveCommand<Unit, Unit> _changeSortDirectionCommand;

        public ReactiveCommand<Unit, Unit> ChangeSortDirectionCommand => _changeSortDirectionCommand ??=
            (_changeSortDirectionCommand = ReactiveCommand.Create(() => { SortIsAscending = !SortIsAscending; }));
        
        private ReactiveCommand<Unit, Unit> _confirmCommand;

        public ReactiveCommand<Unit, Unit> ConfirmCommand => _confirmCommand ??=
            (_confirmCommand = ReactiveCommand.Create(() =>
            {
                ConfirmAction?.Invoke("Lorem ipsum");
            }));

        private void DesignerMode()
        {
            SortTypeString = "Sort by balance";
        }
    }
}