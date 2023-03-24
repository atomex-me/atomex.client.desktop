using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

using Avalonia.Controls;
using NBitcoin;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Network = NBitcoin.Network;

using Atomex.Blockchain;
using Atomex.Blockchain.Bitcoin;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Common;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;
using Atomex.Wallets;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.ConversionViewModels
{
    public abstract class SelectCurrencyViewModelItem : ViewModelBase
    {
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [ObservableAsProperty] public string? SelectedAddressDescription { get; }
        public abstract string? ShortAddressDescription { get; }
        public abstract IFromSource? FromSource { get; }

        public SelectCurrencyViewModelItem(CurrencyViewModel currencyViewModel)
        {
            CurrencyViewModel = currencyViewModel ?? throw new ArgumentNullException(nameof(currencyViewModel));
        }
    }

    public class SelectCurrencyWithOutputsViewModelItem : SelectCurrencyViewModelItem
    {
        public IEnumerable<BitcoinTxOutput> AvailableOutputs { get; set; }
        [Reactive] public IEnumerable<BitcoinTxOutput> SelectedOutputs { get; set; }
        public override string? ShortAddressDescription => $"{SelectedOutputs?.Count() ?? 0} outputs";
        public override IFromSource? FromSource => SelectedOutputs != null
            ? new FromOutputs(SelectedOutputs)
            : null;

        public SelectCurrencyWithOutputsViewModelItem(
            CurrencyViewModel currencyViewModel,
            IEnumerable<BitcoinTxOutput> availableOutputs,
            IEnumerable<BitcoinTxOutput>? selectedOutputs = null)
            : base(currencyViewModel)
        {
            this.WhenAnyValue(vm => vm.SelectedOutputs)
                .WhereNotNull()
                .Select(outputs =>
                {
                    var currency             = (BitcoinBasedConfig)CurrencyViewModel.Currency;
                    var totalAmountInSatoshi = outputs.Sum(o => o.Value);
                    var totalAmount          = currency.SatoshiToCoin(totalAmountInSatoshi);
                    var totalAmountString    = totalAmount.ToString(CurrencyViewModel.CurrencyFormat);

                    return $"from {outputs.Count()} outputs ({totalAmountString} {currency.DisplayedName})";
                })
                .ToPropertyExInMainThread(this, vm => vm.SelectedAddressDescription);

            AvailableOutputs = availableOutputs ?? throw new ArgumentNullException(nameof(availableOutputs));
            SelectedOutputs = selectedOutputs ?? availableOutputs;
        }

        public void TryInitializeFromItem(SelectCurrencyViewModelItem source)
        {
            if (source is not SelectCurrencyWithOutputsViewModelItem sourceWithOutputs)
                return;

            var selectedOutputs = new List<BitcoinTxOutput>();

            foreach (var output in sourceWithOutputs.SelectedOutputs)
            {
                var selectedOutput = AvailableOutputs
                    .FirstOrDefault(o => o.TxId == output.TxId && o.Index == output.Index && o.Value == output.Value);

                if (selectedOutput == null)
                {
                    selectedOutputs.Clear();
                    return;
                }

                selectedOutputs.Add(selectedOutput);
            }

            SelectedOutputs = selectedOutputs;
        }
    }

    public class SelectCurrencyWithAddressViewModelItem : SelectCurrencyViewModelItem
    {
        private readonly SelectCurrencyType _type;

        public IEnumerable<WalletAddress> AvailableAddresses { get; set; }
        [Reactive] public WalletAddress? SelectedAddress { get; set; }
        [Reactive] public bool IsNew { get; set; }
        public override string? ShortAddressDescription => SelectedAddress?.Address?.TruncateAddress();
        public override IFromSource? FromSource => SelectedAddress?.Address != null
            ? new FromAddress(SelectedAddress.Address)
            : null;

        public SelectCurrencyWithAddressViewModelItem(
            CurrencyViewModel currencyViewModel,
            SelectCurrencyType type,
            IEnumerable<WalletAddress> availableAddresses,
            WalletAddress? selectedAddress = null)
            : base(currencyViewModel)
        {
            _type = type;

            this.WhenAnyValue(vm => vm.SelectedAddress)
                .WhereNotNull()
                .Select(address =>
                {
                    if (_type == SelectCurrencyType.To && IsNew)
                        return $"receiving to new address {address.Address.TruncateAddress()}";

                    var prefix = _type == SelectCurrencyType.From ? "from" : "to";
                    var balanceString = address.Balance
                        .FromTokens(CurrencyViewModel.Currency.Decimals)
                        .ToString(CurrencyViewModel.CurrencyFormat);

                    return $"{prefix} {address.Address.TruncateAddress()} ({balanceString} {CurrencyViewModel.Currency.DisplayedName})";
                })
                .ToPropertyExInMainThread(this, vm => vm.SelectedAddressDescription);

            AvailableAddresses = availableAddresses ?? throw new ArgumentNullException(nameof(availableAddresses));
            SelectedAddress = selectedAddress ?? availableAddresses.MaxByOrDefault(w => w.Balance);
        }

        public void TryInitializeFromItem(SelectCurrencyViewModelItem source)
        {
            if (source is not SelectCurrencyWithAddressViewModelItem sourceWithAddress)
                return;

            var address = AvailableAddresses
                .FirstOrDefault(a => a.Address == sourceWithAddress.ShortAddressDescription);

            if (address != null)
                SelectedAddress = address;
        }
    }

    public class SelectCurrencyViewModel : ViewModelBase
    {
        public Action<SelectCurrencyViewModelItem> CurrencySelected;

        [Reactive] public ObservableCollection<SelectCurrencyViewModelItem> Currencies { get; set; }
        [Reactive] public SelectCurrencyViewModelItem? SelectedCurrency { get; set; }
        [Reactive] public SelectCurrencyType Type { get; set; }

        private ICommand _changeAddressesCommand;
        public ICommand ChangeAddressesCommand => _changeAddressesCommand ??= ReactiveCommand.Create<SelectCurrencyViewModelItem>(async i =>
        {
            var currency = i.CurrencyViewModel.Currency;

            if (i is SelectCurrencyWithOutputsViewModelItem itemWithOutputs && Type == SelectCurrencyType.From)
            {
                var bitcoinBasedAccount = _account
                    .GetCurrencyAccount<BitcoinBasedAccount>(currency.Name);

                var outputs = (await bitcoinBasedAccount
                    .GetAvailableOutputsAsync())
                    .Cast<BitcoinTxOutput>();

                var selectOutputsViewModel = new SelectOutputsViewModel(
                    outputs: outputs.Select(o => new OutputViewModel()
                    {
                        Output = o,
                        Config = (BitcoinBasedConfig)currency,
                        IsSelected = itemWithOutputs?.SelectedOutputs?.Any(so => so.TxId == o.TxId && so.Index == o.Index) ?? true
                    }),
                    config: (BitcoinBasedConfig)currency,
                    account: bitcoinBasedAccount)
                {
                    BackAction = () => { App.DialogService.Show(this); },
                    ConfirmAction = ots =>
                    {
                        itemWithOutputs.SelectedOutputs = ots;
                        CurrencySelected?.Invoke(i);
                    }
                };
                
                App.DialogService.Show(selectOutputsViewModel);
            }
            else if (i is SelectCurrencyWithAddressViewModelItem itemWithAddress)
            {
                var selectAddressViewModel = new SelectAddressViewModel(
                    account: _account,
                    localStorage: _localStorage,
                    currency: currency,
                    mode: Type == SelectCurrencyType.From ? SelectAddressMode.SendFrom : SelectAddressMode.ReceiveTo,
                    selectedAddress: itemWithAddress.SelectedAddress?.Address)
                {
                    BackAction = () => { App.DialogService.Show(this); },
                    ConfirmAction = walletAddressViewModel =>
                    {
                        var selectedAvaialbleAddress = itemWithAddress
                            .AvailableAddresses
                            .FirstOrDefault(a => a.Address == walletAddressViewModel.Address);

                        if (Type == SelectCurrencyType.From)
                        {
                            itemWithAddress.SelectedAddress = selectedAvaialbleAddress ?? itemWithAddress.SelectedAddress;
                        }
                        else
                        {
                            itemWithAddress.SelectedAddress = selectedAvaialbleAddress ?? new WalletAddress
                            {
                                Address = walletAddressViewModel.Address,
                                Currency = currency.Name
                            };
                        }

                        CurrencySelected?.Invoke(i);
                    }
                };

                App.DialogService.Show(selectAddressViewModel);
            }
        });

        private readonly IAccount _account;
        private readonly ILocalStorage _localStorage;

        public SelectCurrencyViewModel(
            IAccount account,
            ILocalStorage localStorage,
            SelectCurrencyType type,
            IEnumerable<SelectCurrencyViewModelItem> currencies,
            SelectCurrencyViewModelItem? selected = null)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));

            Type = type;
            Currencies = new ObservableCollection<SelectCurrencyViewModelItem>(currencies);
            
            if (selected != null)
            {
                var selectedCurrencyViewItem = Currencies
                    .FirstOrDefault(c => c.CurrencyViewModel.Currency.Name == selected.CurrencyViewModel.Currency.Name);

                if (selectedCurrencyViewItem is SelectCurrencyWithOutputsViewModelItem selectedCurrencyWithOutputs)
                {
                    selectedCurrencyWithOutputs.TryInitializeFromItem(selected);
                }
                else if (selectedCurrencyViewItem is SelectCurrencyWithAddressViewModelItem selectedCurrencyWithAddress)
                {
                    selectedCurrencyWithAddress.TryInitializeFromItem(selected);
                }

                SelectedCurrency = selectedCurrencyViewItem;
            }

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .SubscribeInMainThread(i => {
                    CurrencySelected?.Invoke(i);
                });
        }

