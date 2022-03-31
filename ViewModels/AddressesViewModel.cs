using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using System.Windows.Input;

using Serilog;
using ReactiveUI;

using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Desktop.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.Cryptography;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.ViewModels
{
    public class AddressInfo : ViewModelBase
    {
        public string Address { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }

        private string _balance;

        public string Balance
        {
            get => _balance;
            set
            {
                _balance = value;
                OnPropertyChanged(nameof(Balance));
            }
        }
        public string TokenBalance { get; set; }

        public Action<string> CopyToClipboard { get; set; }
        public Action<string> OpenInExplorer { get; set; }
        public Action<string> ExportKey { get; set; }
        public Action<string> UpdateAddress { get; set; }

        private bool _isUpdating;

        public bool IsUpdating
        {
            get => _isUpdating;
            set
            {
                _isUpdating = value;
                OnPropertyChanged(nameof(IsUpdating));
            }
        }

        private ICommand _setAddressUpdating;

        public ICommand SetAddressUpdating => _setAddressUpdating ??= ReactiveCommand.Create<string>((address) =>
        {
            if (IsUpdating) return;
            IsUpdating = true;
            UpdateAddress?.Invoke(address);
        });

        private ICommand _setAddressUpdated;

        public ICommand SetAddressUpdated => _setAddressUpdated ??= ReactiveCommand.Create<string>((balance) =>
        {
            Balance = balance;
            IsUpdating = false;
        });

        private ICommand _copyCommand;

        public ICommand CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            CopyToClipboard?.Invoke(s);
        });

        private ICommand _openInExplorerCommand;

        public ICommand OpenInExplorerCommand => _openInExplorerCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            OpenInExplorer?.Invoke(Address);
        });

        private ICommand _exportKeyCommand;

        public ICommand ExportKeyCommand => _exportKeyCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            ExportKey?.Invoke(Address);
        });
    }

    public class AddressesViewModel : ViewModelBase
    {
        private const int DefaultTokenPrecision = 9;
        private readonly IAtomexApp _app;
        private CurrencyConfig _currency;
        private readonly string _tokenContract;

        public ObservableCollection<AddressInfo> Addresses { get; set; }
        
        public bool HasTokens { get; set; }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set
            {
                _warning = value;
                OnPropertyChanged(nameof(Warning));
                OnPropertyChanged(nameof(HasWarning));
            }
        }

        public bool HasWarning => !string.IsNullOrEmpty(Warning);

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
            _app           = app ?? throw new ArgumentNullException(nameof(app));
            _currency      = currency ?? throw new ArgumentNullException(nameof(currency));
            _tokenContract = tokenContract;

            ReloadAddresses();
        }

        public async void ReloadAddresses()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount(_currency.Name);

                var addresses = (await account
                    .GetAddressesAsync())
                    .ToList();

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

                Addresses = new ObservableCollection<AddressInfo>(
                    addresses.Select(a =>
                    {
                        var path = a.KeyType == CurrencyConfig.StandardKey && Currencies.IsTezosBased(_currency.Name)
                            ? $"m/44'/{_currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}'"
                            : $"m/44'/{_currency.Bip44Code}'/{a.KeyIndex.Account}'/{a.KeyIndex.Chain}/{a.KeyIndex.Index}";

                        return new AddressInfo
                        {
                            Address         = a.Address,
                            Type            = KeyTypeToString(a.KeyType),
                            Path            = path,
                            Balance         = $"{a.Balance.ToString(CultureInfo.InvariantCulture)} {_currency.Name}",
                            CopyToClipboard = CopyToClipboard,
                            OpenInExplorer  = OpenInExplorer,
                            UpdateAddress   = UpdateAddress,
                            ExportKey       = ExportKey
                        };
                    }));

                // token balances
                if (_currency.Name == TezosConfig.Xtz && _tokenContract != null)
                {
                    HasTokens = true;

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

                OnPropertyChanged(nameof(Addresses));
                OnPropertyChanged(nameof(HasTokens));
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while load addresses.");
            }
        }
        
        private string KeyTypeToString(int keyType) =>
            keyType switch
            {
                CurrencyConfig.StandardKey  => "Standard",
                TezosConfig.Bip32Ed25519Key => "Atomex",
                _ => throw new NotSupportedException($"Key type {keyType} not supported.")
            };

        private void CopyToClipboard(string address)
        {
            try
            {
                App.Clipboard.SetTextAsync(address);

                Warning = "Address successfully copied to clipboard.";
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
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

        private async void UpdateAddress(string address)
        {
            try
            {
                await new HdWalletScanner(_app.Account)
                    .ScanAddressAsync(_currency.Name, address);

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

                // var targetAddr = Addresses.FirstOrDefault(a => a.Address == address);
                // targetAddr!.SetAddressUpdated.Execute(balance.Available.ToString(CultureInfo.InvariantCulture));
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
                        backAction: () => App.DialogService.Show(this),
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
                                nextAction: () => App.DialogService.Show(this)
                            ));

                            Warning = "Private key successfully copied to clipboard.";
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
            _currency = DesignTime.Currencies.First();

            Addresses = new ObservableCollection<AddressInfo>(
                new List<AddressInfo>
                {
                    new AddressInfo
                    {
                        Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                        Path = "m/44'/0'/0'/0/0",
                        Balance = 4.0000000.ToString(CultureInfo.InvariantCulture),
                    },
                    new AddressInfo
                    {
                        Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                        Path = "m/44'/0'/0'/0/0",
                        Balance = 100.ToString(CultureInfo.InvariantCulture),
                    },
                    new AddressInfo
                    {
                        Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                        Path = "m/44'/0'/0'/0/0",
                        Balance = 16.0000001.ToString(CultureInfo.InvariantCulture),
                    }
                });
        }
    }
}