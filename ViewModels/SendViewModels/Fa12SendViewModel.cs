﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class Fa12SendViewModel : SendViewModel
    {
        protected override void FromClick()
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateAmount(decimal amount)
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateFee(decimal fee)
        {
            throw new NotImplementedException();
        }

        protected override Task OnMaxClick()
        {
            throw new NotImplementedException();
        }

        protected override Task<Error> Send(SendConfirmationViewModel confirmationViewModel, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
    // public class Fa12SendViewModel : SendViewModel
    // {
    //     public string From { get; set; }
    //
    //     public Fa12SendViewModel()
    //         : base()
    //     {
    //     }
    //
    //     public Fa12SendViewModel(
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
    //                 fee: 0,
    //                 feePrice: 0,
    //                 reserve: false);
    //
    //             if (UseDefaultFee)
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
    //                 var estimatedFeeAmount = _amount != 0
    //                     ? await account.EstimateFeeAsync(
    //                         from: new FromAddress(From),
    //                         to: To,
    //                         amount: _amount,
    //                         type: BlockchainTransactionType.Output)
    //                     : 0;
    //
    //                 OnPropertyChanged(nameof(AmountString));
    //
    //                 _fee = estimatedFeeAmount ?? Currency.GetDefaultFee();
    //                 OnPropertyChanged(nameof(FeeString));
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
    //                 Fee = _fee;
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
    //                 if (_fee > CurrencyViewModel.AvailableAmount)
    //                     Warning = Resources.CvInsufficientFunds;
    //
    //                 return;
    //             }
    //
    //             if (!UseDefaultFee)
    //             {
    //                 var availableAmount = CurrencyViewModel.AvailableAmount;
    //
    //                 if (App.Account.GetCurrencyAccount(Currency.Name) is not IEstimatable account)
    //                     return; // todo: error?
    //
    //                 var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
    //                     from: new FromAddress(From),
    //                     to: _to,
    //                     type: BlockchainTransactionType.Output,
    //                     fee: 0,
    //                     feePrice: 0,
    //                     reserve: false);
    //
    //                 var estimatedFeeAmount = _amount != 0
    //                     ? await account.EstimateFeeAsync(
    //                         from: new FromAddress(From),
    //                         to: To,
    //                         amount: _amount,
    //                         type: BlockchainTransactionType.Output)
    //                     : 0;
    //
    //                 if (_amount > maxAmountEstimation.Amount)
    //                 {
    //                     if (_amount <= availableAmount)
    //                         Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //                     else
    //                         Warning = Resources.CvInsufficientFunds;
    //
    //                     return;
    //                 }
    //                 else if (estimatedFeeAmount == null || _fee < estimatedFeeAmount.Value)
    //                 {
    //                     Warning = Resources.CvLowFees;
    //                 }
    //
    //                 if (_fee > maxAmountEstimation.Fee)
    //                     Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //
    //                 OnPropertyChanged(nameof(FeeString));
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
    //             var maxAmountEstimation = await account.EstimateMaxAmountToSendAsync(
    //                 from: new FromAddress(From),
    //                 to: _to,
    //                 type: BlockchainTransactionType.Output,
    //                 fee: 0,
    //                 feePrice: 0,
    //                 reserve: UseDefaultFee);
    //
    //             if (UseDefaultFee)
    //             {
    //                 if (maxAmountEstimation.Amount > 0)
    //                     _amount = maxAmountEstimation.Amount;
    //                 else
    //                     Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //
    //                 OnPropertyChanged(nameof(AmountString));
    //
    //                 _fee = maxAmountEstimation.Fee;
    //                 OnPropertyChanged(nameof(FeeString));
    //             }
    //             else
    //             {
    //                 if (_fee < maxAmountEstimation.Fee)
    //                 {
    //                     Warning = Resources.CvLowFees;
    //                     if (_fee == 0)
    //                     {
    //                         _amount = 0;
    //                         OnPropertyChanged(nameof(AmountString));
    //                         return;
    //                     }
    //                 }
    //
    //                 _amount = maxAmountEstimation.Amount;
    //
    //                 if (maxAmountEstimation.Amount < availableAmount || _fee > maxAmountEstimation.Fee)
    //                     Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
    //
    //                 OnPropertyChanged(nameof(AmountString));
    //                 OnPropertyChanged(nameof(FeeString));
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
    //         var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);
    //
    //         AmountInBase = Amount * (quote?.Bid ?? 0m);
    //         FeeInBase = Fee * (xtzQuote?.Bid ?? 0m);
    //     }
    //
    //     protected override async Task<Error> Send(
    //         SendConfirmationViewModel confirmationViewModel,
    //         CancellationToken cancellationToken = default)
    //     {
    //         var tokenConfig = (Fa12Config)Currency;
    //         var tokenContract = tokenConfig.TokenContractAddress;
    //         var tokenId = 0;
    //         var tokenType = "FA12";
    //
    //         var tokenAddress = await TezosTokensSendViewModel.GetTokenAddressAsync(
    //             account: App.Account,
    //             address: From,
    //             tokenContract: tokenContract,
    //             tokenId: tokenId,
    //             tokenType: tokenType);
    //
    //         var currencyName = App.Account.Currencies
    //             .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == tokenContract)
    //             ?.Name ?? "FA12";
    //
    //         var tokenAccount = App.Account.GetTezosTokenAccount<Fa12Account>(
    //             currency: currencyName,
    //             tokenContract: tokenContract,
    //             tokenId: tokenId);
    //
    //         return await tokenAccount.SendAsync(
    //             from: tokenAddress.Address,
    //             to: confirmationViewModel.To,
    //             amount: confirmationViewModel.Amount,
    //             fee: confirmationViewModel.Fee,
    //             useDefaultFee: confirmationViewModel.UseDeafultFee,
    //             cancellationToken: cancellationToken);   
    //     }
    // }
}