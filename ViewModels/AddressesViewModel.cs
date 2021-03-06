﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using System.Windows.Input;
using Serilog;
using Atomex.Core;
using Atomex.Client.Desktop.Common;
using System.Diagnostics;
using Atomex.Client.Desktop.Controls;
using Atomex.Wallet;
using Atomex.Common;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using ReactiveUI;

namespace Atomex.Client.Desktop.ViewModels
{
    public class AddressInfo : ViewModelBase
    {
        public string Address { get; set; }
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
            App.Clipboard.SetTextAsync(Address);
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
        private readonly IAtomexApp _app;
        private Currency _currency;

        public ObservableCollection<AddressInfo> Addresses { get; set; }

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
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public AddressesViewModel(IAtomexApp app, Currency currency)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));

            Load();
        }

        public async void Load()
        {
            try
            {
                var account = _app.Account.GetCurrencyAccount(_currency.Name);

                var addresses = (await account.GetAddressesAsync())
                    .ToList();

                addresses.Sort((a1, a2) =>
                {
                    var chainResult = a1.KeyIndex.Chain.CompareTo(a2.KeyIndex.Chain);

                    return chainResult == 0
                        ? a1.KeyIndex.Index.CompareTo(a2.KeyIndex.Index)
                        : chainResult;
                });

                Addresses = new ObservableCollection<AddressInfo>(
                    addresses.Select(a => new AddressInfo
                    {
                        Address = a.Address,
                        Path = $"m/44'/{_currency.Bip44Code}/0'/{a.KeyIndex.Chain}/{a.KeyIndex.Index}",
                        Balance = a.Balance.ToString(CultureInfo.InvariantCulture),
                        CopyToClipboard = CopyToClipboard,
                        OpenInExplorer = OpenInExplorer,
                        ExportKey = ExportKey,
                        UpdateAddress = UpdateAddress
                    }));

                OnPropertyChanged(nameof(Addresses));
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while load addresses.");
            }
        }

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
                    Process.Start(uri.ToString());
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
                    .ScanAddressAsync(_currency.Name, address)
                    .ConfigureAwait(false);

                var balance = await _app.Account
                    .GetAddressBalanceAsync(_currency.Name, address)
                    .ConfigureAwait(false);

                var targetAddr = Addresses.FirstOrDefault(a => a.Address == address);
                targetAddr!.SetAddressUpdated.Execute(balance.Available.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception e)
            {
                Log.Error(e, "Update address error");
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

                            using var privateKey = hdWallet.KeyStorage
                                .GetPrivateKey(_currency, walletAddress.KeyIndex);

                            using var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();

                            var hex = Hex.ToHexString(unsecuredPrivateKey.Data);

                            await App.Clipboard.SetTextAsync(hex);

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