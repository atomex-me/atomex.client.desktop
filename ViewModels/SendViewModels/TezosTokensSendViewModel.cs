using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.ViewModels;
using Atomex.Wallet.Tezos;
using Atomex.Wallet.Abstract;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class TezosTokensSendViewModel : ViewModelBase
    {
        public static readonly string DefaultCurrencyFormat = $"F{AddressesHelper.MaxTokenCurrencyFormatDecimals}";
        public const string DefaultBaseCurrencyCode = "USD";
        public const string DefaultBaseCurrencyFormat = "$0.00";

        private readonly IAtomexApp _app;
        [Reactive] public decimal SelectedFromBalance { get; set; }
        [Reactive] public string From { get; set; }
        [ObservableAsProperty] public string FromBeautified { get; }
        [Reactive] private string TokenContract { get; set; }
        [ObservableAsProperty] public string TokenContractBeautified { get; }
        [Reactive] public decimal TokenId { get; set; }
        [Reactive] public virtual string To { get; set; }
        [Reactive] public IBitmap TokenPreview { get; set; }
        private readonly string _tokenType;
        public bool IsFa2 => _tokenType == "FA2";

        [Reactive] public string CurrencyFormat { get; set; }
        private string FeeCurrencyFormat { get; set; }
        [Reactive] public string BaseCurrencyFormat { get; set; }

        [Reactive] private decimal Amount { get; set; }
        [ObservableAsProperty] public string AmountString { get; }

        public void SetAmountFromString(string value)
        {
            if (value == AmountString)
                return;

            var parsed = decimal.TryParse(
                value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount);

            if (!parsed)
                amount = Amount;

            var truncatedValue = amount.TruncateByFormat(CurrencyFormat);

            if (truncatedValue != Amount)
                Amount = truncatedValue;

            Dispatcher.UIThread.InvokeAsync(() => this.RaisePropertyChanged(nameof(AmountString)));
        }

        [Reactive] private decimal Fee { get; set; }
        [ObservableAsProperty] public virtual string FeeString { get; }

        public void SetFeeFromString(string value)
        {
            if (value == FeeString)
                return;

            var parsed = decimal.TryParse(
                value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var fee);

            if (!parsed)
                fee = Fee;

            var truncatedValue = fee.TruncateByFormat(FeeCurrencyFormat);

            if (truncatedValue != Fee)
                Fee = truncatedValue;

            Dispatcher.UIThread.InvokeAsync(() => this.RaisePropertyChanged(nameof(FeeString)));
        }

        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [Reactive] public string CurrencyCode { get; set; }
        [Reactive] public string FeeCurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }
        [Reactive] public string Warning { get; set; }
        [Reactive] public bool ConfirmStage { get; set; }

        public SelectAddressViewModel SelectFromViewModel { get; set; }
        public SelectAddressViewModel SelectToViewModel { get; set; }
        private Func<string, decimal, IBitmap> GetTokenPreview { get; }

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
            Func<string, decimal, IBitmap> getTokenPreview,
            string? balanceFormat = null,
            string? from = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            var updateCurrencyCodeCommand = ReactiveCommand.Create(UpdateCurrencyCode);
            var updateAmountCommand = ReactiveCommand.CreateFromTask(UpdateAmount);
            var updateFeeCommand = ReactiveCommand.CreateFromTask(UpdateFee);

            this.WhenAnyValue(
                    vm => vm.From,
                    vm => vm.Amount,
                    vm => vm.Fee
                )
                .Subscribe(_ => Warning = string.Empty);
            
            this.WhenAnyValue(vm => vm.Amount)
                .Select(amount => amount.ToString(CurrencyFormat ?? balanceFormat, CultureInfo.InvariantCulture))
                .ToPropertyEx(this, vm => vm.AmountString);

            this.WhenAnyValue(vm => vm.From)
                .Select(SendViewModel.GetShortenedAddress)
                .ToPropertyEx(this, vm => vm.FromBeautified);

            this.WhenAnyValue(vm => vm.TokenContract)
                .Select(SendViewModel.GetShortenedAddress)
                .ToPropertyEx(this, vm => vm.TokenContractBeautified);

            this.WhenAnyValue(
                    vm => vm.From,
                    vm => vm.TokenId
                )
                .Select(_ => Unit.Default)
                .InvokeCommand(updateCurrencyCodeCommand);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.From,
                    vm => vm.To,
                    vm => vm.TokenId
                )
                .Select(_ => Unit.Default)
                .InvokeCommand(updateAmountCommand);

            this.WhenAnyValue(
                    vm => vm.Fee,
                    vm => vm.From,
                    vm => vm.TokenId,
                    vm => vm.UseDefaultFee
                )
                .Select(_ => Unit.Default)
                .InvokeCommand(updateFeeCommand);

            this.WhenAnyValue(
                    vm => vm.Amount,
                    vm => vm.Fee
                )
                .Subscribe(_ => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));

            this.WhenAnyValue(vm => vm.Fee)
                .Select(fee => fee.ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture))
                .ToPropertyEx(this, vm => vm.FeeString);


            CurrencyCode = string.Empty;
            FeeCurrencyCode = TezosConfig.Xtz;
            BaseCurrencyCode = DefaultBaseCurrencyCode;
            
            FeeCurrencyFormat = tezosConfig.FeeFormat;
            BaseCurrencyFormat = DefaultBaseCurrencyFormat;

            TokenContract = tokenContract;

            TokenId = tokenId;
            _tokenType = tokenType;
            GetTokenPreview = getTokenPreview;

            if (from != null)
            {
                From = from;
                Amount = SelectedFromBalance;
            }
            
            UpdateCurrencyCode();
            SubscribeToServices();
            UseDefaultFee = true;

            SelectFromViewModel =
                new SelectAddressViewModel(_app.Account, tezosConfig, true, from, tokenId, tokenContract)
                {
                    BackAction = () => { App.DialogService.Show(this); },
                    ConfirmAction = (address, balance, tokenId) =>
                    {
                        TokenId = tokenId;
                        From = address;
                        App.DialogService.Show(SelectToViewModel);
                    }
                };

            SelectToViewModel = new SelectAddressViewModel(_app.Account, tezosConfig)
            {
                BackAction = () => { App.DialogService.Show(SelectFromViewModel); },
                ConfirmAction = (address, _, _) =>
                {
                    To = address;
                    App.DialogService.Show(this);
                }
            };
        }

        private void SubscribeToServices()
        {
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private ReactiveCommand<Unit, Unit> _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand => _backCommand ??= (_backCommand = ReactiveCommand.Create(() =>
        {
            Desktop.App.DialogService.Close();
        }));

        private ReactiveCommand<Unit, Unit> _nextCommand;
        public ReactiveCommand<Unit, Unit> NextCommand => _nextCommand ??= ReactiveCommand.Create(OnNextCommand);

        private ReactiveCommand<Unit, Unit> _maxCommand;
        public ReactiveCommand<Unit, Unit> MaxCommand => _maxCommand ??= ReactiveCommand.Create(OnMaxClick);

        private ReactiveCommand<Unit, Unit> _undoConfirmStageCommand;

        public ReactiveCommand<Unit, Unit> UndoConfirmStageCommand => _undoConfirmStageCommand ??=
            (_undoConfirmStageCommand = ReactiveCommand.Create(() => { ConfirmStage = false; }));

        private ReactiveCommand<Unit, Unit> _selectFromCommand;

        public ReactiveCommand<Unit, Unit> SelectFromCommand => _selectFromCommand ??=
            (_selectFromCommand = ReactiveCommand.Create(FromClick));

        private ReactiveCommand<Unit, Unit> _selectToCommand;

        public ReactiveCommand<Unit, Unit> SelectToCommand => _selectToCommand ??=
            (_selectToCommand = ReactiveCommand.Create(ToClick));

        private void FromClick()
        {
            SelectFromViewModel.ConfirmAction = (address, balance, tokenId) =>
            {
                TokenId = tokenId;
                From = address;
                App.DialogService.Show(this);
            };

            App.DialogService.Show(SelectFromViewModel);
        }

        private void ToClick()
        {
            SelectToViewModel.BackAction = () => App.DialogService.Show(this);
            App.DialogService.Show(SelectToViewModel);
        }

        private async void OnNextCommand()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (string.IsNullOrEmpty(To))
            {
                Warning = Resources.SvEmptyAddressError;
                return;
            }


            if (!tezosConfig.IsValidAddress(To))
            {
                Warning = Resources.SvInvalidAddressError;
                return;
            }

            if (Amount <= 0)
            {
                Warning = Resources.SvAmountLessThanZeroError;
                return;
            }

            if (Fee <= 0)
            {
                Warning = Resources.SvCommissionLessThanZeroError;
                return;
            }

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
                address: From,
                tokenContract: TokenContract,
                tokenId: TokenId,
                tokenType: _tokenType);

            if (fromTokenAddress == null)
            {
                Warning = $"Insufficient token funds on address {From}! Please update your balance!";
                return;
            }

            if (Amount > fromTokenAddress.Balance)
            {
                Warning =
                    $"Insufficient token funds on address {fromTokenAddress.Address}! Please use Max button to find out how many tokens you can send!";
                return;
            }

            var xtzAddress = await _app.Account
                .GetAddressAsync(TezosConfig.Xtz, From);

            if (xtzAddress == null)
            {
                Warning = $"Insufficient funds for fee. Please update your balance for address {From}!";
                return;
            }

            if (xtzAddress.AvailableBalance() < Fee)
            {
                Warning = "Insufficient funds for fee!";
                return;
            }

            if (ConfirmStage)
            {
                try
                {
                    App.DialogService.Show(new SendingViewModel());

                    var error = await Send();

                    if (error != null)
                    {
                        App.DialogService.Show(MessageViewModel.Error(
                            text: error.Description,
                            backAction: () => App.DialogService.Show(this)));

                        return;
                    }

                    App.DialogService.Show(MessageViewModel.Success(
                        text: "Sending was successful",
                        nextAction: () => { App.DialogService.Close(); }));
                }
                catch (Exception e)
                {
                    App.DialogService.Show(MessageViewModel.Error(
                        text: "An error has occurred while sending transaction.",
                        backAction: () => App.DialogService.Show(this)));

                    Log.Error(e, "Tezos tokens transaction send error");
                }
                finally
                {
                    ConfirmStage = false;
                }
            }

            ConfirmStage = true;
        }

        private async Task UpdateAmount()
        {
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
                    address: From,
                    tokenContract: TokenContract,
                    tokenId: TokenId,
                    tokenType: _tokenType);

                if (fromTokenAddress == null)
                {
                    Warning = $"Insufficient token funds on address {From}! Please update your balance!";
                    return;
                }

                if (Amount > fromTokenAddress.Balance)
                {
                    Warning =
                        $"Insufficient token funds on address {fromTokenAddress.Address}! Please use Max button to find out how many tokens you can send!";
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens update amount error");
            }
        }

        private async Task UpdateFee()
        {
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
                        address: From,
                        tokenContract: TokenContract,
                        tokenId: TokenId,
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

                    Fee = estimatedFee;
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

                    Fee = Math.Min(Fee, tezosConfig.GetMaximumFee());

                    if (xtzAddress.AvailableBalance() < Fee)
                    {
                        Warning = "Insufficient funds for fee!";
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens update fee error");
            }
        }

        protected virtual async void OnMaxClick()
        {
            try
            {
                var tezosConfig = _app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz);

                if (TokenContract == null || From == null)
                {
                    Amount = 0;
                    return;
                }

                if (!tezosConfig.IsValidAddress(TokenContract))
                {
                    Amount = 0;
                    Warning = "Invalid token contract address!";
                    return;
                }

                var fromTokenAddress = await GetTokenAddressAsync(
                    account: _app.Account,
                    address: From,
                    tokenContract: TokenContract,
                    tokenId: TokenId,
                    tokenType: _tokenType);

                if (fromTokenAddress == null)
                {
                    Amount = 0;
                    Warning = $"Insufficient token funds on address {From}! Please update your balance!";
                    return;
                }

                Amount = fromTokenAddress.Balance;
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens max click error");
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

        private async void UpdateCurrencyCode()
        {
            if (TokenContract == null || From == null)
                return;

            var tokenAddress = await GetTokenAddressAsync(
                account: _app.Account,
                address: From,
                tokenContract: TokenContract,
                tokenId: TokenId,
                tokenType: _tokenType);

            if (tokenAddress?.TokenBalance?.Symbol != null)
            {
                CurrencyCode = tokenAddress.TokenBalance.Symbol.ToUpper();
                CurrencyFormat = $"F{Math.Min(tokenAddress.TokenBalance.Decimals, AddressesHelper.MaxTokenCurrencyFormatDecimals)}";
                SelectedFromBalance = tokenAddress.AvailableBalance();
                this.RaisePropertyChanged(nameof(AmountString));
            }
            else
            {
                CurrencyCode = _app.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract)
                    ?.Name.ToUpper() ?? "TOKENS";
                CurrencyFormat = DefaultCurrencyFormat;
                this.RaisePropertyChanged(nameof(AmountString));
            }


            TokenPreview = GetTokenPreview(From, TokenId);
        }

        private async Task<Error> Send(CancellationToken cancellationToken = default)
        {
            var tokenAddress = await GetTokenAddressAsync(
                account: App.AtomexApp.Account,
                address: From,
                tokenContract: TokenContract,
                tokenId: TokenId,
                tokenType: _tokenType);

            if (tokenAddress.Currency == "FA12")
            {
                var currencyName = App.AtomexApp.Account.Currencies
                    .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == TokenContract)
                    ?.Name ?? "FA12";

                var tokenAccount = App.AtomexApp.Account.GetTezosTokenAccount<Fa12Account>(
                    currency: currencyName,
                    tokenContract: TokenContract,
                    tokenId: TokenId);

                return await tokenAccount.SendAsync(
                    from: tokenAddress.Address,
                    to: To,
                    amount: Amount,
                    fee: Fee,
                    useDefaultFee: UseDefaultFee,
                    cancellationToken: cancellationToken);
            }
            else
            {
                var tokenAccount = App.AtomexApp.Account.GetTezosTokenAccount<Fa2Account>(
                    currency: "FA2",
                    tokenContract: TokenContract,
                    tokenId: TokenId);

                var decimals = tokenAddress.TokenBalance.Decimals;
                var amount = Amount * (decimal)Math.Pow(10, decimals);
                var fee = (int)Fee.ToMicroTez();

                return await tokenAccount.SendAsync(
                    from: From,
                    to: To,
                    amount: amount,
                    tokenContract: TokenContract,
                    tokenId: (int)TokenId,
                    fee: fee,
                    useDefaultFee: UseDefaultFee,
                    cancellationToken: cancellationToken);
            }
        }

        private void DesignerMode()
        {
        }
    }
}