using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Tezos.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.Cryptography;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Atomex.ViewModels;

namespace Atomex.Client.Desktop.ViewModels
{
    public class AddressViewModel : ViewModelBase, IWalletAddressViewModel
    {
        public WalletAddress WalletAddress { get; set; }
        public string Address => WalletAddress.Address;
        public string Type => KeyTypeToString(WalletAddress.KeyType);
        public string AddressExplorerUri { get; set; }
        public string Path { get; set; }
        public bool HasTokens { get; set; }
        public Action<string> CopyToClipboard { get; set; }
        public Action<string> OpenInExplorer { get; set; }
        public Action<string> ExportKey { get; set; }
        public Func<string, Task>? UpdateAddress { get; set; }
        public string Balance { get; set; }
        public TokenBalance? TokenBalance { get; set; }
        public string TokenBalanceString =>
            $"{TokenBalance?.GetTokenBalance() ?? 0} {TokenBalance?.Symbol ?? string.Empty}";

        [ObservableAsProperty] public bool IsUpdating { get; }

        public AddressViewModel()
        {
            UpdateAddressCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsUpdating);
        }

        private static string KeyTypeToString(int keyType) =>
            keyType switch
            {
                CurrencyConfig.StandardKey => "Standard",
                TezosConfig.Bip32Ed25519Key => "Atomex",
                _ => throw new NotSupportedException($"Key type {keyType} not supported.")
            };

        private ReactiveCommand<Unit, Unit>? _updateAddressCommand;
        public ReactiveCommand<Unit, Unit> UpdateAddressCommand => _updateAddressCommand ??=
            ReactiveCommand.CreateFromTask(async () =>
            {
                if (UpdateAddress == null) return;
                await UpdateAddress(Address);
            });

        private ReactiveCommand<Unit, Unit>? _copyCommand;
        public ReactiveCommand<Unit, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create(
            () => CopyToClipboard?.Invoke(Address));

        private ReactiveCommand<Unit, Unit>? _openInExplorerCommand;
        public ReactiveCommand<Unit, Unit> OpenInExplorerCommand => _openInExplorerCommand ??= ReactiveCommand.Create(
            () => OpenInExplorer?.Invoke(Address));

