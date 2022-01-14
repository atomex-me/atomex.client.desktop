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
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;

namespace Atomex.Client.Desktop.ViewModels.ConversionViewModels
{
    public class SelectCurrencyViewModelItem : ViewModelBase
    {
        [Reactive] public CurrencyViewModel CurrencyViewModel { get; set; }
        [Reactive] public WalletAddress Address { get; set; }
        [Reactive] public IEnumerable<BitcoinBasedTxOutput> Outputs { get; set; }
        [ObservableAsProperty] public string SelectAddressDescription { get; }

        public SelectCurrencyViewModelItem(CurrencyViewModel currencyViewModel)
        {
            CurrencyViewModel = currencyViewModel ?? throw new ArgumentNullException(nameof(currencyViewModel));

            this.WhenAnyValue(vm => vm.Address, vm => vm.Outputs)
                .Select(i =>
                {
                    if (i.Item1 != null)
                    {
                        return $"from {i.Item1.Address.TruncateAddress()} ({i.Item1.Balance} {CurrencyViewModel.Currency.Name})";
                    }
                    else if (i.Item2 != null)
                    {
                        var currency = (BitcoinBasedConfig)CurrencyViewModel.Currency;
                        var totalOutputsValue = currency.SatoshiToCoin(i.Item2.Sum(o => o.Value));

                        return $"from {i.Item2.Count()} outputs ({totalOutputsValue} {currency.Name})";
                    }
                    else return null;
                })
                .WhereNotNull()
                .ToPropertyEx(this, vm => vm.SelectAddressDescription);
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

            if (Atomex.Currencies.IsBitcoinBased(currency.Name))
            {
                var bitcoinBasedAccount = _account
                    .GetCurrencyAccount<BitcoinBasedAccount>(currency.Name);

                var outputs = (await bitcoinBasedAccount
                    .GetAvailableOutputsAsync())
                    .Cast<BitcoinBasedTxOutput>();

                var selectOutputsViewModel = new SelectOutputsViewModel(
                    outputs: outputs,
                    config: (BitcoinBasedConfig)currency,
                    account: bitcoinBasedAccount)
                {
                    BackAction = () => { App.DialogService.Show(this); },
                    ConfirmAction = ots =>
                    {
                        i.Outputs = ots;
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

        public SelectCurrencyViewModel(IAccount account, string title)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));

            Title = title ?? throw new ArgumentNullException(nameof(title));

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
                .Select(c =>
                {
                    if (Atomex.Currencies.IsBitcoinBased(c.Name))
                    {
                        return new SelectCurrencyViewModelItem(CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                        {
                            Outputs = new List<BitcoinBasedTxOutput>
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
                            }
                        };
                    }
                    else
                    {
                        return new SelectCurrencyViewModelItem(CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                        {
                            Address = new WalletAddress {
                                Address = "0xE9C251cbB4881f9e056e40135E7d3EA9A7d037df",
                                Balance = 1.2m
                            }
                        };
                    }
                 });

            Currencies = new ObservableCollection<SelectCurrencyViewModelItem>(currencies);
        }
#endif
    }
}