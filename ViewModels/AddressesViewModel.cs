using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using System.Reactive;
using System.Threading.Tasks;
using Serilog;
using ReactiveUI;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.Cryptography;
using Atomex.ViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels
{
    public enum AddressesSortField
    {
        ByPath,
        ByBalance,
    }

    public class AddressViewModel : ViewModelBase
    {
        public string Address { get; set; }
        public string ShortenedAddress => Address?.TruncateAddress(15, 12);
        public string Type { get; set; }
        public string Path { get; set; }
        public bool HasTokens { get; set; }
        public Action<string> CopyToClipboard { get; set; }
        public Action<string> OpenInExplorer { get; set; }
        public Action<string> ExportKey { get; set; }
        public Func<string, Task>? UpdateAddress { get; set; }

        [Reactive] public string Balance { get; set; }
        [Reactive] public string TokenBalance { get; set; }
        [Reactive] public bool IsUpdating { get; set; }

        private ReactiveCommand<Unit, Unit> _updateAddressCommand;

        public ReactiveCommand<Unit, Unit> UpdateAddressCommand => _updateAddressCommand ??=
            ReactiveCommand.CreateFromTask(async () =>
            {
                if (UpdateAddress == null) return;
                IsUpdating = true;
                await UpdateAddress(Address);
                IsUpdating = false;
            });

        private ReactiveCommand<Unit, Unit> _copyCommand;

        public ReactiveCommand<Unit, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.Create(
            () => CopyToClipboard?.Invoke(Address));

        private ReactiveCommand<Unit, Unit> _openInExplorerCommand;

        public ReactiveCommand<Unit, Unit> OpenInExplorerCommand => _openInExplorerCommand ??= ReactiveCommand.Create(
            () => OpenInExplorer?.Invoke(Address));

        private ReactiveCommand<Unit, Unit> _exportKeyCommand;

        public ReactiveCommand<Unit, Unit> ExportKeyCommand => _exportKeyCommand ??= ReactiveCommand.Create(
            () => ExportKey?.Invoke(Address));
    }

    public class AddressesViewModel : ViewModelBase
    {
        private const int DefaultTokenPrecision = 9;
        private const int MinimalUpdateTimeMs = 1000;
        private readonly IAtomexApp _app;
        private CurrencyConfig _currency;
        private readonly string? _tokenContract;

        public bool HasTokens => _currency.Name == TezosConfig.Xtz && _tokenContract != null;
        [Reactive] public ObservableCollection<AddressViewModel> Addresses { get; set; }
        [Reactive] public bool SortByPathAndAsc { get; set; }
        [Reactive] public bool SortByPathAndDesc { get; set; }
        [Reactive] public bool SortByBalanceAndAsc { get; set; }
        [Reactive] public bool SortByBalanceAndDesc { get; set; }
        [Reactive] public bool SortByPath { get; set; }
        [Reactive] public bool SortByBalance { get; set; }
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
            string tokenContract = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _tokenContract = tokenContract;

            this.WhenAnyValue(vm => vm.CurrentSortField, vm => vm.CurrentSortDirection)
                .WhereAllNotNull()
                .SubscribeInMainThread(_ => { ReloadAddresses(); });

            CurrentSortField = AddressesSortField.ByPath;
            CurrentSortDirection = SortDirection.Asc;

            SubscribeToServices();
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

                switch (CurrentSortField)
                {
                    case AddressesSortField.ByPath when CurrentSortDirection == SortDirection.Desc:
                        addresses.Sort((a2, a1) =>
                        {
                            var typeResult = a1.KeyType.CompareTo(a2.KeyType);

                            if (typeResult != 0)
                                return typeResult;

                            var accountResult = a1.KeyIndex.Account.CompareTo(a2.KeyIndex.Account);

                            if (accountResult != 0)
                                return accountResult;

                            var chainResult = a1.KeyIndex.Chain.CompareTo(a2.KeyIndex.Chain);

                            return chainResult != 0
                                ? chainResult
                                : a1.KeyIndex.Index.CompareTo(a2.KeyIndex.Index);
                        });
                        break;
                    case AddressesSortField.ByPath when CurrentSortDirection == SortDirection.Asc:
                        addresses.Sort((a1, a2) =>
                        {
                            var typeResult = a1.KeyType.CompareTo(a2.KeyType);

                            if (typeResult != 0)
                                return typeResult;

                            var accountResult = a1.KeyIndex.Account.CompareTo(a2.KeyIndex.Account);

                            if (accountResult != 0)
                                return accountResult;

                            var chainResult = a1.KeyIndex.Chain.CompareTo(a2.KeyIndex.Chain);

                            return chainResult != 0
                                ? chainResult
                                : a1.KeyIndex.Index.CompareTo(a2.KeyIndex.Index);
                        });
                        break;
                    case AddressesSortField.ByBalance when CurrentSortDirection == SortDirection.Desc:
                        addresses = addresses.OrderByDescending(a => a.AvailableBalance()).ToList();
                        break;

                    case AddressesSortField.ByBalance when CurrentSortDirection == SortDirection.Asc:
                        addresses = addresses.OrderBy(a => a.AvailableBalance()).ToList();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Addresses = new ObservableCollection<AddressViewModel>(
                    addresses.Select(a =>
                    {
                        var path = a.KeyType == CurrencyConfig.StandardKey && Currencies.IsTezosBased(_currency.Name)
                            ? $"m/44'/{_currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}'"
                            : $"m/44'/{_currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}/{a.KeyIndex.Index}";

                        return new AddressViewModel
                        {
                            Address = a.Address,
                            Type = KeyTypeToString(a.KeyType),
                            Path = path,
                            HasTokens = HasTokens,
                            Balance = $"{a.Balance.ToString(CultureInfo.InvariantCulture)} {_currency.Name}",
                            CopyToClipboard = address => App.Clipboard.SetTextAsync(address),
                            OpenInExplorer = OpenInExplorer,
                            UpdateAddress = UpdateAddress,
                            ExportKey = ExportKey
                        };
                    }));

                // token balances
                if (HasTokens)
                {
                    var tezosAccount = account as TezosAccount;

                    var addressesWithTokens = (await tezosAccount
                            .DataRepository
                            .GetTezosTokenAddressesByContractAsync(_tokenContract))
                        .Where(w => w.Balance != 0)
                        .GroupBy(w => w.Address);

                    foreach (var addressWithTokens in addressesWithTokens)
                    {
                        var addressInfo = Addresses.FirstOrDefault(a => a.Address == addressWithTokens.Key);

                        if (addressInfo == null)
                            continue;

                        if (addressWithTokens.Count() == 1)
                        {
                            var tokenAddress = addressWithTokens.First();

                            addressInfo.TokenBalance = tokenAddress.Balance.FormatWithPrecision(DefaultTokenPrecision);

                            var tokenCode = tokenAddress?.TokenBalance?.Symbol;

                            if (tokenCode != null)
                                addressInfo.TokenBalance += $" {tokenCode}";
                        }
                        else
                        {
                            addressInfo.TokenBalance = $"{addressWithTokens.Count()} TOKENS";
                        }
                    }
                }

                SortByPathAndAsc =
                    CurrentSortField == AddressesSortField.ByPath && CurrentSortDirection == SortDirection.Asc;
                SortByPathAndDesc =
                    CurrentSortField == AddressesSortField.ByPath && CurrentSortDirection == SortDirection.Desc;
                SortByBalanceAndAsc =
                    CurrentSortField == AddressesSortField.ByBalance && CurrentSortDirection == SortDirection.Asc;
                SortByBalanceAndDesc =
                    CurrentSortField == AddressesSortField.ByBalance && CurrentSortDirection == SortDirection.Desc;

                SortByPath = CurrentSortField == AddressesSortField.ByPath;
                SortByBalance = CurrentSortField == AddressesSortField.ByBalance;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while load addresses.");
            }
        }

        private ReactiveCommand<AddressesSortField, Unit> _setSortTypeCommand;

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
        
        private void SubscribeToServices()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        private async void OnBalanceUpdatedEventHandler(object? sender, CurrencyEventArgs args)
        {
            try
            {
                if (_currency.Name != args.Currency) return;

                // reload addresses list
                await ReloadAddresses();
            }
            catch (Exception e)
            {
                Log.Error(e, "Reload addresses event handler error");
            }
        }

        private string KeyTypeToString(int keyType) =>
            keyType switch
            {
                CurrencyConfig.StandardKey => "Standard",
                TezosConfig.Bip32Ed25519Key => "Atomex",
                _ => throw new NotSupportedException($"Key type {keyType} not supported.")
            };

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
                var updateTask = new HdWalletScanner(_app.Account)
                    .ScanAddressAsync(_currency.Name, address);

                await Task.WhenAll(Task.Delay(MinimalUpdateTimeMs), updateTask);

                if (_currency.Name == TezosConfig.Xtz && _tokenContract != null)
                {
                    // update tezos token balance
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    await new TezosTokensScanner(tezosAccount)
                        .ScanContractAsync(address, _tokenContract);

                    // reload balances for all tezos tokens account
                    foreach (var currency in _app.Account.Currencies)
                        if (Currencies.IsTezosToken(currency.Name))
                            _app.Account
                                .GetCurrencyAccount<TezosTokenAccount>(currency.Name)
                                .ReloadBalances();
                }

                ReloadAddresses();
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

                            var hdWallet = _app.Account.Wallet as HdWallet;

                            using var privateKey = hdWallet.KeyStorage.GetPrivateKey(
                                currency: _currency,
                                keyIndex: walletAddress.KeyIndex,
                                keyType: walletAddress.KeyType);

                            using var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();

                            if (Currencies.IsBitcoinBased(_currency.Name))
                            {
                                var btcBasedConfig = _currency as BitcoinBasedConfig;

                                var wif = new NBitcoin.Key(unsecuredPrivateKey)
                                    .GetWif(btcBasedConfig.Network)
                                    .ToWif();

                                _ = App.Clipboard.SetTextAsync(wif);
                            }
                            else if (Currencies.IsTezosBased(_currency.Name))
                            {
                                var base58 = unsecuredPrivateKey.Length == 32
                                    ? Base58Check.Encode(unsecuredPrivateKey, Prefix.Edsk)
                                    : Base58Check.Encode(unsecuredPrivateKey, Prefix.EdskSecretKey);

                                _ = App.Clipboard.SetTextAsync(base58);
                            }
                            else
                            {
                                var hex = Hex.ToHexString(unsecuredPrivateKey.Data);

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

        private void DesignerMode()
        {
            _currency = DesignTime.TestNetCurrencies.First();

            Addresses = new ObservableCollection<AddressViewModel>(
                new List<AddressViewModel>
                {
                    new AddressViewModel
                    {
                        Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                        Path = "m/44'/0'/0'/0/0",
                        Balance = 4.0000000.ToString(CultureInfo.InvariantCulture),
                    },
                    new AddressViewModel
                    {
                        Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                        Path = "m/44'/0'/0'/0/0",
                        Balance = 100.ToString(CultureInfo.InvariantCulture),
                    },
                    new AddressViewModel
                    {
                        Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                        Path = "m/44'/0'/0'/0/0",
                        Balance = 16.0000001.ToString(CultureInfo.InvariantCulture),
                    }
                });
        }
    }
}