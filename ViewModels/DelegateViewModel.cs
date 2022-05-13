using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using Atomex.ViewModels;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.ViewModels
{
    internal class TezosTxFill
    {
        public TezosTransaction? Tx { get; set; }
        public Error? Error { get; set; }
    }

    public class DelegateViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        private readonly TezosConfig _tezosConfig;

        [Reactive] public WalletAddressViewModel? SelectedAddress { get; set; }
        [Reactive] public int WalletAddressIndex { get; set; }
        [Reactive] public List<BakerViewModel>? BakersList { get; set; }
        [Reactive] public List<BakerViewModel>? InitialBakersList { get; set; }
        [Reactive] public bool BakersLoading { get; set; }
        [Reactive] public List<WalletAddressViewModel> FromAddressList { get; set; }
        [Reactive] public BakerViewModel? BakerViewModel { get; set; }
        [Reactive] public decimal Fee { get; set; }
        [Reactive] public string BaseCurrencyFormat { get; set; }
        [Reactive] public string FeeFormat { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [Reactive] public string FeeCurrencyCode { get; set; }
        [Reactive] public string BaseCurrencyCode { get; set; }
        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public bool DelegationCheck { get; set; }
        [Reactive] public string Address { get; set; }
        [Reactive] public string Warning { get; set; }
        [Reactive] public SendStage Stage { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public DelegationSortField? CurrentSortField { get; set; }
        [Reactive] public SortDirection? CurrentSortDirection { get; set; }


        private ReactiveCommand<Unit, Unit> _backCommand;

        public ReactiveCommand<Unit, Unit> BackCommand =>
            _backCommand ??= ReactiveCommand.Create(() => { App.DialogService.Close(); });

        private ReactiveCommand<Unit, Unit> _nextCommand;

        public ReactiveCommand<Unit, Unit> NextCommand => _nextCommand ??= ReactiveCommand.CreateFromTask(async () =>
        {
            if (DelegationCheck)
                return;

            DelegationCheck = true;

            try
            {
                if (string.IsNullOrEmpty(Address))
                {
                    Warning = Resources.SvEmptyAddressError;
                    return;
                }

                if (!_tezosConfig.IsValidAddress(Address))
                {
                    Warning = Resources.SvInvalidAddressError;
                    return;
                }

                if (SelectedAddress == null)
                {
                    Warning = Resources.SvInvalidAddressError;
                    return;
                }

                if (Fee < 0)
                {
                    Warning = Resources.SvCommissionLessThanZeroError;
                    return;
                }

                if (Fee > SelectedAddress.AvailableBalance - (BakerViewModel?.MinDelegation ?? 0))
                {
                    Warning = Resources.SvAvailableFundsError;
                    return;
                }

                var result = await GetDelegate();

                if (result.HasError)
                {
                    Warning = result.Error.Description;
                }
                else
                {
                    var walletAddress = _app.Account
                        .GetCurrencyAccount(TezosConfig.Xtz)
                        .GetAddressAsync(SelectedAddress.Address)
                        .WaitForResult();

                    var isAmountLessThanMin = SelectedAddress.AvailableBalance < (BakerViewModel?.MinDelegation ?? 0);

                    var confirmationViewModel = new DelegateConfirmationViewModel(_app, _onDelegate)
                    {
                        DelegationVM = this,
                        Currency = _tezosConfig,
                        WalletAddress = walletAddress,
                        UseDefaultFee = UseDefaultFee,
                        From = SelectedAddress.Address,
                        To = Address,
                        IsAmountLessThanMin = isAmountLessThanMin,
                        BaseCurrencyCode = BaseCurrencyCode,
                        BaseCurrencyFormat = BaseCurrencyFormat,
                        Fee = Fee,
                        FeeInBase = FeeInBase,
                        CurrencyCode = _tezosConfig.FeeCode,
                        CurrencyFormat = _tezosConfig.FeeFormat
                    };

                    App.DialogService.Show(confirmationViewModel);
                }
            }
            finally
            {
                DelegationCheck = false;
            }
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

        private readonly Action _onDelegate;

        public DelegateViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public DelegateViewModel(IAtomexApp app, Action onDelegate = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            _onDelegate = onDelegate;
            _tezosConfig = _app.Account.Currencies.Get<TezosConfig>(TezosConfig.Xtz);

            this.WhenAnyValue(vm => vm.WalletAddressIndex)
                .SubscribeInMainThread(i => SelectedAddress = FromAddressList?.ElementAt(i));

            this.WhenAnyValue(vm => vm.BakersList)
                .SubscribeInMainThread(l => BakerViewModel = l?.FirstOrDefault());

            this.WhenAnyValue(vm => vm.FromAddressList)
                .SubscribeInMainThread(l =>
                {
                    if (l != null && l.Count > 0)
                        WalletAddressIndex = 0; // First address;)
                });

            this.WhenAnyValue(vm => vm.BakerViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(bvm =>
                {
                    Address = bvm.Address;

                    if (SelectedAddress != null)
                        CheckDelegateAsync();
                });

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .SubscribeInMainThread(f =>
                {
                    if (UseDefaultFee && SelectedAddress != null)
                        CheckDelegateAsync();
                });

            this.WhenAnyValue(vm => vm.Fee)
                .SubscribeInMainThread(f => { OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty); });

            this.WhenAnyValue(vm => vm.Address)
                .SubscribeInMainThread(a =>
                {
                    var baker = BakersList?.FirstOrDefault(b => b.Address == a);

                    if (baker == null)
                        BakerViewModel = null;
                    else if (baker != BakerViewModel)
                        BakerViewModel = baker;
                });

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

            FeeFormat = _tezosConfig.FeeFormat;
            FeeCurrencyCode = _tezosConfig.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            UseDefaultFee = true;
            Stage = SendStage.Edit;
            CurrentSortDirection = SortDirection.Desc;

            SubscribeToServices();
            _ = LoadBakerList();
            PrepareWallet().WaitForResult();
        }

        private async Task LoadBakerList()
        {
            BakersLoading = true;
            List<BakerViewModel>? bakers = null;

            try
            {
                await Task.Run(async () =>
                {
                    bakers = (await BbApi
                            .GetBakers(_app.Account.Network)
                            .ConfigureAwait(false))
                        .Select(x => new BakerViewModel
                        {
                            Address = x.Address,
                            Logo = x.Logo,
                            Name = x.Name,
                            Fee = x.Fee,
                            Roi = x.EstimatedRoi,
                            MinDelegation = x.MinDelegation,
                            StakingAvailable = x.StakingAvailable
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
                BakersLoading = false;
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

        private async Task PrepareWallet()
        {
            FromAddressList = (await _app.Account
                    .GetUnspentAddressesAsync(_tezosConfig.Name)
                    .ConfigureAwait(false))
                .OrderByDescending(x => x.Balance)
                .Select(w => new WalletAddressViewModel
                {
                    Address = w.Address,
                    AvailableBalance = w.AvailableBalance(),
                    CurrencyFormat = _tezosConfig.Format
                })
                .ToList();

            if (!FromAddressList?.Any() ?? false)
            {
                Warning = "You don't have non-empty accounts";
                return;
            }

            SelectedAddress = FromAddressList.FirstOrDefault();
        }

        private async void CheckDelegateAsync()
        {
            try
            {
                if (DelegationCheck)
                    return;

                DelegationCheck = true;
                Warning = string.Empty;

                var result = await GetDelegate();

                if (result.HasError)
                    Warning = result.Error.Description;
            }
            finally
            {
                DelegationCheck = false;
            }
        }

        private async Task<Result<bool>> GetDelegate(
            CancellationToken cancellationToken = default)
        {
            if (SelectedAddress == null)
                return new Error(Errors.InvalidWallets, "You don't have non-empty accounts.");

            JObject delegateData;

            try
            {
                var rpc = new Rpc(_tezosConfig.RpcNodeUri);

                delegateData = await rpc
                    .GetDelegate(Address)
                    .ConfigureAwait(false);
            }
            catch
            {
                return new Error(Errors.WrongDelegationAddress, "Wrong delegation address.");
            }

            if (delegateData["deactivated"].Value<bool>())
                return new Error(Errors.WrongDelegationAddress, "Baker is deactivated. Pick another one.");

            var delegators = delegateData["delegated_contracts"]?.Values<string>();

            if (delegators != null && delegators.Contains(SelectedAddress.Address))
                return new Error(Errors.AlreadyDelegated,
                    $"Already delegated from {SelectedAddress.Address} to {Address}.");

            try
            {
                var tx = new TezosTransaction
                {
                    StorageLimit = _tezosConfig.StorageLimit,
                    GasLimit = _tezosConfig.GasLimit,
                    From = SelectedAddress.Address,
                    To = Address,
                    Fee = 0,
                    Currency = _tezosConfig.Name,
                    CreationTime = DateTime.UtcNow,

                    UseRun = true,
                    UseOfflineCounter = false,
                    OperationType = OperationType.Delegation
                };

                var walletAddress = _app.Account
                    .GetCurrencyAccount(TezosConfig.Xtz)
                    .GetAddressAsync(SelectedAddress.Address, cancellationToken)
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

                    if (Fee > SelectedAddress.AvailableBalance)
                        return new Error(Errors.TransactionCreationError,
                            $"Insufficient funds at the address {SelectedAddress.Address}.");
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
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object? sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode);

            if (quote != null)
                FeeInBase = Fee.SafeMultiply(quote.Bid);
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
                    StakingAvailable = 10000.000000m
                }
            };

            BakerViewModel = BakersList.FirstOrDefault();

            Address = "tz1sdfldjsflksjdlkf123sfa";
            Fee = 5;
            FeeInBase = 123m;
        }
#endif
    }
}