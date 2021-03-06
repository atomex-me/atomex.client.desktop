﻿using System;
using System.Windows.Input;
using Serilog;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Controls;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Core;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class DelegateConfirmationViewModel : ViewModelBase
    {
        private IAtomexApp App { get; }
        public Tezos Currency { get; set; }

        public WalletAddress WalletAddress { get; set; }
        public bool UseDefaultFee { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Fee { get; set; }
        public bool IsAmountLessThanMin { get; set; }
        public decimal FeeInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }

        private ICommand _backCommand;

        public ICommand BackCommand => _backCommand ??= ReactiveCommand.Create(() =>
        {
            Desktop.App.DialogService.Show(DelegationVM);
        });

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= ReactiveCommand.Create(Send);
        public DelegateViewModel DelegationVM { get; set; }

        private readonly Action _onDelegate;

#if DEBUG
        public DelegateConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public DelegateConfirmationViewModel(IAtomexApp app, Action onDelegate = null)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            _onDelegate = onDelegate;
        }

        private async void Send()
        {
            var wallet = (HdWallet) App.Account.Wallet;
            var keyStorage = wallet.KeyStorage;
            var tezos = Currency;

            var tezosAccount = App.Account
                .GetCurrencyAccount<TezosAccount>("XTZ");

            try
            {
                Desktop.App.DialogService.Show(new SendingViewModel());

                await tezosAccount.AddressLocker
                    .LockAsync(WalletAddress.Address);

                var tx = new TezosTransaction
                {
                    StorageLimit = Currency.StorageLimit,
                    GasLimit = Currency.GasLimit,
                    From = WalletAddress.Address,
                    To = To,
                    Fee = Fee.ToMicroTez(),
                    Currency = Currency,
                    CreationTime = DateTime.UtcNow,

                    UseRun = true,
                    UseOfflineCounter = true,
                    OperationType = OperationType.Delegation
                };

                using var securePublicKey = App.Account.Wallet
                    .GetPublicKey(Currency, WalletAddress.KeyIndex);

                await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    headOffset: Tezos.HeadOffset);

                var signResult = await tx
                    .SignAsync(keyStorage, WalletAddress, default);

                if (!signResult)
                {
                    Log.Error("Transaction signing error");

                    Desktop.App.DialogService.Show(
                        MessageViewModel.Error(
                            text: "Transaction signing error",
                            backAction: BackToConfirmation)
                    );

                    return;
                }

                var result = await tezos.BlockchainApi
                    .TryBroadcastAsync(tx);

                if (result.Error != null)
                {
                    Desktop.App.DialogService.Show(
                        MessageViewModel.Error(
                            text: result.Error.Description,
                            backAction: BackToConfirmation));

                    return;
                }


                Desktop.App.DialogService.Show(
                    MessageViewModel.Success(
                        text: $"Successful delegation!",
                        tezos.TxExplorerUri,
                        result.Value,
                        nextAction: () =>
                        {
                            Desktop.App.DialogService.CloseDialog();
                            Console.WriteLine("Invoking ondelegate!");
                            _onDelegate?.Invoke();
                        }));
            }
            catch (Exception e)
            {
                Desktop.App.DialogService.Show(
                    MessageViewModel.Error(
                        text: "An error has occurred while delegation.",
                        backAction: BackToConfirmation));


                Log.Error(e, "delegation send error.");
            }
            finally
            {
                tezosAccount.AddressLocker.Unlock(WalletAddress.Address);
            }
        }

        private void BackToConfirmation()
        {
            Desktop.App.DialogService.Show(this, customHeight: IsAmountLessThanMin ? 370 : 240);
        }

        private void DesignerMode()
        {
            From = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            To = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            Fee = 0.0001m;
            FeeInBase = 8.43m;
        }
    }
}