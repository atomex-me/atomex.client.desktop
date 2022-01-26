using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class SelectAddressViewModel : ViewModelBase
    {
        public Action BackAction { get; set; }
        public Action<string, decimal> ConfirmAction { get; set; }
        public bool UseToSelectFrom { get; set; }
        private ObservableCollection<WalletAddressViewModel> InitialMyAddresses { get; set; }
        [Reactive] public ObservableCollection<WalletAddressViewModel> MyAddresses { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] private bool SortIsAscending { get; set; }
        [Reactive] private bool SortByDate { get; set; }
        [Reactive] public WalletAddressViewModel? SelectedAddress { get; set; }

        public SelectAddressViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public SelectAddressViewModel(
            IAccount account,
            CurrencyConfig currency,
            bool useToSelectFrom = false,
            string selectedAddress = null)
        {
            this.WhenAnyValue(
                    vm => vm.SortByDate,
                    vm => vm.SortIsAscending,
                    vm => vm.SearchPattern)
                .Subscribe(value =>
                {
                    var (sortByDate, sortByAscending, searchPattern) = value;

                    if (MyAddresses == null) return;

                    var myAddresses = new ObservableCollection<WalletAddressViewModel>(
                        InitialMyAddresses
                            .Where(addressViewModel => addressViewModel.WalletAddress.Address.ToLower()
                                .Contains(searchPattern?.ToLower() ?? string.Empty)));

                    if (sortByDate)
                    {
                        var myAddressesList = myAddresses.ToList();
                        if (sortByAscending)
                        {
                            myAddressesList.Sort((a1, a2) =>
                            {
                                var typeResult = a1.WalletAddress.KeyType.CompareTo(a2.WalletAddress.KeyType);

                                if (typeResult != 0)
                                    return typeResult;

                                var accountResult =
                                    a1.WalletAddress.KeyIndex.Account.CompareTo(a2.WalletAddress.KeyIndex.Account);

                                if (accountResult != 0)
                                    return accountResult;

                                var chainResult =
                                    a1.WalletAddress.KeyIndex.Chain.CompareTo(a2.WalletAddress.KeyIndex.Chain);

                                return chainResult != 0
                                    ? chainResult
                                    : a1.WalletAddress.KeyIndex.Index.CompareTo(a2.WalletAddress.KeyIndex.Index);
                            });
                        }
                        else
                        {
                            myAddressesList.Sort((a2, a1) =>
                            {
                                var typeResult = a1.WalletAddress.KeyType.CompareTo(a2.WalletAddress.KeyType);

                                if (typeResult != 0)
                                    return typeResult;

                                var accountResult =
                                    a1.WalletAddress.KeyIndex.Account.CompareTo(a2.WalletAddress.KeyIndex.Account);

                                if (accountResult != 0)
                                    return accountResult;

                                var chainResult =
                                    a1.WalletAddress.KeyIndex.Chain.CompareTo(a2.WalletAddress.KeyIndex.Chain);

                                return chainResult != 0
                                    ? chainResult
                                    : a1.WalletAddress.KeyIndex.Index.CompareTo(a2.WalletAddress.KeyIndex.Index);
                            });
                        }

                        MyAddresses = new ObservableCollection<WalletAddressViewModel>(myAddressesList);
                    }
                    else
                    {
                        MyAddresses = new ObservableCollection<WalletAddressViewModel>(sortByAscending
                            ? myAddresses.OrderBy(addressViewModel => addressViewModel.AvailableBalance)
                            : myAddresses.OrderByDescending(addressViewModel => addressViewModel.AvailableBalance));
                    }
                });

            UseToSelectFrom = useToSelectFrom;

            var addresses = AddressesHelper
                .GetReceivingAddressesAsync(
                    account: account,
                    currency: currency)
                .WaitForResult()
                .Where(address => !useToSelectFrom ||
                                  address.IsTezosToken && address.TokenBalance != 0 ||
                                  !address.IsTezosToken && address.AvailableBalance != 0)
                .OrderByDescending(address => address.AvailableBalance);

            MyAddresses = new ObservableCollection<WalletAddressViewModel>(addresses);
            InitialMyAddresses = new ObservableCollection<WalletAddressViewModel>(addresses);

            if (selectedAddress != null)
                SelectedAddress = MyAddresses.FirstOrDefault(vm => vm.Address == selectedAddress);
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
                var selectedAddress = SelectedAddress == null
                    ? SearchPattern
                    : SelectedAddress.WalletAddress.Address;

                var balance = SelectedAddress == null
                    ? 0m
                    : SelectedAddress.WalletAddress.AvailableBalance();

                ConfirmAction?.Invoke(selectedAddress, balance);
            }));

        private ICommand _copyAddressCommand;

        public ICommand CopyAddressCommand =>
            _copyAddressCommand ??= (_copyAddressCommand = ReactiveCommand.Create((WalletAddress address) =>
            {
                _ = App.Clipboard.SetTextAsync(address.Address);

                // MyAddresses.ForEachDo(o => o.CopyButtonToolTip = AddressViewModel.DefaultCopyButtonToolTip);
                // MyAddresses.First(o => o.WalletAddress.Address == address.Address).CopyButtonToolTip =
                //     AddressViewModel.CopiedButtonToolTip;
            }));

        private void DesignerMode()
        {
        }
    }
}