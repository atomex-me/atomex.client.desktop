using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using Serilog;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.Properties;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Avalonia.Threading;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    internal class TezosTxFill
    {
        public TezosTransaction Tx { get; set; }
        public Error? Error { get; set; }
    }

    public class DelegateViewModel : ViewModelBase
    {
        private IAtomexApp App { get; }

        private readonly Tezos _tezos;
        private WalletAddress _walletAddress;

        public WalletAddress WalletAddress
        {
            get => _walletAddress;
            set
            {
                _walletAddress = value;
                OnPropertyChanged(nameof(WalletAddress));
            }
        }

        private int _walletAddressIndex;

        public int WalletAddressIndex
        {
            get => _walletAddressIndex;
            set
            {
                _walletAddressIndex = value;
                OnPropertyChanged(nameof(WalletAddressIndex));

                WalletAddress = FromAddressList.ElementAt(_walletAddressIndex).WalletAddress;
            }
        }

        private List<BakerViewModel> _fromBakersList;

        public List<BakerViewModel> FromBakersList
        {
            get => _fromBakersList;
            private set
            {
                _fromBakersList = value;
                OnPropertyChanged(nameof(FromBakersList));

                BakerViewModel = FromBakersList.FirstOrDefault();
            }
        }

        private bool _bakersLoading;

        public bool BakersLoading
        {
            get => _bakersLoading;
            set
            {
                _bakersLoading = value;
                OnPropertyChanged(nameof(BakersLoading));
            }
        }

        private List<WalletAddressViewModel> _fromAddressList;

        public List<WalletAddressViewModel> FromAddressList
        {
            get => _fromAddressList;
            private set
            {
                _fromAddressList = value;
                OnPropertyChanged(nameof(FromAddressList));
                
                if (_fromAddressList.Count > 0) 
                    WalletAddressIndex = 0; // First address;
            }
        }

        private BakerViewModel _bakerViewModel;

        public BakerViewModel BakerViewModel
        {
            get => _bakerViewModel;
            set
            {
                _bakerViewModel = value;
                OnPropertyChanged(nameof(BakerViewModel));

                if (_bakerViewModel != null)
                    Address = _bakerViewModel.Address;
                
            }
        }

        public string FeeString
        {
            get => Fee.ToString(_tezos.FeeFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                {
                    OnPropertyChanged(nameof(FeeString));
                    return;
                }

                Fee = fee.TruncateByFormat(_tezos.FeeFormat);
            }
        }

        private decimal _fee;

        public decimal Fee
        {
            get => _fee;
            set
            {
                _fee = value;

                if (!UseDefaultFee)
                {
                    var feeAmount = _fee;

                    if (feeAmount > _walletAddress.Balance)
                        feeAmount = _walletAddress.Balance;

                    _fee = feeAmount;

                    Warning = string.Empty;
                }
                
                OnPropertyChanged(nameof(FeeString));
                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
        }

        private string _baseCurrencyFormat;

        public string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set
            {
                _baseCurrencyFormat = value;
                OnPropertyChanged(nameof(BaseCurrencyFormat));
            }
        }

        private decimal _feeInBase;

        public decimal FeeInBase
        {
            get => _feeInBase;
            set
            {
                _feeInBase = value;
                OnPropertyChanged(nameof(FeeInBase));
            }
        }

        private string _feeCurrencyCode;

        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set
            {
                _feeCurrencyCode = value;
                OnPropertyChanged(nameof(FeeCurrencyCode));
            }
        }

        private string _baseCurrencyCode;

        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set
            {
                _baseCurrencyCode = value;
                OnPropertyChanged(nameof(BaseCurrencyCode));
            }
        }

        private bool _useDefaultFee;

        public bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                {
                    Task.Run(async () =>
                    {
                        TezosTxFill txFill = await FillTx(default);
                        if (txFill.Error?.Code == null)
                            Fee = txFill.Tx.Fee;
                    });
                }
            }
        }

        private string _address;

        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged(nameof(Address));

                var baker = FromBakersList.FirstOrDefault(b => b.Address == _address);

                if (baker == null)
                    BakerViewModel = null;
                else if (baker != BakerViewModel)
                    BakerViewModel = baker;
            }
        }

        private string _warning;

        public string Warning
        {
            get => _warning;
            set
            {
                _warning = value;
                OnPropertyChanged(nameof(Warning));
            }
        }

        private ICommand _backCommand;

        public ICommand BackCommand => _backCommand ??= ReactiveCommand.Create(() =>
        {
            Desktop.App.DialogService.CloseDialog();
        });

        private bool _delegationCheck;

        public bool DelegationCheck
        {
            get => _delegationCheck;
            set
            {
                _delegationCheck = value;
                OnPropertyChanged(nameof(DelegationCheck));
            }
        }

        private ICommand _nextCommand;

        public ICommand NextCommand => _nextCommand ??= ReactiveCommand.Create(async () =>
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

                if (!_tezos.IsValidAddress(Address))
                {
                    Warning = Resources.SvInvalidAddressError;
                    return;
                }

                if (Fee < 0)
                {
                    Warning = Resources.SvCommissionLessThanZeroError;
                    return;
                }

                /*
                if (xTezos.GetFeeAmount(Fee, FeePrice) > CurrencyViewModel.AvailableAmount) {
                    Warning = Resources.SvAvailableFundsError;
                    return;
                }*/

                var result = await GetDelegate();

                if (result.HasError)
                {
                    Warning = result.Error.Description;
                }
                else
                {
                    var isAmountLessThanMin = WalletAddress.Balance < (BakerViewModel?.MinDelegation ?? 0);
                    var confirmationViewModel = new DelegateConfirmationViewModel(App, _onDelegate)
                    {
                        DelegationVM        = this,
                        Currency            = _tezos,
                        WalletAddress       = WalletAddress,
                        UseDefaultFee       = UseDefaultFee,
                        From                = WalletAddress.Address,
                        To                  = Address,
                        IsAmountLessThanMin = isAmountLessThanMin, 
                        BaseCurrencyCode    = BaseCurrencyCode,
                        BaseCurrencyFormat  = BaseCurrencyFormat,
                        Fee                 = Fee,
                        FeeInBase           = FeeInBase,
                        CurrencyCode        = _tezos.FeeCode,
                        CurrencyFormat      = _tezos.FeeFormat
                    };
                    
                    Desktop.App.DialogService.Show(confirmationViewModel, customHeight: isAmountLessThanMin ? 370 : 240);
                }
            }
            finally
            {
                DelegationCheck = false;
            }
        });

        private readonly Action _onDelegate;

        public DelegateViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public DelegateViewModel(IAtomexApp app, Action onDelegate = null)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            _onDelegate = onDelegate;
            _tezos = App.Account.Currencies.Get<Tezos>("XTZ");

            FeeCurrencyCode = _tezos.FeeCode;
            BaseCurrencyCode = "USD";
            BaseCurrencyFormat = "$0.00";
            UseDefaultFee = true;

            SubscribeToServices();
            
            _ = LoadBakerList();

            PrepareWallet().WaitForResult();
        }

        private async Task LoadBakerList()
        {
            BakersLoading = true;
            List<BakerViewModel> bakers = null;
            
            try
            {
                await Task.Run(async () =>
                {
                    bakers = (await BbApi
                            .GetBakers(App.Account.Network)
                            .ConfigureAwait(false))
                        .Select(x => new BakerViewModel
                        {
                            Address = x.Address,
                            Logo = x.Logo,
                            Name = x.Name,
                            Fee = x.Fee,
                            MinDelegation = x.MinDelegation,
                            StakingAvailable = x.StakingAvailable
                        })
                        .ToList();

                    bakers.ForEach(bakerVM => Desktop.App.ImageService.LoadImage(bakerVM.Logo));
                });
            }
            catch (Exception e)
            {
                Log.Error(e.Message, "Error while fetching bakers list");
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BakersLoading = false;
                FromBakersList = bakers;
                UseDefaultFee = _useDefaultFee;
            }, DispatcherPriority.Background);
        }

        private async Task PrepareWallet(CancellationToken cancellationToken = default)
        {
            FromAddressList = (await App.Account
                    .GetUnspentAddressesAsync(_tezos.Name, cancellationToken).ConfigureAwait(false))
                .OrderByDescending(x => x.Balance)
                .Select(w => new WalletAddressViewModel(w, _tezos.Format))
                .ToList();

            if (!FromAddressList?.Any() ?? false)
            {
                Warning = "You don't have non-empty accounts";
                return;
            }

            WalletAddress = FromAddressList.FirstOrDefault().WalletAddress;
        }

        private async Task<Result<string>> GetDelegate(
            CancellationToken cancellationToken = default)
        {
            if (_walletAddress == null)
                return new Error(Errors.InvalidWallets, "You don't have non-empty accounts");

            JObject delegateData;

            try
            {
                var rpc = new Rpc(_tezos.RpcNodeUri);

                delegateData = await rpc
                    .GetDelegate(_address)
                    .ConfigureAwait(false);
            }
            catch
            {
                return new Error(Errors.WrongDelegationAddress, "Wrong delegation address");
            }

            if (delegateData["deactivated"].Value<bool>())
                return new Error(Errors.WrongDelegationAddress, "Baker is deactivated. Pick another one");

            var delegators = delegateData["delegated_contracts"]?.Values<string>();

            if (delegators.Contains(_walletAddress.Address))
                return new Error(Errors.AlreadyDelegated,
                    $"Already delegated from {_walletAddress.Address} to {_address}");

            try
            {
                TezosTxFill txFill = await FillTx(cancellationToken);
                if (txFill.Error != null)
                    return txFill.Error;

                if (UseDefaultFee)
                    Fee = txFill.Tx.Fee;
            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill delegation error");

                return new Error(Errors.TransactionCreationError, $"Autofill delegation error. Try again later");
            }

            return "Successful check";
        }

        private async Task<TezosTxFill> FillTx(CancellationToken cancellationToken)
        {
            var res = new TezosTxFill();
            var tx = new TezosTransaction
            {
                StorageLimit = _tezos.StorageLimit,
                GasLimit = _tezos.GasLimit,
                From = _walletAddress.Address,
                To = _address,
                Fee = Fee.ToMicroTez(),
                Currency = _tezos,
                CreationTime = DateTime.UtcNow,
                UseRun = true,
                UseOfflineCounter = false,
                OperationType = OperationType.Delegation
            };

            using var securePublicKey = App.Account.Wallet
                .GetPublicKey(_tezos, _walletAddress.KeyIndex);

            var isSuccess = await tx.FillOperationsAsync(
                securePublicKey: securePublicKey,
                headOffset: Tezos.HeadOffset,
                cancellationToken: cancellationToken);

            res.Tx = tx;

            if (!isSuccess)
                res.Error = new Error(Errors.TransactionCreationError, $"Autofill transaction failed");

            return res;
        }

        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(FeeCurrencyCode, BaseCurrencyCode);

            if (quote != null)
                FeeInBase = Fee * quote.Bid;
        }

        private void DesignerMode()
        {
            FromBakersList = new List<BakerViewModel>()
            {
                new BakerViewModel()
                {
                    Logo = "https://api.baking-bad.org/logos/tezoshodl.png",
                    Name = "TezosHODL",
                    Address = "tz1sdfldjsflksjdlkf123sfa",
                    Fee = 5,
                    MinDelegation = 10,
                    StakingAvailable = 10000.000000m
                }
            };

            BakerViewModel = FromBakersList.FirstOrDefault();

            _address = "tz1sdfldjsflksjdlkf123sfa";
            _fee = 5;
            _feeInBase = 123m;
        }
    }
}