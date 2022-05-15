using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.ViewModels
{
    public class DelegateViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        private readonly TezosConfig _tezosConfig;
        private const string ChangeBakerTitle = "Change baker";
        private const string DelegateTitle = "Delegate 1 address to...";

        [Reactive] public string Title { get; set; }
        [Reactive] public string DelegateAddress { get; set; }
        [Reactive] public decimal DelegateAddressBalance { get; set; }
        [Reactive] public List<BakerViewModel>? BakersList { get; set; }
        private List<BakerViewModel>? InitialBakersList { get; set; }
        [Reactive] public BakerViewModel? SelectedBaker { get; set; }
        [Reactive] public decimal Fee { get; set; }
        [Reactive] public string BaseCurrencyFormat { get; set; }
        [Reactive] public string FeeFormat { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [Reactive] public string FeeCurrencyCode { get; set; }
        [Reactive] public string BaseCurrencyCode { get; set; }
        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public string Warning { get; set; }
        [Reactive] public bool ChoosenBakerIsOverdelegated { get; set; }
        [Reactive] public SendStage Stage { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public DelegationSortField? CurrentSortField { get; set; }
        [Reactive] public SortDirection? CurrentSortDirection { get; set; }
        [ObservableAsProperty] public bool IsSending { get; }


        private ReactiveCommand<Unit, Unit> _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand =>
            _backCommand ??= ReactiveCommand.Create(() => { App.DialogService.Close(); });

        private ReactiveCommand<Unit, Unit> _nextCommand;

        public ReactiveCommand<Unit, Unit> NextCommand =>
            _nextCommand ??= ReactiveCommand.CreateFromTask(CheckDelegation);

        private ReactiveCommand<Unit, Unit> _sendCommand;

        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.CreateFromTask(
            async () =>
            {
                await Task.Delay(Constants.DelayBeforeSendMs);
                await Send();
            });

        private ReactiveCommand<Unit, Unit> _undoConfirmStageCommand;

        public ReactiveCommand<Unit, Unit> UndoConfirmStageCommand => _undoConfirmStageCommand ??=
            ReactiveCommand.Create(
                () => { Stage = SendStage.Edit; });

        private ReactiveCommand<DelegationSortField, Unit> _setSortTypeCommand;

        public ReactiveCommand<DelegationSortField, Unit> SetSortTypeCommand =>
            _setSortTypeCommand ??= ReactiveCommand.Create<DelegationSortField>(sortField =>
            {
                if (CurrentSortField != sortField)
                    CurrentSortField = sortField;
                else
                    CurrentSortDirection = CurrentSortDirection == SortDirection.Asc
                        ? SortDirection.Desc
                        : SortDirection.Asc;
            });

        private ReactiveCommand<string, Unit> _copyCommand;

        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>(data =>
        {
            try
            {
                App.Clipboard.SetTextAsync(data);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

        private async Task CheckDelegation()
        {
            if (Stage == SendStage.Edit)
            {
                Stage = SendStage.Confirmation;
            }

            try
            {
                if (!_tezosConfig.IsValidAddress(SelectedBaker?.Address))
                {
                    Warning = Resources.SvInvalidAddressError;
                    return;
                }

                if (DelegateAddress == null)
                {
                    Warning = Resources.SvInvalidAddressError;
                    return;
                }

                if (Fee < 0)
                {
                    Warning = Resources.SvCommissionLessThanZeroError;
                    return;
                }

                if (Fee > DelegateAddressBalance - (SelectedBaker?.MinDelegation ?? 0))
                {
                    Warning = Resources.SvAvailableFundsError;
                    return;
                }

                var result = await GetDelegate();

                if (result.HasError)
                {
                    Warning = result.Error.Description;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Delegation check error");
            }
        }

        private async Task Send()
        {
            var wallet = (HdWallet)_app.Account.Wallet;
            var keyStorage = wallet.KeyStorage;

            var walletAddress = _app.Account
                .GetCurrencyAccount(TezosConfig.Xtz)
                .GetAddressAsync(DelegateAddress)
                .WaitForResult();

            var tezosAccount = _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            try
            {
                await tezosAccount.AddressLocker
                    .LockAsync(walletAddress.Address);

                // temporary fix: check operation sequence
                await TezosOperationsSequencer
                    .WaitAsync(walletAddress.Address, tezosAccount)
                    .ConfigureAwait(false);

                var tx = new TezosTransaction
                {
                    StorageLimit = _tezosConfig.StorageLimit,
                    GasLimit = _tezosConfig.GasLimit,
                    From = walletAddress.Address,
                    To = SelectedBaker!.Address,
                    Fee = Fee.ToMicroTez(),
                    Currency = _tezosConfig.Name,
                    CreationTime = DateTime.UtcNow,
                    UseRun = true,
                    UseOfflineCounter = true,
                    OperationType = OperationType.Delegation
                };

                using var securePublicKey = _app.Account.Wallet.GetPublicKey(
                    currency: _tezosConfig,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                var _ = await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: _tezosConfig,
                    headOffset: TezosConfig.HeadOffset);

                var signResult = await tx
                    .SignAsync(keyStorage, walletAddress, _tezosConfig);

                if (!signResult)
                {
                    Log.Error("Delegation transaction signing error");

                    App.DialogService.Show(
                        MessageViewModel.Error(
                            text: "Delegation transaction signing error",
                            backAction: () => App.DialogService.Show(this)));

                    return;
                }

                var result = await _tezosConfig.BlockchainApi
                    .TryBroadcastAsync(tx);

                if (result.Error != null)
                {
                    App.DialogService.Show(
                        MessageViewModel.Error(
                            text: result.Error.Description,
                            backAction: () => App.DialogService.Show(this)));

                    return;
                }
                
                App.DialogService.Show(
                    MessageViewModel.Success(
                        text: "Successful delegation, it will updated in delegations list very soon!",
                        _tezosConfig.TxExplorerUri,
                        result.Value,
                        nextAction: () => App.DialogService.Close()));
            }
            catch (HttpRequestException e)
            {
                App.DialogService.Show(
                    MessageViewModel.Error(
                        text: "A network error has occurred while sending delegation transaction, " +
                              "check your internet connection and try again.",
                        backAction: () => App.DialogService.Show(this)));

                Log.Error(e, "Delegation send network error");
            }
            catch (Exception e)
            {
                App.DialogService.Show(
                    MessageViewModel.Error(
                        text: "An error has occurred while delegation.",
                        backAction: () => App.DialogService.Show(this)));

                Log.Error(e, "Delegation send error");
            }
            finally
            {
                tezosAccount.AddressLocker.Unlock(walletAddress.Address);
            }
        }

        public DelegateViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public DelegateViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _tezosConfig = _app.Account.Currencies.Get<TezosConfig>(TezosConfig.Xtz);

            this.WhenAnyValue(vm => vm.Fee)
                .SubscribeInMainThread(f => { OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty); });


            this.WhenAnyValue(vm => vm.SearchPattern)
                .WhereNotNull()
                .Select(searchPattern => searchPattern.ToLower())
                .SubscribeInMainThread(searchPattern =>
                {
                    if (searchPattern == string.Empty)
                    {
                        BakersList = new List<BakerViewModel>(InitialBakersList);
                        CurrentSortField = null;
                        return;
                    }

                    BakersList = GetSortedBakersList(InitialBakersList)
                        .Where(baker => baker.Name.ToLower().Contains(searchPattern))
                        .ToList();
                });

            this.WhenAnyValue(
                    vm => vm.CurrentSortField,
                    vm => vm.CurrentSortDirection)
                .WhereAllNotNull()
                .Where(_ => BakersList != null)
                .SubscribeInMainThread(_ => BakersList = GetSortedBakersList(BakersList));

            this.WhenAnyValue(vm => vm.Stage)
                .Where(stage => stage == SendStage.Edit)
                .SubscribeInMainThread(_ => Warning = string.Empty);

            this.WhenAnyValue(vm => vm.SelectedBaker)
                .WhereNotNull()
                .SubscribeInMainThread(selectedBaker =>
                    ChoosenBakerIsOverdelegated = selectedBaker.StakingAvailable - DelegateAddressBalance < 0);

            SendCommand.IsExecuting.ToPropertyExInMainThread(this, vm => vm.IsSending);

            FeeFormat = _tezosConfig.FeeFormat;
            FeeCurrencyCode = _tezosConfig.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            UseDefaultFee = true;
            Stage = SendStage.Edit;
            CurrentSortDirection = SortDirection.Desc;

            SubscribeToServices();
            _ = LoadBakerList();
        }

        public void InitializeWith(Delegation delegation)
        {
            DelegateAddress = delegation.Address;
            DelegateAddressBalance = delegation.Balance;
            Stage = SendStage.Edit;

            if (delegation.Baker != null)
            {
                Title = ChangeBakerTitle;
                SelectedBaker = BakersList?
                    .FirstOrDefault(baker => baker.Address == delegation.Baker.Address);

                BakersList = new List<BakerViewModel>(BakersList?
                    .Select(baker =>
                    {
                        if (baker.Address == delegation.Baker.Address)
                            baker.IsCurrentlyActive = true;
                        return baker;
                    })!);
            }
            else
            {
                Title = DelegateTitle;
                SelectedBaker = null;
                BakersList = new List<BakerViewModel>(BakersList?
                    .Select(baker =>
                    {
                        if (baker.IsCurrentlyActive)
                            baker.IsCurrentlyActive = false;
                        return baker;
                    })!);
            }
        }

        private async Task LoadBakerList()
        {
            List<BakerViewModel>? bakers = null;

            try
            {
                await Task.Run(async () =>
                {
                    bakers = (await BbApi
                            .GetBakers(_app.Account.Network)
                            .ConfigureAwait(false))
                        .Select(bakerData => new BakerViewModel
                        {
                            Address = bakerData.Address,
                            Logo = bakerData.Logo,
                            Name = bakerData.Name,
                            Fee = bakerData.Fee,
                            Roi = bakerData.EstimatedRoi,
                            MinDelegation = bakerData.MinDelegation,
                            StakingAvailable = bakerData.StakingAvailable
                        })
                        .ToList();

                    bakers.ForEach(bakerVm => _ = App.ImageService.LoadImageFromUrl(bakerVm.Logo));
                });
            }
            catch (Exception e)
            {
                Log.Error(e.Message, "Error while fetching bakers list");
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BakersList = bakers;
                InitialBakersList = new List<BakerViewModel>(BakersList);
                //UseDefaultFee = _useDefaultFee;
            }, DispatcherPriority.Background);
        }

        private List<BakerViewModel> GetSortedBakersList(IEnumerable<BakerViewModel>? bakersList)
        {
            return CurrentSortField switch
            {
                DelegationSortField.ByRoi when CurrentSortDirection == SortDirection.Desc
                    => new List<BakerViewModel>(bakersList.OrderByDescending(baker => baker.Roi)),
                DelegationSortField.ByRoi when CurrentSortDirection == SortDirection.Asc
                    => new List<BakerViewModel>(bakersList.OrderBy(baker => baker.Roi)),

                DelegationSortField.ByMinTez when CurrentSortDirection == SortDirection.Desc
                    => new List<BakerViewModel>(bakersList.OrderByDescending(baker => baker.MinDelegation)),
                DelegationSortField.ByMinTez when CurrentSortDirection == SortDirection.Asc
                    => new List<BakerViewModel>(bakersList.OrderBy(baker => baker.MinDelegation)),

                DelegationSortField.ByValidator when CurrentSortDirection == SortDirection.Desc
                    => new List<BakerViewModel>(bakersList.OrderByDescending(baker => baker.Name)),
                DelegationSortField.ByValidator when CurrentSortDirection == SortDirection.Asc
                    => new List<BakerViewModel>(bakersList.OrderBy(baker => baker.Name)),

                _ => InitialBakersList
            };
        }


        private async Task<Result<bool>> GetDelegate(
            CancellationToken cancellationToken = default)
        {
            if (DelegateAddress == null)
                return new Error(Errors.InvalidWallets, "You don't have non-empty accounts.");

            JObject delegateData;

            try
            {
                var rpc = new Rpc(_tezosConfig.RpcNodeUri);

                delegateData = await rpc
                    .GetDelegate(SelectedBaker?.Address)
                    .ConfigureAwait(false);
            }
            catch
            {
                return new Error(Errors.WrongDelegationAddress, "Wrong delegation address.");
            }

            if (delegateData["deactivated"].Value<bool>())
                return new Error(Errors.WrongDelegationAddress, "Baker is deactivated. Pick another one.");

            var delegators = delegateData["delegated_contracts"]?.Values<string>();

            if (delegators != null && delegators.Contains(DelegateAddress))
                return new Error(Errors.AlreadyDelegated,
                    $"Already delegated from {DelegateAddress} to {SelectedBaker?.Address}.");

            try
            {
                var tx = new TezosTransaction
                {
                    StorageLimit = _tezosConfig.StorageLimit,
                    GasLimit = _tezosConfig.GasLimit,
                    From = DelegateAddress,
                    To = SelectedBaker?.Address,
                    Fee = 0,
                    Currency = _tezosConfig.Name,
                    CreationTime = DateTime.UtcNow,

                    UseRun = true,
                    UseOfflineCounter = false,
                    OperationType = OperationType.Delegation
                };

                var walletAddress = _app.Account
                    .GetCurrencyAccount(TezosConfig.Xtz)
                    .GetAddressAsync(DelegateAddress, cancellationToken)
                    .WaitForResult();

                using var securePublicKey = _app.Account.Wallet.GetPublicKey(
                    currency: _tezosConfig,
                    keyIndex: walletAddress.KeyIndex,
                    keyType: walletAddress.KeyType);

                var (isSuccess, isRunSuccess, hasReveal) = await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: _tezosConfig,
                    headOffset: TezosConfig.HeadOffset,
                    cancellationToken: cancellationToken);

                if (!isSuccess)
                    return new Error(Errors.TransactionCreationError, $"Autofill transaction failed.");

                if (UseDefaultFee)
                {
                    if (isRunSuccess)
                    {
                        Fee = tx.Fee;
                    }
                    else
                    {
                        return new Error(Errors.TransactionCreationError, $"Autofill transaction failed.");
                    }

                    if (Fee > DelegateAddressBalance)
                        return new Error(Errors.TransactionCreationError,
                            $"Insufficient funds at the address {DelegateAddress}.");
                }
                else
                {
                    if (isRunSuccess && Fee < tx.Fee)
                        return new Error(Errors.TransactionCreationError,
                            $"Fee less than minimum {tx.Fee.ToString(CultureInfo.InvariantCulture)}.");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill delegation error.");

                return new Error(Errors.TransactionCreationError, $"Autofill delegation error. Try again later.");
            }

            return true;
        }

        private void SubscribeToServices()
        {
            if (!_app.HasQuotesProvider) return;

            _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
            _app.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode);

            if (quote != null)
                FeeInBase = Fee.SafeMultiply(quote.Bid);
        }
        
        private void OnQuotesProviderAvailabilityChangedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider provider)
                return;

            if (provider.IsAvailable)
                _ = LoadBakerList();
        }

#if DEBUG
        private void DesignerMode()
        {
            BakersList = new List<BakerViewModel>()
            {
                new BakerViewModel()
                {
                    Logo = "https://api.baking-bad.org/logos/tezoshodl.png",
                    Name = "TezosHODL",
                    Address = "tz1sdfldjsflksjdlkf123sfa",
                    Fee = 5,
                    MinDelegation = 100.001m,
                    Roi = 6.56m,
                    StakingAvailable = -10000.000000m
                },
                new BakerViewModel()
                {
                    Logo = "https://api.baking-bad.org/logos/tezoshodl.png",
                    Name = "TezosHODL",
                    Address = "tz1sdfldjsflksjdlkf123sfa",
                    Fee = 5,
                    MinDelegation = 100.001m,
                    Roi = 6.56m,
                    StakingAvailable = 10000.000000m
                }
            };

            Fee = 5;
            FeeInBase = 123m;

            Title = ChangeBakerTitle;
            Stage = SendStage.Confirmation;
            SelectedBaker = BakersList.FirstOrDefault();

            DelegateAddress = "tz3bvNMQ95vfAYtG8193ymshqjSvmxiCUuR5";
            DelegateAddressBalance = 123.456789m;
            Warning = Resources.SvAvailableFundsError;
            ChoosenBakerIsOverdelegated = true;
        }
#endif
    }
}