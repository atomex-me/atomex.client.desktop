using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class SelectToViewModel : ViewModelBase
    {
        public Action BackAction { get; set; }
        public Action<string> ConfirmAction { get; set; }

        private ObservableCollection<AddressViewModel> InitialMyAddresses { get; set; }
        [Reactive] public ObservableCollection<AddressViewModel> MyAddresses { get; set; }
        [Reactive] public string SearchPattern { get; set; }
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

            var myAddresses = activeAddresses
                .Concat(tokenAddresses)
                .Concat(new[] { freeAddress })
                .GroupBy(w => w.Address)
                .Select(g =>
                {
                    // main address
                    var address = g.FirstOrDefault(w => w.Currency == currency.Name);

                    var isFreeAddress = address?.Address == freeAddress.Address;

                    return new AddressViewModel()
                    {
                        WalletAddress = address,
                        AvailableBalance = address?.AvailableBalance() ?? 0m,
                        CurrencyFormat = currency.Format,
                        CurrencyCode = currency.Name,
                        IsFreeAddress = isFreeAddress
                    };
                });

            MyAddresses = new ObservableCollection<AddressViewModel>(myAddresses);
            InitialMyAddresses = new ObservableCollection<AddressViewModel>(MyAddresses);
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
            (_confirmCommand = ReactiveCommand.Create(() => { ConfirmAction?.Invoke("Lorem ipsum"); }));

        private ICommand _copyAddressCommand;

        public ICommand CopyAddressCommand =>
            _copyAddressCommand ??= (_copyAddressCommand = ReactiveCommand.Create((WalletAddress address) =>
            {
                _ = Desktop.App.Clipboard.SetTextAsync(address.Address);

                // MyAddresses.ForEachDo(o => o.CopyButtonToolTip = AddressViewModel.DefaultCopyButtonToolTip);
                // MyAddresses.First(o => o.WalletAddress.Address == address.Address).CopyButtonToolTip =
                //     AddressViewModel.CopiedButtonToolTip;
            }));

        private void DesignerMode()
        {
        }
    }

    public class AddressViewModel
    {
        public const string DefaultCopyButtonToolTip = "Copy address to clipboard";
        public const string CopiedButtonToolTip = "Successfully copied!";

        public AddressViewModel()
        {
            CopyButtonToolTip = DefaultCopyButtonToolTip;
        }

        [Reactive] public string CopyButtonToolTip { get; set; }
        public WalletAddress WalletAddress { get; set; }
        public decimal AvailableBalance { get; set; }
        public string CurrencyFormat { get; set; }
        public string CurrencyCode { get; set; }
        public bool IsFreeAddress { get; set; }
    }
}