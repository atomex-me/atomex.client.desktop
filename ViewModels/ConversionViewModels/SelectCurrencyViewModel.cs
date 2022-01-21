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

using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;

namespace Atomex.Client.Desktop.ViewModels.ConversionViewModels
{
    public enum SelectCurrencyType
    {
        From,
        To
    }

    public abstract class SelectCurrencyViewModelItem : ViewModelBase
    {
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [ObservableAsProperty] public string? SelectedAddressDescription { get; }
        public abstract string Address { get; }

        public SelectCurrencyViewModelItem(CurrencyViewModel currencyViewModel)
        {
            CurrencyViewModel = currencyViewModel ?? throw new ArgumentNullException(nameof(currencyViewModel));
        }
    }

    public class SelectCurrencyWithOutputsViewModelItem : SelectCurrencyViewModelItem
    {
        public IEnumerable<BitcoinBasedTxOutput> AvailableOutputs { get; set; }
        [Reactive] public IEnumerable<BitcoinBasedTxOutput> SelectedOutputs { get; set; }
        public override string Address => $"{SelectedOutputs?.Count() ?? 0} outputs";

        public SelectCurrencyWithOutputsViewModelItem(
            CurrencyViewModel currencyViewModel,
            IEnumerable<BitcoinBasedTxOutput> availableOutputs,
            IEnumerable<BitcoinBasedTxOutput>? selectedOutputs = null)
            : base(currencyViewModel)
        {
            this.WhenAnyValue(vm => vm.SelectedOutputs)
                .WhereNotNull()
                .Select(outputs =>
                {
                    var currency = (BitcoinBasedConfig)CurrencyViewModel.Currency;
                    var totalAmountInSatoshi = outputs.Sum(o => o.Value);
                    var totalAmount = currency.SatoshiToCoin(totalAmountInSatoshi);

                    return $"from {outputs.Count()} outputs ({totalAmount} {currency.Name})";
                })
                .ToPropertyEx(this, vm => vm.SelectedAddressDescription);

            AvailableOutputs = availableOutputs ?? throw new ArgumentNullException(nameof(availableOutputs));
            SelectedOutputs = selectedOutputs ?? availableOutputs;
        }

        public void TryInitializeFromItem(SelectCurrencyViewModelItem source)
        {
            if (source is not SelectCurrencyWithOutputsViewModelItem sourceWithOutputs)
                return;

            var selectedOutputs = new List<BitcoinBasedTxOutput>();

            foreach (var output in sourceWithOutputs.SelectedOutputs)
            {
                var selectedOutput = AvailableOutputs.FirstOrDefault(o => o.TxId == output.TxId && o.Index == output.Index && o.Value == output.Value);

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
        [Reactive] public WalletAddress SelectedAddress { get; set; }
        [Reactive] public bool IsNew { get; set; }
        public override string Address => SelectedAddress?.Address?.TruncateAddress();

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
                    return $"{prefix} {address.Address.TruncateAddress()} ({address.Balance} {CurrencyViewModel.Currency.Name})";
                })
                .ToPropertyEx(this, vm => vm.SelectedAddressDescription);

            AvailableAddresses = availableAddresses ?? throw new ArgumentNullException(nameof(availableAddresses));
            SelectedAddress = selectedAddress ?? availableAddresses.MaxBy(w => w.Balance);
        }

