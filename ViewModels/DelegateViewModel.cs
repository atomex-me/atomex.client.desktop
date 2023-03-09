using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using Netezos.Forging.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels
{
    public class DelegateViewModel : ViewModelBase, IDisposable
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
        [ObservableAsProperty] public bool IsChecking { get; }

        private ReactiveCommand<Unit, Unit> _backCommand;
        public ReactiveCommand<Unit, Unit> BackCommand =>
            _backCommand ??= ReactiveCommand.Create(() => { App.DialogService.Close(); });

        private ReactiveCommand<Unit, Unit> _checkDelegationCommand;
        public ReactiveCommand<Unit, Unit> CheckDelegationCommand =>
            _checkDelegationCommand ??= ReactiveCommand.CreateFromTask(CheckDelegation);

        private ReactiveCommand<Unit, Unit> _sendCommand;
        public ReactiveCommand<Unit, Unit> SendCommand => _sendCommand ??= ReactiveCommand.CreateFromTask(
            async () =>
            {
                await Task.Delay(Constants.DelayBeforeSendMs);
                await SendAsync(Fee);
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
                Stage = SendStage.Confirmation;

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

                var (result, error) = await GetDelegateAsync();

                if (error != null)
                    Warning = error.Value.Message;
            }
            catch (Exception e)
            {
                Log.Error(e, "Delegation check error");
            }
        }

        public async Task UndelegateAsync(string address)
        {
            DelegateAddress = address;
            SelectedBaker = null;

            var (tx, error) = await RunAutoFillOperationAsync(
                delegateAddress: DelegateAddress,
                bakerAddress: null,
                cancellationToken: default);

            if (error != null)
            {
                App.DialogService.Show(MessageViewModel.Message(
                    title: "Error",
                    nextTitle: "Close",
                    text: error.Value.Message,
                    nextAction: () => App.DialogService.Close()));

                return;
            }

            if (!tx.IsAutoFilled)
            {
                App.DialogService.Show(MessageViewModel.Message(
                    title: "Error",
                    nextTitle: "Close",
                    text: "Autofill transaction failed",
                    nextAction: () => App.DialogService.Close()));

                return;
            }

            var totalFee = tx
                .TotalFee()
                .ToTez();

            var messageViewModel = MessageViewModel.Message(
                title: "Confirm undelegating",
                text: "Are you sure you want to send transaction, " +
                      $"that will stop delegating {address} with fee {totalFee} {TezosConfig.Xtz}?",
                nextTitle: "Undelegate",
                nextAction: () => _ = SendAsync(totalFee));

            App.DialogService.Show(messageViewModel);
        }

        private async Task SendAsync(decimal fee, CancellationToken cancellationToken = default)
        {
            var tezosAccount = _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            try
            {
                var (result, error) = await tezosAccount
                    .DelegateAsync(
                        from: DelegateAddress,
                        @delegate: SelectedBaker?.Address,
                        fee: Blockchain.Tezos.Fee.FromNetwork(defaultValue: fee.ToMicroTez()),
                        gasLimit: GasLimit.FromNetwork((int)_tezosConfig.GasLimit),
                        storageLimit: StorageLimit.FromNetwork((int)_tezosConfig.StorageLimit),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (error != null)
                {
                    App.DialogService.Show(
                        MessageViewModel.Error(
                            text: error.Value.Message,
                            backAction: () => App.DialogService.Show(this)));

                    return;
                }

                var operationType = SelectedBaker?.Address != null
                    ? "delegated"
                    : "undelegated";

                App.DialogService.Show(
                    MessageViewModel.Success(
                        text: $"Successfully {operationType}, your delegations list will updated very soon!",
                        baseUrl: _tezosConfig.TxExplorerUri,
                        id: result?.OperationId,
                        nextAction: () => App.DialogService.Close()));
            }
            catch (HttpRequestException e)
            {
                App.DialogService.Show(
                    MessageViewModel.Error(
                        text: "A network error has occurred while sending delegation transaction, " +
                              "check your internet connection and try again",
                        backAction: () => App.DialogService.Show(this)));

                Log.Error(e, "Delegation send network error");
            }
            catch (Exception e)
            {
                App.DialogService.Show(
                    MessageViewModel.Error(
                        text: "An error has occurred while delegation",
                        backAction: () => App.DialogService.Show(this)));

                Log.Error(e, "Delegation send error");
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
            FeeFormat = _tezosConfig.FeeFormat;
            FeeCurrencyCode = _tezosConfig.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            UseDefaultFee = true;
            Stage = SendStage.Edit;
            CurrentSortDirection = SortDirection.Desc;

            this.WhenAnyValue(vm => vm.Fee)
                .SubscribeInMainThread(f => OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty));


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
            CheckDelegationCommand.IsExecuting.ToPropertyExInMainThread(this, vm => vm.IsChecking);

            SubscribeToServices();
            _ = LoadBakerListAsync();
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

        private async Task LoadBakerListAsync()
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


        private async Task<Result<bool>> GetDelegateAsync(
            CancellationToken cancellationToken = default)
        {
            if (DelegateAddress == null)
                return new Error(Errors.InvalidWallets, "You don't have non-empty accounts");

            TezosRpcDelegates delegateData;

            try
            {
                var rpc = new TezosRpc(_tezosConfig.GetRpcSettings());

                delegateData = await rpc
                    .GetDelegateAsync(SelectedBaker?.Address, cancellationToken);
            }
            catch (HttpRequestException)
            {
                return new Error(
                    Errors.InvalidConnection,
                    "A network error has occurred while checking baker data, " +
                    "check your internet connection and try again");
            }
            catch (Exception)
            {
                return new Error(Errors.WrongDelegationAddress, "Wrong delegation address");
            }

            if (delegateData.Deactivated)
                return new Error(Errors.WrongDelegationAddress, "Baker is deactivated. Pick another one");

            if (delegateData.DelegatedContracts.Contains(DelegateAddress))
                return new Error(Errors.AlreadyDelegated,
                    $"Already delegated from {DelegateAddress} to {SelectedBaker?.Address}");

            var (tx, error) = await RunAutoFillOperationAsync(
                delegateAddress: DelegateAddress,
                bakerAddress: SelectedBaker!.Address,
                cancellationToken: cancellationToken);

            if (error != null)
                return error;

            if (!tx.IsAutoFilled)
                return new Error(Errors.TransactionCreationError, "Autofill transaction failed");

            var totalFee = tx
                .TotalFee()
                .ToTez();

            if (UseDefaultFee)
            {
                Fee = totalFee;

                if (Fee > DelegateAddressBalance)
                    return new Error(Errors.TransactionCreationError,
                        $"Insufficient funds at the address {DelegateAddress}");
            }
            else
            {
                if (Fee < totalFee)
                    return new Error(Errors.TransactionCreationError,
                        $"Fee less than minimum {totalFee.ToString(CultureInfo.CurrentCulture)}");
            }

            return true;
        }

        private async Task<Result<TezosOperationRequest>> RunAutoFillOperationAsync(
            string delegateAddress,
            string? bakerAddress,
            CancellationToken cancellationToken)
        {
            try
            {
                var walletAddress = await _app.Account
                    .GetCurrencyAccount(TezosConfig.Xtz)
                    .GetAddressAsync(delegateAddress, cancellationToken)
                    .ConfigureAwait(false);

                using var securePublicKey = _app.Account.Wallet.GetPublicKey(
                    currency: _tezosConfig,
                    keyPath: walletAddress.KeyPath,
                    keyType: walletAddress.KeyType);

                var rpc = new TezosRpc(_tezosConfig.GetRpcSettings());

                var (request, error) = await TezosOperationFiller
                    .FillOperationAsync(
                        rpc: rpc,
                        operationsRequests: new List<TezosOperationParameters>
                        {
                            new TezosOperationParameters
                            {
                                Content = new DelegationContent
                                {
                                    Delegate     = bakerAddress,
                                    Fee          = 0,
                                    GasLimit     = (int)_tezosConfig.GasLimit,
                                    StorageLimit = (int)_tezosConfig.StorageLimit,
                                    Source       = delegateAddress
                                },
                                From         = delegateAddress,
                                Fee          = Blockchain.Tezos.Fee.FromNetwork(),
                                GasLimit     = GasLimit.FromNetwork(defaultValue: (int)_tezosConfig.GasLimit),
                                StorageLimit = StorageLimit.FromNetwork(defaultValue: (int)_tezosConfig.StorageLimit)
                            }
                        },
                        publicKey: securePublicKey.ToUnsecuredBytes(),
                        settings: _tezosConfig.GetFillOperationSettings(),
                        headOffset: 0,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (error != null)
                    return error;

                return request!;
            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill transaction error");

                return new Error(Errors.TransactionCreationError, "Autofill transaction error. Try again later");
            }
        }

        private void SubscribeToServices()
        {
            if (!_app.HasQuotesProvider)
                return;

            _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
            _app.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode);

            if (quote != null)
                FeeInBase = Fee.SafeMultiply(quote.Bid);
        }

        private void OnQuotesProviderAvailabilityChangedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not IQuotesProvider provider)
                return;

            if (provider.IsAvailable)
                _ = LoadBakerListAsync();
        }
        
        public void Dispose()
        {
            _app.QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
            _app.QuotesProvider.AvailabilityChanged -= OnQuotesProviderAvailabilityChangedEventHandler;
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