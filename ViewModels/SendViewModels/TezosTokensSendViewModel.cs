using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using ReactiveUI;

using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.Common;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class TezosTokensSendViewModel : ViewModelBase 
    {
        public const string DefaultCurrencyFormat = "F8";
        public const string DefaultBaseCurrencyCode = "USD";
        public const string DefaultBaseCurrencyFormat = "$0.00";
        public const int MaxCurrencyDecimals = 9;

        private readonly IAtomexApp _app;

        private ObservableCollection<WalletAddressViewModel> _fromAddresses;
        public ObservableCollection<WalletAddressViewModel> FromAddresses
        {
            get => _fromAddresses;
            set
            {
                _fromAddresses = value;
                OnPropertyChanged(nameof(FromAddresses));
            }
        }
        
        private int _fromIndex;
        public int FromIndex
        {
            get => _fromIndex;
            set
            {
                _fromIndex = value;
                OnPropertyChanged(nameof(FromIndex));

                From = FromAddresses.ElementAt(_fromIndex).Address;
            }
        }

        private string _from;
        public string From
        {
            get => _from;
            set
            {
                _from = value;
                OnPropertyChanged(nameof(From));

                Warning = string.Empty;
                Amount = _amount;
                Fee = _fee;

                UpdateCurrencyCode();
            }
        }

        public string FromBalance { get; set; }

        private string _tokenContract;
        public string TokenContract
        {
            get => _tokenContract;
            set
            {
                _tokenContract = value;
                OnPropertyChanged(nameof(TokenContract));
            }
        }

        private decimal _tokenId;
        public decimal TokenId
        {
            get => _tokenId;
            set
            {
                _tokenId = value;
                OnPropertyChanged(nameof(TokenId));
            }
        }

        private readonly string _tokenType;

        public bool IsFa2 => _tokenType == "FA2";

        protected string _to;
        public virtual string To
        {
            get => _to;
            set
            {
                _to = value;
                OnPropertyChanged(nameof(To));

                Warning = string.Empty;
            }
        }

        public string CurrencyFormat { get; set; }
        public string FeeCurrencyFormat { get; set; }

        private string _baseCurrencyFormat;
        public virtual string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set { _baseCurrencyFormat = value; OnPropertyChanged(nameof(BaseCurrencyFormat)); }
        }

        protected decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { UpdateAmount(value); }
        }

        private bool _isAmountUpdating;
        public bool IsAmountUpdating
        {
            get => _isAmountUpdating;
            set { _isAmountUpdating = value; OnPropertyChanged(nameof(IsAmountUpdating)); }
        }
        
        public string AmountString
        {
            get => Amount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var amount))
                {
                    if (amount == 0)
                        Amount = amount;
                    OnPropertyChanged(nameof(AmountString));
                    return;
                }

                Amount = amount.TruncateByFormat(CurrencyFormat);
                OnPropertyChanged(nameof(AmountString));
            }
        }

        private bool _isFeeUpdating;
        public bool IsFeeUpdating
        {
            get => _isFeeUpdating;
            set { _isFeeUpdating = value; OnPropertyChanged(nameof(IsFeeUpdating)); }
        }

        protected decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set { UpdateFee(value); }
        }

        public virtual string FeeString
        {
            get => Fee.ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                {
                    if (fee == 0)
                        Fee = fee;
                    OnPropertyChanged(nameof(FeeString));
                    return;
                }

                Fee = fee.TruncateByFormat(FeeCurrencyFormat);
                OnPropertyChanged(nameof(FeeString));
            }
        }

        protected bool _useDefaultFee;
        public virtual bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                    Fee = _fee;
            }
        }

        protected decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        protected decimal _feeInBase;
        public decimal FeeInBase
        {
            get => _feeInBase;
            set { _feeInBase = value; OnPropertyChanged(nameof(FeeInBase)); }
        }

        protected string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set { _currencyCode = value; OnPropertyChanged(nameof(CurrencyCode)); }
        }

        protected string _feeCurrencyCode;
        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set { _feeCurrencyCode = value; OnPropertyChanged(nameof(FeeCurrencyCode)); }
        }

        protected string _baseCurrencyCode;
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
        }

        protected string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        public TezosTokensSendViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TezosTokensSendViewModel(
            IAtomexApp app,
            string tokenContract,
            decimal tokenId,
            string tokenType,
            string from = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            CurrencyCode     = "";
            FeeCurrencyCode  = TezosConfig.Xtz;
            BaseCurrencyCode = DefaultBaseCurrencyCode;

            CurrencyFormat     = DefaultCurrencyFormat;
            FeeCurrencyFormat  = tezosConfig.FeeFormat;
            BaseCurrencyFormat = DefaultBaseCurrencyFormat;

            _tokenContract = tokenContract;
            _tokenId       = tokenId;
            _tokenType     = tokenType;

            UpdateFromAddressList(from, tokenContract);
            UpdateCurrencyCode();

            SubscribeToServices();

            UseDefaultFee = true;
        }

        private void SubscribeToServices()
        {
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= (_backCommand = ReactiveCommand.Create(() =>
        {
            Desktop.App.DialogService.CloseDialog();
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= ReactiveCommand.Create(OnNextCommand);

        protected ICommand _maxCommand;
        public ICommand MaxCommand => _maxCommand ??= ReactiveCommand.Create(OnMaxClick);

        protected async virtual void OnNextCommand()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (string.IsNullOrEmpty(_to))
            {
                Warning = Resources.SvEmptyAddressError;
                return;
            }

            if (!tezosConfig.IsValidAddress(_to))
            {
                Warning = Resources.SvInvalidAddressError;
                return;
            }

            if (_amount <= 0)
            {
                Warning = Resources.SvAmountLessThanZeroError;
                return;
            }

            if (_fee <= 0)
            {
                Warning = Resources.SvCommissionLessThanZeroError;
                return;
            }

            if (_tokenContract == null || _from == null)
            {
                Warning = "Invalid 'From' address or token contract address!";
                return;
            }

            if (!tezosConfig.IsValidAddress(TokenContract))
            {
                Warning = "Invalid token contract address!";
                return;
            }

            var fromTokenAddress = await GetTokenAddressAsync(
                account: _app.Account,
                address: _from,
                tokenContract: _tokenContract,
                tokenId: _tokenId,
                tokenType: _tokenType);

            if (fromTokenAddress == null)
            {
                Warning = $"Insufficient token funds on address {_from}! Please update your balance!";
                return;
            }

            if (_amount > fromTokenAddress.Balance)
            {
                Warning = $"Insufficient token funds on address {fromTokenAddress.Address}! Please use Max button to find out how many tokens you can send!";
                return;
            }

            var xtzAddress = await _app.Account
                .GetAddressAsync(TezosConfig.Xtz, _from);

            if (xtzAddress == null)
            {
                Warning = $"Insufficient funds for fee. Please update your balance for address {_from}!";
                return;
            }

            if (xtzAddress.AvailableBalance() < _fee)
            {
                Warning = $"Insufficient funds for fee!";
                return;
            }

            var confirmationViewModel = new SendConfirmationViewModel()
            {
                Currency           = tezosConfig,
                From               = _from,
                To                 = _to,
                Amount             = _amount,
                AmountInBase       = _amountInBase,
                BaseCurrencyCode   = _baseCurrencyCode,
                BaseCurrencyFormat = _baseCurrencyFormat,
                Fee                = _fee,
                UseDeafultFee      = _useDefaultFee,
                FeeInBase          = _feeInBase,
                CurrencyCode       = _currencyCode,
                CurrencyFormat     = CurrencyFormat,

                FeeCurrencyCode    = _feeCurrencyCode,
                FeeCurrencyFormat  = FeeCurrencyFormat,

                TokenContract      = _tokenContract,
                TokenId            = _tokenId,
                TokenType          = _tokenType,
                
                SendCallback       = Send
            };

            App.DialogService.Show(confirmationViewModel);
        }

        protected virtual async void UpdateAmount(decimal amount)
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;
            Warning = string.Empty;

            _amount = amount;
            OnPropertyChanged(nameof(AmountString));

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From == null)
                {
                    Warning = "Invalid 'From' address or token contract address!";
                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract))
                {
                    Warning = "Invalid token contract address!";
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(
                    account: _app.Account,
                    address: _from,
                    tokenContract: _tokenContract,
                    tokenId: _tokenId,
                    tokenType: _tokenType);

                if (fromTokenAddress == null)
                {
                    Warning = $"Insufficient token funds on address {From}! Please update your balance!";
                    return;
                }

                if (_amount > fromTokenAddress.Balance)
                {
                    Warning = $"Insufficient token funds on address {fromTokenAddress.Address}! Please use Max button to find out how many tokens you can send!";
                    return;
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected virtual async void UpdateFee(decimal fee)
        {
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From == null)
                {
                    Warning = "Invalid 'From' address or token contract address!";
                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract))
                {
                    Warning = "Invalid token contract address!";
                    return;
                }

                if (UseDefaultFee)
                {
                    var fromTokenAddress = await GetTokenAddressAsync(
                        account: _app.Account,
                        address: _from,
                        tokenContract: _tokenContract,
                        tokenId: _tokenId,
                        tokenType: _tokenType);

                    if (fromTokenAddress == null)
                    {
                        Warning = $"Insufficient token funds on address {From}! Please update your balance!";
                        return;
                    }

                    var tokenAccount = _app.Account
                        .GetTezosTokenAccount<TezosTokenAccount>(fromTokenAddress.Currency, TokenContract, TokenId);

                    var (estimatedFee, isEnougth) = await tokenAccount
                        .EstimateTransferFeeAsync(From);

                    if (!isEnougth)
                    {
                        Warning = $"Insufficient funds for fee. Minimum {estimatedFee} XTZ is required!";
                        return;
                    }

                    _fee = estimatedFee;
                }
                else
                {
                    var xtzAddress = await _app.Account
                        .GetAddressAsync(TezosConfig.Xtz, From);

                    if (xtzAddress == null)
                    {
                        Warning = $"Insufficient funds for fee. Please update your balance for address {From}!";
                        return;
                    }

                    _fee = Math.Min(fee, tezosConfig.GetMaximumFee());

                    if (xtzAddress.AvailableBalance() < _fee)
                    {
                        Warning = $"Insufficient funds for fee!";
                        return;
                    }
                }

                OnPropertyChanged(nameof(FeeString));

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected virtual async void OnMaxClick()
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From == null)
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract))
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    Warning = "Invalid token contract address!";
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(
                    account: _app.Account,
                    address: _from,
                    tokenContract: _tokenContract,
                    tokenId: _tokenId,
                    tokenType: _tokenType);

                if (fromTokenAddress == null)
                {
                    _amount = 0;
                    OnPropertyChanged(nameof(AmountString));

                    Warning = $"Insufficient token funds on address {From}! Please update your balance!";
                    return;
                }

                _amount = fromTokenAddress.Balance;
                OnPropertyChanged(nameof(AmountString));

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            AmountInBase = !string.IsNullOrEmpty(CurrencyCode)
                ? Amount * (quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode)?.Bid ?? 0m)
                : 0;

            FeeInBase = !string.IsNullOrEmpty(FeeCurrencyCode)
                ? Fee * (quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode)?.Bid ?? 0m)
                : 0;
        }

        public static async Task<WalletAddress> GetTokenAddressAsync(
            IAccount account,
            string address,
            string tokenContract,
            decimal tokenId,
            string tokenType)
        {
            var tezosAccount = account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            return await tezosAccount
                .DataRepository
                .GetTezosTokenAddressAsync(tokenType, tokenContract, tokenId, address);
        }

        private void UpdateFromAddressList(string from, string tokenContract)
        {
            _fromAddresses = new ObservableCollection<WalletAddressViewModel>(GetFromAddressList(tokenContract));

            var tempFrom = from;

            if (tempFrom == null)
            {
                var unspentAddresses = _fromAddresses.Where(w => w.AvailableBalance > 0);
                var unspentTokenAddresses = _fromAddresses.Where(w => w.TokenBalance > 0);

                tempFrom = unspentTokenAddresses.MaxByOrDefault(w => w.TokenBalance)?.Address ??
                    unspentAddresses.MaxByOrDefault(w => w.AvailableBalance)?.Address;
            }

            OnPropertyChanged(nameof(FromAddresses));
            
            // From = tempFrom;
            
            var walletAddressViewModel = FromAddresses
                .FirstOrDefault(a => a.Address == tempFrom);
            
            FromIndex = FromAddresses.IndexOf(walletAddressViewModel);
        }

        private async void UpdateCurrencyCode()
        {
            if (TokenContract == null || From == null)
                return;

            var tokenAddress = await GetTokenAddressAsync(
                account: _app.Account,
                address: _from,
                tokenContract: _tokenContract,
                tokenId: _tokenId,
                tokenType: _tokenType);

            if (tokenAddress?.TokenBalance?.Symbol != null)
            {
                CurrencyCode = tokenAddress.TokenBalance.Symbol;
                CurrencyFormat = $"F{Math.Min(tokenAddress.TokenBalance.Decimals, MaxCurrencyDecimals)}";
                OnPropertyChanged(nameof(AmountString));
            }
            else
            {
                CurrencyCode = _app.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract)
                    ?.Name ?? "TOKENS";
                CurrencyFormat = DefaultCurrencyFormat;
                OnPropertyChanged(nameof(AmountString));
            }
        }

        private IEnumerable<WalletAddressViewModel> GetFromAddressList(string tokenContract)
        {
            if (tokenContract == null)
                return Enumerable.Empty<WalletAddressViewModel>();

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var tezosAccount = _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            var tezosAddresses = tezosAccount
                .GetUnspentAddressesAsync()
                .WaitForResult()
                .ToDictionary(w => w.Address, w => w);

            var tokenAddresses = tezosAccount.DataRepository
                .GetTezosTokenAddressesByContractAsync(tokenContract)
                .WaitForResult();

            return tokenAddresses
                .Where(w => w.Balance != 0)
                .Select(w =>
                {
                    var tokenBalance = w.Balance;

                    var showTokenBalance = tokenBalance != 0;

                    var tokenCode = w.TokenBalance?.Symbol ?? "TOKENS";

                    var tezosBalance = tezosAddresses.TryGetValue(w.Address, out var tezosAddress)
                        ? tezosAddress.AvailableBalance()
                        : 0m;

                    return new WalletAddressViewModel
                    {
                        Address          = w.Address,
                        AvailableBalance = tezosBalance,
                        CurrencyFormat   = tezosConfig.Format,
                        CurrencyCode     = tezosConfig.Name,
                        IsFreeAddress    = false,
                        ShowTokenBalance = showTokenBalance,
                        TokenBalance     = tokenBalance,
                        TokenFormat      = "F8",
                        TokenCode        = tokenCode
                    };
                })
                .ToList();
        }

        protected async Task<Error> Send(
            SendConfirmationViewModel confirmationViewModel,
            CancellationToken cancellationToken = default)
        {
            var tokenAddress = await GetTokenAddressAsync(
                account: App.AtomexApp.Account,
                address: confirmationViewModel.From,
                tokenContract: confirmationViewModel.TokenContract,
                tokenId: confirmationViewModel.TokenId,
                tokenType: confirmationViewModel.TokenType);

            if (tokenAddress.Currency == "FA12")
            {
                var currencyName = App.AtomexApp.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == confirmationViewModel.TokenContract)
                    ?.Name ?? "FA12";

                var tokenAccount = App.AtomexApp.Account.GetTezosTokenAccount<Fa12Account>(
                    currency: currencyName,
                    tokenContract: confirmationViewModel.TokenContract,
                    tokenId: confirmationViewModel.TokenId);

                return await tokenAccount.SendAsync(
                    from: tokenAddress.Address,
                    to: confirmationViewModel.To,
                    amount: confirmationViewModel.Amount,
                    fee: confirmationViewModel.Fee,
                    useDefaultFee: confirmationViewModel.UseDeafultFee);
            }
            else
            {
                var tokenAccount = App.AtomexApp.Account.GetTezosTokenAccount<Fa2Account>(
                    currency: "FA2",
                    tokenContract: confirmationViewModel.TokenContract,
                    tokenId: confirmationViewModel.TokenId);

                var decimals = tokenAddress.TokenBalance.Decimals;
                var amount = Amount * (decimal)Math.Pow(10, decimals);
                var fee = (int)confirmationViewModel.Fee.ToMicroTez();

                return await tokenAccount.SendAsync(
                    from: confirmationViewModel.From,
                    to: confirmationViewModel.To,
                    amount: amount,
                    tokenContract: confirmationViewModel.TokenContract,
                    tokenId: (int)confirmationViewModel.TokenId,
                    fee: fee,
                    useDefaultFee: confirmationViewModel.UseDeafultFee);
            }
        }

        private void DesignerMode()
        {
        }
    }
}