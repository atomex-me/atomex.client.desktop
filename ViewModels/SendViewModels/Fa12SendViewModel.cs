﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using Atomex.MarketData.Abstract;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class Fa12SendViewModel : SendViewModel
    {
        public Fa12SendViewModel()
            : base()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Fa12SendViewModel(
            IAtomexApp app,
            CurrencyConfig currency)
            : base(app, currency)
        {
            SelectFromViewModel = new SelectAddressViewModel(
                _app.Account,
                _app.LocalStorage,
                Currency,
                SelectAddressMode.SendFrom)
            {
                BackAction = () => { App.DialogService.Show(this); },
                ConfirmAction = walletAddressViewModel =>
                {
                    From = walletAddressViewModel.Address;
                    SelectedFromBalance = walletAddressViewModel.Balance;

                    App.DialogService.Show(SelectToViewModel);
                }
            };

            SelectToViewModel = new SelectAddressViewModel(
                _app.Account,
                _app.LocalStorage,
                _app.Account.Currencies.Get<TezosConfig>(TezosConfig.Xtz))
            {
                BackAction = () => { App.DialogService.Show(SelectFromViewModel); },
                ConfirmAction = walletAddressViewModel =>
                {
                    To = walletAddressViewModel.Address;
                    App.DialogService.Show(this);
                }
            };
        }

        protected override void FromClick()
        {
            var selectFromViewModel = SelectFromViewModel as SelectAddressViewModel;

            selectFromViewModel!.ConfirmAction = walletAddressViewModel =>
            {
                From = walletAddressViewModel.Address;
                SelectedFromBalance = walletAddressViewModel.Balance;
                App.DialogService.Show(this);
            };
            
            selectFromViewModel.BackAction = () => App.DialogService.Show(this);
            App.DialogService.Show(selectFromViewModel);
        }

        protected override void ToClick()
        {
            SelectToViewModel.BackAction = () => App.DialogService.Show(this);
            App.DialogService.Show(SelectToViewModel);
        }

        protected override async Task UpdateAmount()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: TransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    Fee = maxAmountEstimation.Fee.ToTez();

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Value.Message;
                    WarningToolTip = maxAmountEstimation.ErrorHint;
                    WarningType = MessageType.Error;
                    return;
                }

                var from = await account.GetAddressAsync(From);

                if (Amount > maxAmountEstimation.Amount.FromTokens(from?.TokenBalance?.Decimals ?? 0))
                {
                    Warning = Resources.CvInsufficientFunds;
                    WarningToolTip = "";
                    WarningType = MessageType.Error;
                    return;
                }

                if (Fee < maxAmountEstimation.Fee.ToTez())
                {
                    Warning = Resources.CvLowFees;
                    WarningToolTip = "";
                    WarningType = MessageType.Error;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update amount error", Currency?.Description);
            }
        }

        protected override async Task UpdateFee()
        {
            try
            {
                if (!UseDefaultFee)
                {
                    var account = _app.Account
                        .GetCurrencyAccount<Fa12Account>(Currency.Name);

                    var maxAmountEstimation = await account
                        .EstimateMaxAmountToSendAsync(
                            from: From,
                            type: TransactionType.Output,
                            reserve: false);

                    if (maxAmountEstimation.Error != null)
                    {
                        Warning = maxAmountEstimation.Error.Value.Message;
                        WarningToolTip = maxAmountEstimation.ErrorHint;
                        WarningType = MessageType.Error;
                        return;
                    }

                    var from = await account.GetAddressAsync(From);

                    if (Amount > maxAmountEstimation.Amount.FromTokens(from?.TokenBalance?.Decimals ?? 0))
                    {
                        Warning = Resources.CvInsufficientFunds;
                        WarningToolTip = "";
                        WarningType = MessageType.Error;
                        return;
                    }

                    if (Fee < maxAmountEstimation.Fee.ToTez())
                    {
                        Warning = Resources.CvLowFees;
                        WarningToolTip = "";
                        WarningType = MessageType.Error;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: update fee error", Currency?.Description);
            }
        }

        protected override async Task OnMaxClick()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name);

                var maxAmountEstimation = await account
                    .EstimateMaxAmountToSendAsync(
                        from: From,
                        type: TransactionType.Output,
                        reserve: false);

                if (UseDefaultFee && maxAmountEstimation.Fee > 0)
                    Fee = maxAmountEstimation.Fee.ToTez();

                if (maxAmountEstimation.Error != null)
                {
                    Warning = maxAmountEstimation.Error.Value.Message;
                    WarningToolTip = maxAmountEstimation.ErrorHint;
                    WarningType = MessageType.Error;
                    Amount = 0;
                    return;
                }

                var from = await account.GetAddressAsync(From);

                Amount = maxAmountEstimation.Amount > 0
                    ? maxAmountEstimation.Amount.FromTokens(from?.TokenBalance?.Decimals ?? 0)
                    : 0;

                if (Fee < maxAmountEstimation.Fee.ToTez())
                {
                    Warning = Resources.CvLowFees;
                    WarningToolTip = "";
                    WarningType = MessageType.Error;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "{@currency}: max click error", Currency?.Description);
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AmountInBase = Amount.SafeMultiply(quote?.Bid ?? 0m);
                FeeInBase = Fee.SafeMultiply(xtzQuote?.Bid ?? 0m);
            });
        }

        protected override async Task<Error?> Send(CancellationToken cancellationToken = default)
        {
            var tokenConfig = (Fa12Config)Currency;
            var tokenContract = tokenConfig.TokenContractAddress;
            const int tokenId = 0;
            const string? tokenType = "FA12";

            var tokenAddress = await TezosTokensSendViewModel
                .GetTokenAddressAsync(
                    account: _app.Account,
                    address: From,
                    tokenContract: tokenContract,
                    tokenId: tokenId,
                    tokenType: tokenType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var currencyName = _app.Account.Currencies
                .FirstOrDefault(c => c is Fa12Config fa12 && fa12.TokenContractAddress == tokenContract)
                ?.Name ?? "FA12";

            var tokenAccount = _app.Account.GetCurrencyAccount<Fa12Account>(
                currency: currencyName,
                tokenContract: tokenContract,
                tokenId: tokenId);

            var (_, error) = await tokenAccount
                .SendAsync(
                    from: tokenAddress.Address,
                    to: To,
                    amount: AmountToSend.ToTokens(tokenAddress.TokenBalance.Decimals),
                    fee: Fee.ToMicroTez(),
                    useDefaultFee: UseDefaultFee,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return error;
        }
    }
}