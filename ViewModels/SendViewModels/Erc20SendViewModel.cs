using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Ethereum;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class Erc20SendViewModel : EthereumSendViewModel
    {
    }
    // {
    //     public override CurrencyConfig Currency
    //     {
    //         get => _currency;
    //         set
    //         {
    //             if (_currency != null && _currency != value)
    //             {
    //                 var sendViewModel = SendViewModelCreator.CreateViewModel(App, value);
    //
    //                 Desktop.App.DialogService.Show(sendViewModel);
    //                 return;
    //             }
    //
    //             _currency = value;
    //             OnPropertyChanged(nameof(Currency));
    //
    //             CurrencyViewModel = FromCurrencies.FirstOrDefault(c => c.Currency.Name == Currency.Name);
    //
    //             _amount = 0;
    //             OnPropertyChanged(nameof(AmountString));
    //
    //             _fee = 0;
    //             OnPropertyChanged(nameof(FeeString));
    //
    //             _feePrice = 0;
    //             OnPropertyChanged(nameof(FeePriceString));
    //
    //             OnPropertyChanged(nameof(TotalFeeString));
    //
    //             FeePriceFormat = _currency.FeePriceFormat;
    //             FeePriceCode = _currency.FeePriceCode;
    //
    //             Warning = string.Empty;
    //         }
    //     }
    //
    //     public override string TotalFeeCurrencyCode => Currency.FeeCurrencyName;
    //
    //     public override decimal FeePrice
    //     {
    //         get => _feePrice;
    //         set { UpdateFeePrice(value); }
    //     }
    //
    //     public Erc20SendViewModel()
    //         : base()
    //     {
    //     }
    //
    //     public Erc20SendViewModel(
    //         IAtomexApp app,
    //         CurrencyConfig currency)
    //         : base(app, currency)
    //     {
    //     }
    //
    //     public override bool UseDefaultFee
    //     {
    //         get => _useDefaultFee;
    //         set
    //         {
    //             Warning = string.Empty;
    //
    //             _useDefaultFee = value;
    //             OnPropertyChanged(nameof(UseDefaultFee));
    //
    //             Amount = _amount; // recalculate amount
    //         }
    //     }
    //
    //     protected override async void UpdateAmount(decimal amount)
    //     {
    //         if (IsAmountUpdating)
    //             return;
    //
    //         IsAmountUpdating = true;
    //
    //         var availableAmount = CurrencyViewModel.AvailableAmount;
    //         _amount = amount;
    //
    //         Warning = string.Empty;
    //
    //         try
    //         {
    //             if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
    //                 return; // todo: error?
    //
    //             var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
    //                 from: new FromAddress(From),
    //                 to: _to,
    //                 type: BlockchainTransactionType.Output,
    //                 fee: UseDefaultFee ? 0 : _fee,
    //                 feePrice: UseDefaultFee ? 0 : _feePrice,
    //                 reserve: false);
    //
    //             if (UseDefaultFee)
    //             {
    //                 _fee = Currency.GetDefaultFee();
    //                 OnPropertyChanged(nameof(GasString));
    //
    //                 _feePrice = await Currency.GetDefaultFeePriceAsync();
    //                 OnPropertyChanged(nameof(FeePriceString));
    //
    //                 if (_amount > maxAmountEstimation.Amount)
    //                 {
    //                     if (_amount <= availableAmount)
    //                         Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //                     else
    //                         Warning = Resources.CvInsufficientFunds;
    //
    //                     IsAmountUpdating = false;
    //                     return;
    //                 }
    //
    //                 OnPropertyChanged(nameof(AmountString));
    //
    //                 UpdateTotalFeeString();
    //                 OnPropertyChanged(nameof(TotalFeeString));
    //             }
    //             else
    //             {
    //                 if (_amount > maxAmountEstimation.Amount)
    //                 {
    //                     if (_amount <= availableAmount)
    //                         Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //                     else
    //                         Warning = Resources.CvInsufficientFunds;
    //
    //                     IsAmountUpdating = false;
    //                     return;
    //                 }
    //
    //                 OnPropertyChanged(nameof(AmountString));
    //
    //                 if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
    //                     Warning = Resources.CvLowFees;
    //             }
    //
    //             OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
    //         }
    //         finally
    //         {
    //             IsAmountUpdating = false;
    //         }
    //     }
    //
    //     private async void UpdateFeePrice(decimal value)
    //     {
    //         if (IsFeeUpdating)
    //             return;
    //
    //         IsFeeUpdating = true;
    //
    //         _feePrice = value;
    //
    //         Warning = string.Empty;
    //
    //         try
    //         {
    //             if (_amount == 0)
    //             {
    //                 if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
    //                     Warning = Resources.CvInsufficientFunds;
    //                 return;
    //             }
    //
    //             if (value == 0)
    //             {
    //                 Warning = Resources.CvLowFees;
    //                 UpdateTotalFeeString();
    //                 OnPropertyChanged(nameof(TotalFeeString));
    //                 return;
    //             }
    //
    //             if (!UseDefaultFee)
    //             {
    //                 if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
    //                     return; // todo: error?
    //
    //                 var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
    //                     from: new FromAddress(From),
    //                     to: _to,
    //                     type: BlockchainTransactionType.Output,
    //                     fee: _fee,
    //                     feePrice: _feePrice,
    //                     reserve: false);
    //
    //                 if (_amount > maxAmountEstimation.Amount)
    //                 {
    //                     var availableAmount = CurrencyViewModel.AvailableAmount;
    //
    //                     if(_amount <= availableAmount)
    //                         Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //                     else
    //                         Warning = Resources.CvInsufficientFunds;
    //
    //                     return;
    //                 }
    //
    //                 OnPropertyChanged(nameof(FeePriceString));
    //
    //                 UpdateTotalFeeString();
    //                 OnPropertyChanged(nameof(TotalFeeString));
    //             }
    //
    //             OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
    //         }
    //         finally
    //         {
    //             IsFeeUpdating = false;
    //         }
    //     }
    //
    //     protected override async void UpdateFee(decimal fee)
    //     {
    //         if (IsFeeUpdating)
    //             return;
    //
    //         IsFeeUpdating = true;
    //
    //         _fee = Math.Min(fee, Currency.GetMaximumFee());
    //
    //         Warning = string.Empty;
    //
    //         try
    //         {
    //             if (_amount == 0)
    //             {
    //                 if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
    //                     Warning = Resources.CvInsufficientFunds;
    //                 return;
    //             }
    //
    //             if (_fee < Currency.GetDefaultFee())
    //             {
    //                 Warning = Resources.CvLowFees;
    //                 if (fee == 0)
    //                 {
    //                     UpdateTotalFeeString();
    //                     OnPropertyChanged(nameof(TotalFeeString));
    //                     return;
    //                 }
    //             }
    //
    //             if (!UseDefaultFee)
    //             {
    //                 if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
    //                     return; // todo: error?
    //
    //                 var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
    //                     from: new FromAddress(From),
    //                     to: _to,
    //                     type: BlockchainTransactionType.Output,
    //                     fee: _fee,
    //                     feePrice: _feePrice,
    //                     reserve: false);
    //
    //                 if (_amount > maxAmountEstimation.Amount)
    //                 {
    //                     var availableAmount = CurrencyViewModel.AvailableAmount;
    //
    //                     if (_amount <= availableAmount)
    //                         Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //                     else
    //                         Warning = Resources.CvInsufficientFunds;
    //
    //                     return;
    //                 }
    //
    //                 OnPropertyChanged(nameof(FeePriceString));
    //
    //                 UpdateTotalFeeString();
    //                 OnPropertyChanged(nameof(TotalFeeString));
    //             }
    //
    //             OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
    //         }
    //         finally
    //         {
    //             IsFeeUpdating = false;
    //         }
    //     }        
    //
    //     protected override async void OnMaxClick()
    //     {
    //         if (IsAmountUpdating)
    //             return;
    //
    //         IsAmountUpdating = true;
    //
    //         Warning = string.Empty;
    //
    //         try
    //         {
    //             var availableAmount = CurrencyViewModel.AvailableAmount;
    //
    //             if (availableAmount == 0)
    //                 return;
    //
    //             if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
    //                 return; // todo: error?
    //
    //             if (UseDefaultFee)
    //             {
    //                 var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
    //                     from: new FromAddress(From),
    //                     to: _to,
    //                     type: BlockchainTransactionType.Output,
    //                     fee: 0,
    //                     feePrice: 0,
    //                     reserve: false);
    //
    //                 if (maxAmountEstimation.Amount > 0)
    //                     _amount = maxAmountEstimation.Amount;
    //                 else if(CurrencyViewModel.AvailableAmount > 0)
    //                     Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //
    //                 OnPropertyChanged(nameof(AmountString));
    //
    //                 _fee = Currency.GetDefaultFee();
    //                 OnPropertyChanged(nameof(GasString));
    //
    //                 _feePrice = await Currency.GetDefaultFeePriceAsync();
    //                 OnPropertyChanged(nameof(FeePriceString));
    //
    //                 UpdateTotalFeeString(maxAmountEstimation.Fee);
    //                 OnPropertyChanged(nameof(TotalFeeString));
    //             }
    //             else
    //             {
    //                 if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
    //                 {
    //                     Warning = Resources.CvLowFees;
    //                     if (_fee == 0 || _feePrice == 0)
    //                     {
    //                         _amount = 0;
    //                         OnPropertyChanged(nameof(AmountString));
    //                         return;
    //                     }
    //                 }
    //
    //                 var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
    //                     from: new FromAddress(From),
    //                     to: _to,
    //                     type: BlockchainTransactionType.Output,
    //                     fee: _fee,
    //                     feePrice: _feePrice,
    //                     reserve: false);
    //
    //                 _amount = maxAmountEstimation.Amount;
    //
    //                 if (maxAmountEstimation.Amount < availableAmount)
    //                     Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //
    //                 OnPropertyChanged(nameof(AmountString));
    //
    //                 UpdateTotalFeeString(maxAmountEstimation.Fee);
    //                 OnPropertyChanged(nameof(TotalFeeString));
    //             }
    //
    //             OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
    //         }
    //         finally
    //         {
    //             IsAmountUpdating = false;
    //         }
    //     }
    //
    //     protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
    //     {
    //         if (sender is not ICurrencyQuotesProvider quotesProvider)
    //             return;
    //
    //         var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
    //         var ethQuote = quotesProvider.GetQuote(Currency.FeeCurrencyName, BaseCurrencyCode);
    //
    //         AmountInBase = Amount * (quote?.Bid ?? 0m);
    //         FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (ethQuote?.Bid ?? 0m);
    //     }
    //
    //     protected override Task<Error> Send(
    //         SendConfirmationViewModel confirmationViewModel,
    //         CancellationToken cancellationToken = default)
    //     {
    //         var account = App.Account.GetCurrencyAccount<Erc20Account>(Currency.Name);
    //
    //         return account.SendAsync(
    //             from: confirmationViewModel.From,
    //             to: confirmationViewModel.To,
    //             amount: confirmationViewModel.Amount,
    //             gasLimit: confirmationViewModel.Fee,
    //             gasPrice: confirmationViewModel.FeePrice,
    //             useDefaultFee: confirmationViewModel.UseDeafultFee,
    //             cancellationToken: cancellationToken);
    //     }
    // }
}