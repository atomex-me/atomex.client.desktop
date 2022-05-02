using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Media;
using ReactiveUI;

using Atomex.Client.Desktop.Api;
using Atomex.Client.Desktop.Common;
using Atomex.Core;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Cryptography;
using Atomex.Wallet.Abstract;
using Atomex.ViewModels;
using Atomex.Cryptography.Abstract;

namespace Atomex.Client.Desktop.ViewModels
{
    public class WertCurrencyViewModel : ViewModelBase
    {
        private string ClientId => _app.Account.Network == Network.MainNet ? "atomex" : "01F298K3HP4DY326AH1NS3MM3M";

        private string BuyUrl => $"{_api.BaseUrl}{ClientId}/widget" +
                                 $"?commodity={Currency.Name}" +
                                 $"&address={SelectedAddress}" +
                                 $"&{GetConvertAmount}" +
                                 $"&click_id=user:{GetUserId()}/network:{_app.Account.Network}";

        private readonly IAtomexApp _app;
        private readonly WertApi _api;

        private string GetConvertAmount => FromAmountChangedFromKeyboard
            ? $"currency_amount={FromAmount}"
            : $"commodity_amount={ToAmount}";

        protected CancellationTokenSource _cancellationTokenSource;

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
        public CurrencyConfig Currency => CurrencyViewModel.Currency;

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

        private string GetUserId()
        {
            using var servicePublicKey =
                _app.Account.Wallet.GetServicePublicKey(_app.Account.UserSettings.AuthenticationKeyIndex);
            var publicKey = servicePublicKey.ToUnsecuredBytes();

            return HashAlgorithm.Sha256.Hash(publicKey).ToHexString();
        }

        public WertCurrencyViewModel()
        {
        }

        public WertCurrencyViewModel(CurrencyConfig currency, IAtomexApp app, WertApi wertApi)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _api = wertApi ?? throw new ArgumentNullException(nameof(wertApi));
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);
            _cancellationTokenSource = new CancellationTokenSource();

            // get all addresses with tokens (if exists)
            var tokenAddresses = Currencies.HasTokens(Currency.Name)
                ? (_app.Account
                    .GetCurrencyAccount(Currency.Name) as IHasTokens)
                    ?.GetUnspentTokenAddressesAsync()
                    .WaitForResult() ?? new List<WalletAddress_OLD>()
                : new List<WalletAddress_OLD>();

            // get all active addresses
            var activeAddresses = _app.Account
                .GetUnspentAddressesAsync(Currency.Name)
                .WaitForResult()
                .ToList();

            // get free external address
            var freeAddress = _app.Account
                .GetFreeExternalAddressAsync(Currency.Name)
                .WaitForResult();

            FromAddressList = activeAddresses
                .Concat(tokenAddresses)
                .Concat(new WalletAddress_OLD[] { freeAddress })
                .GroupBy(w => w.Address)
                .Select(g =>
                {
                    // main address
                    var address = g.FirstOrDefault(w => w.Currency == Currency.Name);

                    var isFreeAddress = address?.Address == freeAddress.Address;

                    var hasTokens = g.Any(w => w.Currency != Currency.Name);

                    return new WalletAddressViewModel
                    {
                        Address = g.Key,
                        HasActivity = address?.HasActivity ?? hasTokens,
                        AvailableBalance = address?.AvailableBalance() ?? 0m,
                        CurrencyFormat = Currency.Format,
                        CurrencyCode = Currency.Name,
                        IsFreeAddress = isFreeAddress,
                    };
                })
                .ToList();

            this.WhenAnyValue(vm => vm.FromAmount)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Where(_ => FromAmountChangedFromKeyboard)
                .SubscribeInMainThread(async fromAmount =>
                {
                    if (fromAmount == 0)
                    {
                        ResetToZero();
                        return;
                    }

                    _ = GetFromCurrencyRequest();
                });

