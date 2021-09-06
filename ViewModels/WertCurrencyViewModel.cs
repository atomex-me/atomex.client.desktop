using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomex.Client.Desktop.Api;
using Atomex.Client.Desktop.Common;
using Atomex.Core;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Cryptography;
using Avalonia.Media;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WertCurrencyViewModel : ViewModelBase
    {
        // private string ClientId => App.Account.Network == Network.MainNet ? "atomex" : "01F298K3HP4DY326AH1NS3MM3M";
        //
        // private string BuyUrl => $"{Api.BaseUrl}{ClientId}/widget" +
        //                          $"?commodity={Currency.Name}" +
        //                          $"&address={SelectedAddress.Address}" +
        //                          $"&{GetConvertAmount}" +
        //                          $"&click_id=user:{GetUserId()}/network:{App.Account.Network}";
        //
        // protected IAtomexApp App { get; }
        // protected WertApi Api { get; }
        //
        // private string GetConvertAmount => FromAmountChangedFromKeyboard ?
        //     $"currency_amount={FromAmount}" :
        //     $"commodity_amount={ToAmount}";
        //
        // protected CancellationTokenSource cancellationTokenSource { get; set; }
        //
        // private CurrencyViewModel _currencyViewModel;
        //
        // public CurrencyViewModel CurrencyViewModel
        // {
        //     get => _currencyViewModel;
        //     set
        //     {
        //         _currencyViewModel = value;
        //         OnPropertyChanged(nameof(CurrencyViewModel));
        //     }
        // }
        //
        // public string Header => CurrencyViewModel.Header;
        // public CurrencyConfig Currency => CurrencyViewModel.Currency;
        //
        // public IBrush Background => IsSelected
        //     ? CurrencyViewModel.IconBrush
        //     : CurrencyViewModel.UnselectedIconBrush;
        //
        // public IBrush OpacityMask => IsSelected
        //     ? CurrencyViewModel.IconBrush is ImageBrush ? null : CurrencyViewModel.IconMaskBrush
        //     : CurrencyViewModel.IconMaskBrush;
        //
        // private bool _isSelected;
        //
        // public bool IsSelected
        // {
        //     get => _isSelected;
        //     set
        //     {
        //         _isSelected = value;
        //         OnPropertyChanged(nameof(IsSelected));
        //         OnPropertyChanged(nameof(Background));
        //         OnPropertyChanged(nameof(OpacityMask));
        //     }
        // }
        //
        // private string GetUserId()
        // {
        //     using var servicePublicKey = App.Account.Wallet.GetServicePublicKey(App.Account.UserSettings.AuthenticationKeyIndex);
        //     using var publicKey = servicePublicKey.ToUnsecuredBytes();
        //     return Sha256.Compute(Sha256.Compute(publicKey)).ToHexString();
        // }
        //
        // public WertCurrencyViewModel(CurrencyConfig currency, IAtomexApp app, WertApi wertApi)
        // {
        //     App = app ?? throw new ArgumentNullException(nameof(app));
        //     Api = wertApi ?? throw new ArgumentNullException(nameof(wertApi));
        //     CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);
        //     cancellationTokenSource = new CancellationTokenSource();
        //
        //
        //     if (Currency is TezosConfig || Currency is EthereumConfig)
        //     {
        //         var activeTokenAddresses = App.Account
        //             .GetUnspentTokenAddressesAsync(Currency.Name)
        //             .WaitForResult()
        //             .ToList();
        //
        //         var activeAddresses = App.Account
        //             .GetUnspentAddressesAsync(Currency.Name)
        //             .WaitForResult()
        //             .ToList();
        //
        //         activeTokenAddresses.ForEach(a =>
        //             a.Balance = activeAddresses.Find(b => b.Address == a.Address)?.Balance ?? 0m);
        //
        //         activeAddresses = activeAddresses
        //             .Where(a => activeTokenAddresses.FirstOrDefault(b => b.Address == a.Address) == null)
        //             .ToList();
        //
        //         var freeAddress = App.Account
        //             .GetFreeExternalAddressAsync(Currency.Name)
        //             .WaitForResult();
        //
        //         var receiveAddresses = activeTokenAddresses
        //             .DistinctBy(wa => wa.Address)
        //             .Select(w => new WalletAddressViewModel(w, Currency.Format))
        //             .Concat(activeAddresses.Select(w => new WalletAddressViewModel(w, Currency.Format)))
        //             .ToList();
        //
        //         if (receiveAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
        //             receiveAddresses.AddEx(new WalletAddressViewModel(freeAddress, Currency.Format,
        //                 isFreeAddress: true));
        //
        //         FromAddressList = receiveAddresses;
        //     }
        //     else
        //     {
        //         var activeAddresses = App.Account
        //             .GetUnspentAddressesAsync(Currency.Name)
        //             .WaitForResult();
        //
        //         var freeAddress = App.Account
        //             .GetFreeExternalAddressAsync(Currency.Name)
        //             .WaitForResult();
        //
        //         var receiveAddresses = activeAddresses
        //             .Select(wa => new WalletAddressViewModel(wa, Currency.Format))
        //             .ToList();
        //
        //         if (activeAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
        //             receiveAddresses.AddEx(new WalletAddressViewModel(freeAddress, Currency.Format,
        //                 isFreeAddress: true));
        //
        //         FromAddressList = receiveAddresses;
        //     }
        //     
        //     this.WhenAnyValue(vm => vm.FromAmount)
        //         .Throttle(TimeSpan.FromMilliseconds(500))
        //         .Where(_ => FromAmountChangedFromKeyboard)
        //         .Subscribe(async fromAmount =>
        //         {
        //             if (fromAmount == 0)
        //             {
        //                 ResetToZero();
        //                 return;
        //             }
        //     
        //             _ = GetFromCurrencyRequest();
        //         });
        //     
        //     this.WhenAnyValue(vm => vm.ToAmount)
        //         .Throttle(TimeSpan.FromMilliseconds(500))
        //         .Where(_ => ToAmountChangedFromKeyboard)
        //         .Subscribe(async (toAmount) =>
        //         {
        //             if (toAmount == 0)
        //             {
        //                 ResetToZero();
        //                 return;
        //             }
        //     
        //             _ = GetToCurrencyRequest();
        //         });
        // }
        //
        // private bool _oldRates;
        //
        // public bool OldRates
        // {
        //     get => _oldRates;
        //     set
        //     {
        //         _oldRates = value;
        //         OnPropertyChanged(nameof(OldRates));
        //     }
        // }
        //
        // private void ResetToZero()
        // {
        //     _fromAmount = 0;
        //     _toAmount = 0;
        //     EstimatedPrice = 0;
        //     OnPropertyChanged(nameof(FromAmountString));
        //     OnPropertyChanged(nameof(ToAmountString));
        //     
        //     cancellationTokenSource.Cancel();
        //     cancellationTokenSource = new CancellationTokenSource();
        // }
        //
        // protected virtual WalletAddress GetDefaultAddress()
        // {
        //     if (Currency is TezosConfig || Currency is EthereumConfig)
        //     {
        //         var activeAddressViewModel = FromAddressList
        //             .FirstOrDefault(vm => vm.HasActivity);
        //
        //         if (activeAddressViewModel != null)
        //             return activeAddressViewModel.WalletAddress;
        //
        //         return FromAddressList.First(vm => vm.IsFreeAddress).WalletAddress;
        //     }
        //
        //     return FromAddressList.First(vm => vm.IsFreeAddress).WalletAddress;
        // }
        //
        // private List<WalletAddressViewModel> _fromAddressList;
        //
        // public List<WalletAddressViewModel> FromAddressList
        // {
        //     get => _fromAddressList;
        //     protected set
        //     {
        //         _fromAddressList = value;
        //         OnPropertyChanged(nameof(FromAddressList));
        //
        //         SelectedAddress = GetDefaultAddress();
        //
        //         _selectedAddressIndex = _fromAddressList.FindIndex(fal => fal.WalletAddress == SelectedAddress);
        //         OnPropertyChanged(nameof(SelectedAddressIndex));
        //     }
        // }
        //
        // private int _selectedAddressIndex;
        //
        // public int SelectedAddressIndex
        // {
        //     get => _selectedAddressIndex;
        //     set
        //     {
        //         _selectedAddressIndex = value;
        //         OnPropertyChanged(nameof(SelectedAddressIndex));
        //
        //         SelectedAddress = FromAddressList.ElementAt(_selectedAddressIndex).WalletAddress;
        //     }
        // }
        //
        // private WalletAddress _selectedAddress;
        //
        // public WalletAddress SelectedAddress
        // {
        //     get => _selectedAddress;
        //     set { _selectedAddress = value; }
        // }
        //
        //
        // protected string CurrencyFormat => CurrencyViewModel.CurrencyFormat;
        // protected string BaseCurrencyFormat => "0.00";
        //
        //
        // protected decimal _fromAmount;
        //
        // public decimal FromAmount
        // {
        //     get => _fromAmount;
        //     set
        //     {
        //         _fromAmount = value;
        //         OnPropertyChanged(nameof(FromAmount));
        //     }
        // }
        //
        //
        // public bool FromAmountChangedFromKeyboard { get; set; }
        //
        // public string FromAmountString
        // {
        //     get => FromAmount.ToString(BaseCurrencyFormat, CultureInfo.InvariantCulture);
        //     set
        //     {
        //         if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
        //             out var fromAmount))
        //         {
        //             if (fromAmount == 0)
        //                 FromAmount = fromAmount;
        //             OnPropertyChanged(nameof(FromAmountString));
        //             return;
        //         }
        //
        //         FromAmount = fromAmount.TruncateByFormat(BaseCurrencyFormat);
        //         
        //         this.RaisePropertyChanged(nameof(FromAmountString));
        //     }
        // }
        //
        // protected decimal _toAmount;
        //
        // public decimal ToAmount
        // {
        //     get => _toAmount;
        //     set
        //     {
        //         _toAmount = value;
        //         OnPropertyChanged(nameof(ToAmount));
        //     }
        // }
        //
        // public bool ToAmountChangedFromKeyboard { get; set; }
        //
        // public string ToAmountString
        // {
        //     get => ToAmount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
        //     set
        //     {
        //         if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
        //             out var toAmount))
        //         {
        //             if (toAmount == 0)
        //                 ToAmount = toAmount;
        //             OnPropertyChanged(nameof(ToAmountString));
        //             return;
        //         }
        //
        //         ToAmount = toAmount.TruncateByFormat(CurrencyFormat);
        //         
        //         this.RaisePropertyChanged(nameof(ToAmountString));
        //     }
        // }
        //
        // private decimal _estimatedPrice;
        //
        // public decimal EstimatedPrice
        // {
        //     get => _estimatedPrice;
        //     set
        //     {
        //         _estimatedPrice = value;
        //         OnPropertyChanged(nameof(EstimatedPrice));
        //     }
        // }
        //
        // private Task RatesCheckTask { get; set; }
        //
        // public enum Side
        // {
        //     From,
        //     To
        // }
        //
        // public void StartAsyncRatesCheck(Side side = Side.From)
        // {
        //     if (RatesCheckTask != null && !RatesCheckTask.IsCompleted)
        //     {
        //         Task.Run(async () =>
        //         {
        //             cancellationTokenSource.Cancel();
        //             cancellationTokenSource = new CancellationTokenSource();
        //             await Task.Delay(500);
        //             StartAsyncRatesCheck(side);
        //         });
        //
        //         return;
        //     }
        //
        //     var attempt = 0;
        //     RatesCheckTask = Task.Run(async () =>
        //     {
        //         while (attempt < 10)
        //         {
        //             await Task.Delay(25 * 1000, cancellationTokenSource.Token)
        //                 .ConfigureAwait(false);
        //
        //             if (side == Side.From)
        //             {
        //                 await GetFromCurrencyRequest();
        //             }
        //             else if (side == Side.To)
        //             {
        //                 await GetToCurrencyRequest();
        //             }
        //
        //             attempt += 1;
        //         }
        //
        //         OldRates = true;
        //     }, cancellationTokenSource.Token);
        // }
        //
        // private async Task GetFromCurrencyRequest()
        // {
        //     var res = await Api.GetConvertRates(CurrencyViewModel.BaseCurrencyCode, Currency.Name, _fromAmount);
        //     if (res != null && !res.HasError)
        //     {
        //         OldRates = false;
        //         _toAmount = res.Value.Body.CommodityAmount;
        //         EstimatedPrice = res.Value.Body.Ticker;
        //
        //         OnPropertyChanged(nameof(ToAmountString));
        //         ToAmountChangedFromKeyboard = false;
        //     }
        // }
        //
        // private async Task GetToCurrencyRequest()
        // {
        //     var res = await Api.GetConvertRates(Currency.Name, CurrencyViewModel.BaseCurrencyCode, _toAmount);
        //     if (res != null && !res.HasError)
        //     {
        //         OldRates = false;
        //         _fromAmount = res.Value.Body.CurrencyAmount;
        //         EstimatedPrice = res.Value.Body.Ticker;
        //
        //         OnPropertyChanged(nameof(FromAmountString));
        //         FromAmountChangedFromKeyboard = false;
        //     }
        // }
        //
        // private ICommand _buyCommand;
        //
        // public ICommand BuyCommand => _buyCommand ??= (_buyCommand = ReactiveCommand.Create(() =>
        // {
        //     Desktop.App.OpenBrowser(BuyUrl);
        // }));
    }
}