        private ReactiveCommand<Unit, Unit>? _exportKeyCommand;
        public ReactiveCommand<Unit, Unit> ExportKeyCommand => _exportKeyCommand ??= ReactiveCommand.Create(
            () => ExportKey?.Invoke(Address));
    }

    public class AddressesViewModel : ViewModelBase, IDisposable
    {
        private readonly IAtomexApp _app;
        private CurrencyConfig _currency;
        private readonly string? _tokenContract;
        private readonly BigInteger _tokenId;

        public bool HasTokens => _currency.Name == TezosConfig.Xtz && _tokenContract != null;
        [Reactive] public ObservableCollection<AddressViewModel> Addresses { get; set; }
        [Reactive] public AddressesSortField? CurrentSortField { get; set; }
        [Reactive] public SortDirection? CurrentSortDirection { get; set; }

        public AddressesViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public AddressesViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            string? tokenContract = null,
            BigInteger? tokenId = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _tokenContract = tokenContract;
            _tokenId = tokenId ?? BigInteger.Zero;

            this.WhenAnyValue(vm => vm.CurrentSortField, vm => vm.CurrentSortDirection)
                .WhereAllNotNull()
                .SubscribeInMainThread(_ => { ReloadAddresses(); });

            CurrentSortField = _tokenContract != null
                ? AddressesSortField.ByTokenBalance
                : AddressesSortField.ByPath;

            CurrentSortDirection = _tokenContract != null
                ? SortDirection.Desc
                : SortDirection.Asc;

            _app.LocalStorage.BalanceChanged += OnBalanceChangedEventHandler;
        }

        private async Task ReloadAddresses()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount(_currency.Name);

                var addresses = (await account
                    .GetAddressesAsync())
                    .ToList();

                var addressesViewModels = addresses.Select(a =>
                {
                    return new AddressViewModel
                    {
                        WalletAddress      = a,
                        Path               = a.KeyPath,
                        HasTokens          = HasTokens,
                        Balance            = $"{a.Balance.ToDecimal(_currency.Decimals).ToString(CultureInfo.InvariantCulture)} {_currency.Name}",
                        AddressExplorerUri = $"{_currency.AddressExplorerUri}{a.Address}",
                        CopyToClipboard    = address => App.Clipboard.SetTextAsync(address),
                        OpenInExplorer     = OpenInExplorer,
                        UpdateAddress      = UpdateAddress,
                        ExportKey          = ExportKey
                    };
                }).ToList();

                // token balances
                if (HasTokens)
                {
                    var tezosAccount = (TezosAccount)account;

                    (await tezosAccount
                        .LocalStorage
                        .GetTokenAddressesByContractAsync(_tokenContract))
                        .Where(w => w.TokenBalance.TokenId == _tokenId)
                        .Where(w => w.Balance != 0)
                        .ToList()
                        .ForEachDo(addressWithTokens =>
                        {
                            var addressViewModel = addressesViewModels
                                .FirstOrDefault(a => a.Address == addressWithTokens.Address);

                            if (addressViewModel == null)
                                return;

                            addressViewModel.TokenBalance = addressWithTokens.TokenBalance;
                        });
                }

                switch (CurrentSortField)
                {
                    case AddressesSortField.ByPath when CurrentSortDirection == SortDirection.Desc:
                        addressesViewModels.Sort(new KeyPathDescending<AddressViewModel>());
                        Addresses = new ObservableCollection<AddressViewModel>(addressesViewModels);
                        break;
                    case AddressesSortField.ByPath when CurrentSortDirection == SortDirection.Asc:
                        addressesViewModels.Sort(new KeyPathAscending<AddressViewModel>());
                        Addresses = new ObservableCollection<AddressViewModel>(addressesViewModels);
                        break;
                    case AddressesSortField.ByBalance when CurrentSortDirection == SortDirection.Desc:
                        Addresses = new ObservableCollection<AddressViewModel>(
                            addressesViewModels.OrderByDescending(a => a.WalletAddress.AvailableBalance()));
                        break;

                    case AddressesSortField.ByBalance when CurrentSortDirection == SortDirection.Asc:
                        Addresses = new ObservableCollection<AddressViewModel>(
                            addressesViewModels.OrderBy(a => a.WalletAddress.AvailableBalance()));
                        break;

                    case AddressesSortField.ByTokenBalance when CurrentSortDirection == SortDirection.Desc:
                        if (_tokenContract == null) break;
                        Addresses = new ObservableCollection<AddressViewModel>(
                            addressesViewModels.OrderByDescending(a => a.TokenBalance?.GetTokenBalance()));
                        break;

                    case AddressesSortField.ByTokenBalance when CurrentSortDirection == SortDirection.Asc:
                        if (_tokenContract == null) break;
                        Addresses = new ObservableCollection<AddressViewModel>(
                            addressesViewModels.OrderBy(a => a.TokenBalance?.GetTokenBalance()));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while load addresses.");
            }
        }

        private ReactiveCommand<AddressesSortField, Unit>? _setSortTypeCommand;
        public ReactiveCommand<AddressesSortField, Unit> SetSortTypeCommand =>
            _setSortTypeCommand ??= ReactiveCommand.Create<AddressesSortField>(sortField =>
            {
                if (CurrentSortField != sortField)
                    CurrentSortField = sortField;
                else
                    CurrentSortDirection = CurrentSortDirection == SortDirection.Asc
                        ? SortDirection.Desc
                        : SortDirection.Asc;
            });

        private async void OnBalanceChangedEventHandler(object? sender, BalanceChangedEventArgs args)
        {
            var needReload = _tokenContract == null
                ? args.Currencies.Contains(_currency.Name)
                : args is TokenBalanceChangedEventArgs eventArg && eventArg.Tokens.Contains((_tokenContract, _tokenId));

            if (needReload)
            {
                await ReloadAddresses();
            }
        }

        private void OpenInExplorer(string address)
        {
            try
            {
                if (Uri.TryCreate($"{_currency.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                    App.OpenBrowser(uri.ToString());
                else
                    Log.Error("Invalid uri for address explorer");
            }
            catch (Exception e)
            {
                Log.Error(e, "Open in explorer error");
            }
        }

        private async Task UpdateAddress(string address)
        {
            try
            {
                var updateTask = new WalletScanner(_app.Account)
                    .UpdateBalanceAsync(_currency.Name, address);

                await Task.WhenAll(Task.Delay(Constants.MinimalAddressUpdateTimeMs), updateTask);

                if (_currency.Name == TezosConfig.Xtz && _tokenContract != null)
                {
                    // update tezos token balance
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    await new TezosTokensWalletScanner(tezosAccount)
                        .UpdateBalanceAsync(address, _tokenContract, (int)_tokenId);
                 }

                _ = ReloadAddresses();
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Address balance update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "AddressesViewModel.OnUpdateClick");
                // todo: message to user!?
            }
        }

        private void ExportKey(string address)
        {
            try
            {
                App.DialogService.Show(MessageViewModel.Message(
                        title: "Warning",
                        text:
                        "Copying the private key to the clipboard may result in the loss of all your coins in the wallet. Are you sure you want to copy the private key?",
                        nextTitle: "Copy",
                        nextAction: async () =>
                        {
                            var walletAddress = await _app.Account
                                .GetAddressAsync(_currency.Name, address);

                            var hdWallet = (HdWallet)_app.Account.Wallet;

                            using var privateKey = hdWallet.KeyStorage.GetPrivateKey(
                                currency: _currency,
                                keyPath: walletAddress.KeyPath,
                                keyType: walletAddress.KeyType);

                            var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();

                            if (Currencies.IsBitcoinBased(_currency.Name))
                            {
                                var btcBasedConfig = (BitcoinBasedConfig)_currency;

                                var wif = new NBitcoin.Key(unsecuredPrivateKey)
                                    .GetWif(btcBasedConfig.Network)
                                    .ToWif();

                                _ = App.Clipboard.SetTextAsync(wif);
                            }
                            else if (Currencies.IsTezosBased(_currency.Name))
                            {
                                var base58 = unsecuredPrivateKey.Length == 32
                                    ? Base58Check.Encode(unsecuredPrivateKey, TezosPrefix.Edsk)
                                    : Base58Check.Encode(unsecuredPrivateKey, TezosPrefix.EdskSecretKey);

                                _ = App.Clipboard.SetTextAsync(base58);
                            }
                            else
                            {
                                var hex = Hex.ToHexString(unsecuredPrivateKey);

                                _ = App.Clipboard.SetTextAsync(hex);
                            }

                            App.DialogService.Show(MessageViewModel.Success(
                                text: "Private key successfully copied to clipboard.",
                                nextAction: () => App.DialogService.Close()
                            ));
                        }
                    )
                );
            }
            catch (Exception e)
            {
                Log.Error(e, "Private key export error");
            }
        }
        
        public void Dispose() => _app.LocalStorage.BalanceChanged -= OnBalanceChangedEventHandler;

        private void DesignerMode()
        {
            _currency = DesignTime.TestNetCurrencies.First();

            var walletAddress = new WalletAddress
            {
                Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM"
            };

            Addresses = new ObservableCollection<AddressViewModel>(
                new List<AddressViewModel>
                {
                    new AddressViewModel
                    {
                        WalletAddress = walletAddress,
                        Path = "m/44'/0'/0'/0/0",
                        Balance = 4.0000000.ToString(CultureInfo.InvariantCulture),
                    },
                    new AddressViewModel
                    {
                        WalletAddress = walletAddress,
                        Path = "m/44'/0'/0'/0/0",
                        Balance = 100.ToString(CultureInfo.InvariantCulture),
                    },
                    new AddressViewModel
                    {
                        WalletAddress = walletAddress,
                        Path = "m/44'/0'/0'/0/0",
                        Balance = 16.0000001.ToString(CultureInfo.InvariantCulture),
                    }
                });
        }
    }
}