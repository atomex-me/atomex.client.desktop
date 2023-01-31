using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using Atomex.Wallet.Abstract;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Dialogs;
using Atomex.Client.Desktop.ViewModels.Abstract;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class NavigatableSelectAddress : ViewModelBase
    {
        public Action BackAction { get; set; }
    }

    public class SelectAddressViewModel : NavigatableSelectAddress, IDialogViewModel
    {
        public Action<WalletAddressViewModel> ConfirmAction { get; set; }
        public SelectAddressMode SelectAddressMode { get; set; }
        private CurrencyConfig Currency { get; set; }
        private ObservableCollection<WalletAddressViewModel> InitialMyAddresses { get; set; }
        [Reactive] public ObservableCollection<WalletAddressViewModel> MyAddresses { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] private bool SortIsAscending { get; set; }
        [Reactive] private bool SortByDate { get; set; }
        [Reactive] public WalletAddressViewModel? SelectedAddress { get; set; }
        [ObservableAsProperty] public bool CanConfirm { get; }
        [ObservableAsProperty] public bool ExternalWarning { get; }
        public Action? OnClose { get; set; }

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
            SelectAddressMode mode = SelectAddressMode.ReceiveTo,
            string? selectedAddress = null,
            int selectedTokenId = 0,
            string? tokenContract = null)
        {
            this.WhenAnyValue(
                    vm => vm.SortByDate,
                    vm => vm.SortIsAscending,
                    vm => vm.SearchPattern)
                .SubscribeInMainThread(value =>
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
                            ? myAddresses.OrderBy(addressViewModel => addressViewModel.Balance)
                            : myAddresses.OrderByDescending(addressViewModel => addressViewModel.Balance));
                    }
                });

            this.WhenAnyValue(vm => vm.SelectedAddress, vm => vm.SearchPattern)
                .Select(value =>
                {
                    var (address, searchPattern) = value;
                    if (SelectAddressMode == SelectAddressMode.SendFrom)
                    {
                        return address != null;
                    }

                    return currency.IsValidAddress(address?.Address ?? searchPattern);
                })
                .ToPropertyExInMainThread(this, vm => vm.CanConfirm);

            this.WhenAnyValue(vm => vm.SearchPattern)
                .Where(_ => MyAddresses != null)
                .Select(searchPattern =>
                {
                    if (SelectAddressMode != SelectAddressMode.SendFrom && !string.IsNullOrEmpty(searchPattern))
                    {
                        return MyAddresses!.Count == 0 && currency.IsValidAddress(searchPattern);
                    }

                    return false;
                })
                .ToPropertyExInMainThread(this, vm => vm.ExternalWarning);

            Currency = currency;
            SelectAddressMode = mode;
            var onlyAddressesWithBalances = SelectAddressMode is SelectAddressMode.SendFrom;

            var addresses = AddressesHelper
                .GetReceivingAddressesAsync(
                    account: account,
                    currency: currency,
                    tokenContract: tokenContract,
                    tokenId: selectedTokenId)
                .WaitForResult()
                .Where(address => !onlyAddressesWithBalances || address.Balance != 0)
                .OrderByDescending(address => address.Balance);

            MyAddresses = new ObservableCollection<WalletAddressViewModel>(addresses);
            InitialMyAddresses = new ObservableCollection<WalletAddressViewModel>(addresses);

            SelectedAddress = selectedAddress != null
                ? MyAddresses.FirstOrDefault(vm =>
                    vm.Address == selectedAddress && vm.TokenId == selectedTokenId)
                : SelectAddressMode is SelectAddressMode.SendFrom or SelectAddressMode.Connect
                    ? SelectDefaultAddress()
                    : null;
        }

        public WalletAddressViewModel? SelectDefaultAddress()
        {
            if (Currency is TezosConfig or EthereumConfig)
            {
                var activeAddressViewModel = MyAddresses
                    .Where(vm => vm.HasActivity && vm.AvailableBalance > 0)
                    .MaxByOrDefault(vm => vm.AvailableBalance);

                if (activeAddressViewModel != null)
                {
                    SelectedAddress = activeAddressViewModel;
                }
                else
                {
                    SelectedAddress = MyAddresses.FirstOrDefault(vm => vm.IsFreeAddress) ??
                                      MyAddresses.FirstOrDefault();
                }
            }
            else
            {
                SelectedAddress = MyAddresses.FirstOrDefault(vm => vm.IsFreeAddress) ?? MyAddresses.FirstOrDefault();
            }

            return SelectedAddress;
        }

        private ReactiveCommand<Unit, Unit>? _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??=
            (_backCommand = ReactiveCommand.Create(() => { BackAction?.Invoke(); }));

        private ReactiveCommand<Unit, Unit>? _changeSortTypeCommand;

        public ReactiveCommand<Unit, Unit> ChangeSortTypeCommand => _changeSortTypeCommand ??=
            (_changeSortTypeCommand = ReactiveCommand.Create(() => { SortByDate = !SortByDate; }));

        private ReactiveCommand<Unit, Unit>? _changeSortDirectionCommand;

        public ReactiveCommand<Unit, Unit> ChangeSortDirectionCommand => _changeSortDirectionCommand ??=
            (_changeSortDirectionCommand = ReactiveCommand.Create(() => { SortIsAscending = !SortIsAscending; }));

        private ReactiveCommand<Unit, Unit>? _confirmCommand;

        public ReactiveCommand<Unit, Unit> ConfirmCommand => _confirmCommand ??=
            (_confirmCommand = ReactiveCommand.Create(() =>
            {
                var selectedAddress = SelectedAddress ?? new WalletAddressViewModel
                {
                    Address = SearchPattern,
                    AvailableBalance = 0,
                    TokenId = 0
                };

                ConfirmAction?.Invoke(selectedAddress);
            }));

        private ICommand? _copyAddressCommand;
        public ICommand CopyAddressCommand =>
            _copyAddressCommand ??= (_copyAddressCommand = ReactiveCommand.Create((WalletAddress address) =>
            {
                _ = App.Clipboard.SetTextAsync(address.Address);
            }));
#if DEBUG
        private void DesignerMode()
        {
        }
#endif
    }
}