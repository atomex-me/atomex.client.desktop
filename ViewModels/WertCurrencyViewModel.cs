using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atomex.Client.Desktop.Common;
using Atomex.Core;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Avalonia.Media;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WertCurrencyViewModel : ViewModelBase
    {
        protected IAtomexApp App { get; }

        private CurrencyViewModel _currencyViewModel;

        public CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set
            {
                _currencyViewModel = value;
                OnPropertyChanged(nameof(CurrencyViewModel));
            }
        }

        public string Header => CurrencyViewModel.Header;
        public Currency Currency => CurrencyViewModel.Currency;

        public IBrush Background => IsSelected
            ? CurrencyViewModel.IconBrush
            : CurrencyViewModel.UnselectedIconBrush;

        public IBrush OpacityMask => IsSelected
            ? CurrencyViewModel.IconBrush is ImageBrush ? null : CurrencyViewModel.IconMaskBrush
            : CurrencyViewModel.IconMaskBrush;

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(Background));
                OnPropertyChanged(nameof(OpacityMask));
            }
        }

        public WertCurrencyViewModel(Currency currency, IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);
            
            if (Currency is Tezos || Currency is Ethereum)
            {
                var activeTokenAddresses = App.Account
                    .GetUnspentTokenAddressesAsync(Currency.Name)
                    .WaitForResult()
                    .ToList();

                var activeAddresses = App.Account
                    .GetUnspentAddressesAsync(Currency.Name)
                    .WaitForResult()
                    .ToList();

                activeTokenAddresses.ForEach(a =>
                    a.Balance = activeAddresses.Find(b => b.Address == a.Address)?.Balance ?? 0m);

                activeAddresses = activeAddresses
                    .Where(a => activeTokenAddresses.FirstOrDefault(b => b.Address == a.Address) == null)
                    .ToList();

                var freeAddress = App.Account
                    .GetFreeExternalAddressAsync(Currency.Name)
                    .WaitForResult();

                var receiveAddresses = activeTokenAddresses
                    .DistinctBy(wa => wa.Address)
                    .Select(w => new WalletAddressViewModel(w, Currency.Format))
                    .Concat(activeAddresses.Select(w => new WalletAddressViewModel(w, Currency.Format)))
                    .ToList();

                if (receiveAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
                    receiveAddresses.AddEx(new WalletAddressViewModel(freeAddress, Currency.Format,
                        isFreeAddress: true));

                FromAddressList = receiveAddresses;
            }
            else
            {
                var activeAddresses = App.Account
                    .GetUnspentAddressesAsync(Currency.Name)
                    .WaitForResult();

                var freeAddress = App.Account
                    .GetFreeExternalAddressAsync(Currency.Name)
                    .WaitForResult();

                var receiveAddresses = activeAddresses
                    .Select(wa => new WalletAddressViewModel(wa, Currency.Format))
                    .ToList();

                if (activeAddresses.FirstOrDefault(w => w.Address == freeAddress.Address) == null)
                    receiveAddresses.AddEx(new WalletAddressViewModel(freeAddress, Currency.Format,
                        isFreeAddress: true));

                FromAddressList = receiveAddresses;
            }
        }

        protected virtual WalletAddress GetDefaultAddress()
        {
            if (Currency is Tezos || Currency is Ethereum)
            {
                var activeAddressViewModel = FromAddressList
                    .FirstOrDefault(vm => vm.WalletAddress.HasActivity);

                if (activeAddressViewModel != null)
                    return activeAddressViewModel.WalletAddress;

                return FromAddressList.First(vm => vm.IsFreeAddress).WalletAddress;
            }

            return FromAddressList.First(vm => vm.IsFreeAddress).WalletAddress;
        }

        private List<WalletAddressViewModel> _fromAddressList;

        public List<WalletAddressViewModel> FromAddressList
        {
            get => _fromAddressList;
            protected set
            {
                _fromAddressList = value;
                OnPropertyChanged(nameof(FromAddressList));

                SelectedAddress = GetDefaultAddress();

                _selectedAddressIndex = _fromAddressList.FindIndex(fal => fal.WalletAddress == SelectedAddress);
                OnPropertyChanged(nameof(SelectedAddressIndex));
            }
        }

        private int _selectedAddressIndex;

        public int SelectedAddressIndex
        {
            get => _selectedAddressIndex;
            set
            {
                _selectedAddressIndex = value;
                OnPropertyChanged(nameof(SelectedAddressIndex));

                SelectedAddress = FromAddressList.ElementAt(_selectedAddressIndex).WalletAddress;
            }
        }

        private WalletAddress _selectedAddress;

        public WalletAddress SelectedAddress
        {
            get => _selectedAddress;
            set { _selectedAddress = value; }
        }


        protected string CurrencyFormat => CurrencyViewModel.CurrencyFormat;
        protected string BaseCurrencyFormat => CurrencyViewModel.BaseCurrencyFormat;
        
        
        protected decimal _fromAmount;

        public decimal FromAmount
        {
            get => _fromAmount;
            set { _fromAmount = value; }
        }
            
        
        public string FromAmountString
        {
            get => FromAmount.ToString(BaseCurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var fromAmount))
                {
                    if (fromAmount == 0)
                        FromAmount = fromAmount;
                    OnPropertyChanged(nameof(FromAmountString));
                    return;
                }


                FromAmount = _fromAmount.TruncateByFormat(BaseCurrencyFormat);
                OnPropertyChanged(nameof(FromAmountString));
            }
        }
    }
}