#if DEBUG
        public SelectCurrencyViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();
        }
#endif

#if DEBUG
        public void DesignerMode()
        {
            Type = SelectCurrencyType.From;

            var currencies = DesignTime.TestNetCurrencies
                .Select<CurrencyConfig, SelectCurrencyViewModelItem>(c =>
                {
                    if (Atomex.Currencies.IsBitcoinBased(c.Name))
                    {
                        var outputs = new List<BitcoinTxOutput>
                        {
                            new BitcoinTxOutput
                            {
                                Coin = new Coin(
                                    fromTxHash: new uint256("19aa2187cda7610590d09dfab41ed4720f8570d7414b71b3dc677e237f72d4a1"),
                                    fromOutputIndex: 0u,
                                    amount: Money.Satoshis(1234567),
                                    scriptPubKey: BitcoinAddress.Create("muRDku2ZwNTz2msCZCHSUhDD5o6NxGsoXM", Network.TestNet).ScriptPubKey),
                                Currency = "BTC",
                                IsConfirmed = true,
                            },
                            new BitcoinTxOutput
                            {
                                Coin = new Coin(
                                    fromTxHash: new uint256("d70fa62762775362e767737e56cab7e8a094eafa8f96b935530d6450be1cfbce"),
                                    fromOutputIndex: 0u,
                                    amount: Money.Satoshis(100120000),
                                    scriptPubKey: BitcoinAddress.Create("mg8DcFTnNAJRHEZ248nVjeJuEjTsHn4vrZ", Network.TestNet).ScriptPubKey),
                                Currency = "BTC",
                                IsConfirmed = true,
                            }
                        };

                        return new SelectCurrencyWithOutputsViewModelItem(
                            currencyViewModel: CurrencyViewModelCreator.CreateOrGet(c, subscribeToUpdates: false),
                            availableOutputs: outputs,
                            selectedOutputs: outputs);
                    }
                    else
                    {
                        var address = new WalletAddress
                        {
                            Address = "0xE9C251cbB4881f9e056e40135E7d3EA9A7d037df",
                            Balance = 120000000
                        };

                        return new SelectCurrencyWithAddressViewModelItem(
                            currencyViewModel: CurrencyViewModelCreator.CreateOrGet(c, subscribeToUpdates: false),
                            type: SelectCurrencyType.From,
                            availableAddresses: new WalletAddress[] { address },
                            selectedAddress: address);
                    }
                 });

            Currencies = new ObservableCollection<SelectCurrencyViewModelItem>(currencies);

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .SubscribeInMainThread(i => {
                    CurrencySelected?.Invoke(i);
                });
        }
#endif
    }
}