            this.WhenAnyValue(vm => vm.ToAmount)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Where(_ => ToAmountChangedFromKeyboard)
                .SubscribeInMainThread(async (toAmount) =>
                {
                    if (toAmount == 0)
                    {
                        ResetToZero();
                        return;
                    }

                    _ = GetToCurrencyRequest();
                });
        }

        private bool _oldRates;

        public bool OldRates
        {
            get => _oldRates;
            set
            {
                _oldRates = value;
                OnPropertyChanged(nameof(OldRates));
            }
        }

        private void ResetToZero()
        {
            _fromAmount = 0;
            _toAmount = 0;
            EstimatedPrice = 0;
            OnPropertyChanged(nameof(FromAmountString));
            OnPropertyChanged(nameof(ToAmountString));

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        protected virtual string GetDefaultAddress()
        {
            if (Currency is TezosConfig || Currency is EthereumConfig)
            {
                var activeAddressViewModel = FromAddressList
                    .Where(vm => vm.HasActivity && vm.AvailableBalance > 0)
                    .MaxByOrDefault(vm => vm.AvailableBalance);

                if (activeAddressViewModel != null)
                    return activeAddressViewModel.Address;
            }

            return FromAddressList.First(vm => vm.IsFreeAddress).Address;
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

                _selectedAddressIndex = _fromAddressList.FindIndex(fal => fal.Address == SelectedAddress);
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

                SelectedAddress = FromAddressList.ElementAt(_selectedAddressIndex).Address;
            }
        }

        private string _selectedAddress;

        public string SelectedAddress
        {
            get => _selectedAddress;
            set { _selectedAddress = value; }
        }


        protected string CurrencyFormat => CurrencyViewModel.CurrencyFormat;
        protected string BaseCurrencyFormat => "0.00";


        protected decimal _fromAmount;

        public decimal FromAmount
        {
            get => _fromAmount;
            set
            {
                _fromAmount = value;
                OnPropertyChanged(nameof(FromAmount));
            }
        }

        public bool FromAmountChangedFromKeyboard { get; set; }

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

                FromAmount = fromAmount.TruncateByFormat(BaseCurrencyFormat);

                this.RaisePropertyChanged(nameof(FromAmountString));
            }
        }

        protected decimal _toAmount;

        public decimal ToAmount
        {
            get => _toAmount;
            set
            {
                _toAmount = value;
                OnPropertyChanged(nameof(ToAmount));
            }
        }

        public bool ToAmountChangedFromKeyboard { get; set; }

        public string ToAmountString
        {
            get => ToAmount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var toAmount))
                {
                    if (toAmount == 0)
                        ToAmount = toAmount;
                    OnPropertyChanged(nameof(ToAmountString));
                    return;
                }

                ToAmount = toAmount.TruncateByFormat(CurrencyFormat);

                this.RaisePropertyChanged(nameof(ToAmountString));
            }
        }

        private decimal _estimatedPrice;

        public decimal EstimatedPrice
        {
            get => _estimatedPrice;
            set
            {
                _estimatedPrice = value;
                OnPropertyChanged(nameof(EstimatedPrice));
            }
        }

        private Task RatesCheckTask { get; set; }

        public enum Side
        {
            From,
            To
        }

        public void StartAsyncRatesCheck(Side side = Side.From)
        {
            if (RatesCheckTask != null && !RatesCheckTask.IsCompleted)
            {
                Task.Run(async () =>
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource = new CancellationTokenSource();
                    await Task.Delay(500);
                    StartAsyncRatesCheck(side);
                });

                return;
            }

            var attempt = 0;
            RatesCheckTask = Task.Run(async () =>
            {
                while (attempt < 10)
                {
                    await Task.Delay(25 * 1000, _cancellationTokenSource.Token)
                        .ConfigureAwait(false);

                    if (side == Side.From)
                    {
                        await GetFromCurrencyRequest();
                    }
                    else if (side == Side.To)
                    {
                        await GetToCurrencyRequest();
                    }

                    attempt += 1;
                }

                OldRates = true;
            }, _cancellationTokenSource.Token);
        }

        private async Task GetFromCurrencyRequest()
        {
            var res = await _api.GetConvertRates(CurrencyViewModel.BaseCurrencyCode, Currency.Name, _fromAmount);
            if (res != null && !res.HasError)
            {
                OldRates = false;
                _toAmount = res.Value.Body.CommodityAmount;
                EstimatedPrice = res.Value.Body.Ticker;

                OnPropertyChanged(nameof(ToAmountString));
                ToAmountChangedFromKeyboard = false;
            }
        }

        private async Task GetToCurrencyRequest()
        {
            var res = await _api.GetConvertRates(Currency.Name, CurrencyViewModel.BaseCurrencyCode, _toAmount);
            if (res != null && !res.HasError)
            {
                OldRates = false;
                _fromAmount = res.Value.Body.CurrencyAmount;
                EstimatedPrice = res.Value.Body.Ticker;

                OnPropertyChanged(nameof(FromAmountString));
                FromAmountChangedFromKeyboard = false;
            }
        }

        private ICommand _buyCommand;

        public ICommand BuyCommand => _buyCommand ??= (_buyCommand = ReactiveCommand.Create(() =>
        {
            App.OpenBrowser(BuyUrl);
        }));
    }
}