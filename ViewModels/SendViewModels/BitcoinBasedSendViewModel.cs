﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Controls;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Atomex.Wallet.BitcoinBased;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        protected decimal _feeRate;
        public decimal FeeRate
        {
            get => _feeRate;
            set { _feeRate = value; OnPropertyChanged(nameof(FeeRate)); }
        }

        private BitcoinBasedCurrency BtcBased => Currency as BitcoinBasedCurrency;

        public BitcoinBasedSendViewModel()
            : base()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                BitcoinBasedDesignerMode();
#endif
        }

        public BitcoinBasedSendViewModel(
            IAtomexApp app,
            Currency currency)
            : base(app, currency)
        {
        }

        protected override async void UpdateAmount(decimal amount)
        {
            IsAmountUpdating = true;

            _amount = amount;
            Warning = string.Empty;

            try
            {
                if (UseDefaultFee)
                {
                    var (maxAmount, _, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    var estimatedFeeAmount = _amount != 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = estimatedFeeAmount ?? Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(FeeString));

                    FeeRate = await BtcBased.GetFeeRateAsync();
                }
                else
                {
                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    if (_amount + _fee > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    OnPropertyChanged(nameof(AmountString));

                    Fee = _fee;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected override async void UpdateFee(decimal fee)
        {
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            _fee = Math.Min(fee, Currency.GetMaximumFee());
            Warning = string.Empty;

            try
            {
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (_amount == 0)
                {
                    var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                    if (Currency.GetFeeAmount(_fee, defaultFeePrice) > availableAmount)
                        Warning = Resources.CvInsufficientFunds;

                    IsFeeUpdating = true;
                    return;
                }
                else if (_amount + _fee > availableAmount)
                {
                    Warning = Resources.CvInsufficientFunds;
                    IsFeeUpdating = false;
                    return;
                }

                var estimatedTxSize = await EstimateTxSizeAsync(_amount, _fee);
                
                if (estimatedTxSize == null || estimatedTxSize.Value == 0)
                {
                    Warning = Resources.CvInsufficientFunds;
                    IsFeeUpdating = false;
                    return;
                }

                if (!UseDefaultFee)
                {
                    var minimumFeeSatoshi = BtcBased.GetMinimumFee(estimatedTxSize.Value);
                    var minimumFee = BtcBased.SatoshiToCoin(minimumFeeSatoshi);

                    if (_fee < minimumFee)
                        Warning = Resources.CvLowFees;
                }

                FeeRate = BtcBased.CoinToSatoshi(_fee) / estimatedTxSize.Value;

                OnPropertyChanged(nameof(AmountString));
                OnPropertyChanged(nameof(FeeString));

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected override async void OnMaxClick()
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;
            Warning = string.Empty;

            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                if (UseDefaultFee) // auto fee
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    if (maxAmount > 0)
                        _amount = maxAmount;

                    OnPropertyChanged(nameof(AmountString));

                    var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));

                    FeeRate = await BtcBased.GetFeeRateAsync();
                }
                else // manual fee
                {
                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    if (availableAmount - _fee > 0)
                    {
                        _amount = availableAmount - _fee;
                    }
                    else
                    {
                        _amount = 0;
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;

                        OnPropertyChanged(nameof(AmountString));

                        return;
                    }

                    var estimatedTxSize = await EstimateTxSizeAsync(_amount, _fee);

                    if (estimatedTxSize == null || estimatedTxSize.Value == 0)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    FeeRate = BtcBased.CoinToSatoshi(_fee) / estimatedTxSize.Value;
                }

                OnPropertyChanged(nameof(AmountString));
                OnPropertyChanged(nameof(FeeString));

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        private async Task<int?> EstimateTxSizeAsync(
            decimal amount,
            decimal fee,
            CancellationToken cancellationToken = default)
        {
            return await App.Account
                .GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name)
                .EstimateTxSizeAsync(amount, fee, cancellationToken);
        }

        private void BitcoinBasedDesignerMode()
        {
            _feeRate = 200;
        }
    }
}