using System;
using System.Windows.Input;

using Serilog;
using ReactiveUI;

using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Desktop.Common;
using Atomex.Core;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels
{
    public class DelegateConfirmationViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        public TezosConfig Currency { get; set; }
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
            App.DialogService.Show(DelegationVM);
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
            _app = app ?? throw new ArgumentNullException(nameof(app));

            _onDelegate = onDelegate;
        }

        private async void Send()
        {
            var wallet = (HdWallet) _app.Account.Wallet;
            var keyStorage = wallet.KeyStorage;
            var tezos = Currency;

            var tezosAccount = _app.Account
                .GetCurrencyAccount<TezosAccount>("XTZ");

            try
            {
                App.DialogService.Show(
                    MessageViewModel.Message(title: "Sending, please wait", withProgressBar: true));

                await tezosAccount.AddressLocker
                    .LockAsync(WalletAddress.Address);

                // temporary fix: check operation sequence
                await TezosOperationsSequencer
                    .WaitAsync(WalletAddress.Address, tezosAccount)
                    .ConfigureAwait(false);

                var tx = new TezosTransaction
                {
                    StorageLimit = Currency.StorageLimit,
                    GasLimit     = Currency.GasLimit,
                    From         = WalletAddress.Address,
                    To           = To,
                    Fee          = Fee.ToMicroTez(),
                    Currency     = Currency.Name,
                    CreationTime = DateTime.UtcNow,

                    UseRun            = true,
                    UseOfflineCounter = true,
                    OperationType     = OperationType.Delegation
                };

                using var securePublicKey = _app.Account.Wallet.GetPublicKey(
                    currency: Currency,
                    keyIndex: WalletAddress.KeyIndex,
                    keyType: WalletAddress.KeyType);

                var _ = await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: Currency,
                    headOffset: TezosConfig.HeadOffset);

                var signResult = await tx
                    .SignAsync(keyStorage, WalletAddress, Currency);

                if (!signResult)
                {
                    Log.Error("Transaction signing error");

                    App.DialogService.Show(
                        MessageViewModel.Error(
                            text: "Transaction signing error",
                            backAction: BackToConfirmation));

                    return;
                }

                var result = await tezos.BlockchainApi
                    .TryBroadcastAsync(tx);

                if (result.Error != null)
                {
                    App.DialogService.Show(
                        MessageViewModel.Error(
                            text: result.Error.Description,
                            backAction: BackToConfirmation));

                    return;
                }

                App.DialogService.Show(
                    MessageViewModel.Success(
                        text: $"Successful delegation!",
                        tezos.TxExplorerUri,
                        result.Value,
                        nextAction: () =>
                        {
                            App.DialogService.Close();

                            _onDelegate?.Invoke();
                        }));
            }
            catch (Exception e)
            {
                App.DialogService.Show(
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
            App.DialogService.Show(this);
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