        public void TryInitializeFromItem(SelectCurrencyViewModelItem source)
        {
            if (source is not SelectCurrencyWithAddressViewModelItem sourceWithAddress)
                return;

            var address = AvailableAddresses.FirstOrDefault(a => a.Address == sourceWithAddress.Address);

            if (address != null)
                SelectedAddress = address;
        }
    }

    public class SelectCurrencyViewModel : ViewModelBase
    {
        public Action<SelectCurrencyViewModelItem> CurrencySelected;

        [Reactive] public ObservableCollection<SelectCurrencyViewModelItem> Currencies { get; set; }
        [Reactive] public SelectCurrencyViewModelItem SelectedCurrency { get; set; }
        [Reactive] public string Title { get; set; }

        private ICommand _changeAddressesCommand;
        public ICommand ChangeAddressesCommand => _changeAddressesCommand ??= ReactiveCommand.Create<SelectCurrencyViewModelItem>(async i =>
        {
            var currency = i.CurrencyViewModel.Currency;

            if (i is SelectCurrencyWithOutputsViewModelItem item)
            {
                var bitcoinBasedAccount = _account
                    .GetCurrencyAccount<BitcoinBasedAccount>(currency.Name);

                var outputs = (await bitcoinBasedAccount
                    .GetAvailableOutputsAsync())
                    .Cast<BitcoinBasedTxOutput>();

                var selectOutputsViewModel = new SelectOutputsViewModel(
                    outputs: outputs.Select(o => new OutputViewModel()
                    {
                        Output = o,
                        Config = (BitcoinBasedConfig)currency,
                        IsSelected = item?.SelectedOutputs?.Any(so => so.TxId == o.TxId && so.Index == o.Index) ?? true
                    }),
                    config: (BitcoinBasedConfig)currency,
                    account: bitcoinBasedAccount)
                {
                    BackAction = () => { App.DialogService.Show(this); },
                    ConfirmAction = ots =>
                    {
                        item.SelectedOutputs = ots;
                        CurrencySelected?.Invoke(i);
                    }
                };
                
                App.DialogService.Show(selectOutputsViewModel);
            }
            else
            {
                // todo: show select address menu
            }
        });

        private readonly IAccount _account;

        public SelectCurrencyViewModel(
            IAccount account,
            string title,
            IEnumerable<SelectCurrencyViewModelItem> currencies,
            SelectCurrencyViewModelItem selected = null)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));

            Title = title ?? throw new ArgumentNullException(nameof(title));
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
                .Subscribe(i => {
                    CurrencySelected?.Invoke(i);
                });
        }

#if DEBUG
        public SelectCurrencyViewModel()
        {
            if (Design.IsDesignMode)
                DesignerMode();

            this.WhenAnyValue(vm => vm.SelectedCurrency)
                .WhereNotNull()
                .Subscribe(i => {
                    CurrencySelected?.Invoke(i);
                });
        }
#endif

#if DEBUG
        public void DesignerMode()
        {
            Title = "Send from";

            var currencies = DesignTime.Currencies
                .Select<CurrencyConfig, SelectCurrencyViewModelItem>(c =>
                {
                    if (Atomex.Currencies.IsBitcoinBased(c.Name))
                    {
                        var outputs = new List<BitcoinBasedTxOutput>
                        {
                            new BitcoinBasedTxOutput(
                                coin: new Coin(
                                    fromTxHash: new uint256("19aa2187cda7610590d09dfab41ed4720f8570d7414b71b3dc677e237f72d4a1"),
                                    fromOutputIndex: 0u,
                                    amount: Money.Satoshis(1234567),
                                    scriptPubKey: BitcoinAddress.Create("muRDku2ZwNTz2msCZCHSUhDD5o6NxGsoXM", Network.TestNet).ScriptPubKey),
                                spentTxPoint: null),
                            new BitcoinBasedTxOutput(
                                coin: new Coin(
                                    fromTxHash: new uint256("d70fa62762775362e767737e56cab7e8a094eafa8f96b935530d6450be1cfbce"),
                                    fromOutputIndex: 0u,
                                    amount: Money.Satoshis(100120000),
                                    scriptPubKey: BitcoinAddress.Create("mg8DcFTnNAJRHEZ248nVjeJuEjTsHn4vrZ", Network.TestNet).ScriptPubKey),
                                spentTxPoint: null)
                        };

                        return new SelectCurrencyWithOutputsViewModelItem(
                            currencyViewModel: CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false),
                            availableOutputs: outputs,
                            selectedOutputs: outputs);
                    }
                    else
                    {
                        var address = new WalletAddress
                        {
                            Address = "0xE9C251cbB4881f9e056e40135E7d3EA9A7d037df",
                            Balance = 1.2m
                        };

                        return new SelectCurrencyWithAddressViewModelItem(
                            currencyViewModel: CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false),
                            type: SelectCurrencyType.From,
                            availableAddresses: new WalletAddress[] { address },
                            selectedAddress: address);
                    }
                 });

            Currencies = new ObservableCollection<SelectCurrencyViewModelItem>(currencies);
        }
#endif
    }
}