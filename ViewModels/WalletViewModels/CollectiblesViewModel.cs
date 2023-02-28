using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain.Tezos.Tzkt;
using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class Collectible : IAssetViewModel
    {
        public IEnumerable<TezosTokenViewModel> Tokens { get; set; }
        public string ContractAddress => Tokens.First().Contract.Address;
        public string Name => Tokens.First().Contract.Name ?? ContractAddress.TruncateAddress();
        public string PreviewUrl => Tokens.First().PreviewUrl;

        public decimal TotalAmount =>
            Tokens.Aggregate(0m, (result, tokenViewModel) => result + tokenViewModel.TotalAmount);

        public string? IconPath => null;
        public string? DisabledIconPath => null;
        public string CurrencyName => string.Empty;
        public string CurrencyCode => ContractAddress;
        public string CurrencyDescription => Name;
        public string CurrencyFormat => "F0";
        public string BaseCurrencyFormat => string.Empty;
        public decimal TotalAmountInBase => 0;

        private ReactiveCommand<Unit, Unit>? _onCollectionClickCommand;
        public ReactiveCommand<Unit, Unit> OnCollectionClickCommand => _onCollectionClickCommand ??=
            ReactiveCommand.Create(() => { OnCollectibleClick?.Invoke(Tokens); });

        public Action<IEnumerable<TezosTokenViewModel>> OnCollectibleClick { get; set; }
    }

    public class CollectiblesViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        [Reactive] private ObservableCollection<TokenContract>? Contracts { get; set; }
        [Reactive] public string[] DisabledCollectibles { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public ObservableCollection<Collectible> Collectibles { get; set; }
        public ObservableCollection<Collectible> InitialCollectibles { get; set; }
        private Action<IEnumerable<TezosTokenViewModel>> ShowTezosCollection { get; }

        public CollectiblesViewModel(IAtomexApp app, Action<IEnumerable<TezosTokenViewModel>> showTezosCollection)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            ShowTezosCollection = showTezosCollection ?? throw new ArgumentNullException(nameof(showTezosCollection));

            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.LocalStorage.BalanceChanged += OnBalanceChangedEventHandler;

            this.WhenAnyValue(vm => vm.DisabledCollectibles)
                .WhereNotNull()
                .Skip(1)
                .SubscribeInMainThread(disabledCollectibles =>
                {
                    _app.Account.UserData.DisabledCollectibles = disabledCollectibles;
                    _app.Account.UserData.SaveToFile(_app.Account.SettingsFilePath);
                    _ = LoadCollectibles();
                });

            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .SubscribeInMainThread(collectibles => _ = LoadCollectibles());

            this.WhenAnyValue(vm => vm.SearchPattern)
                .Where(_ => InitialCollectibles != null && DisabledCollectibles != null)
                .SubscribeInMainThread(searchPattern =>
                    Collectibles = new ObservableCollection<Collectible>(InitialCollectibles.Where(c =>
                        {
                            var firstContract = c.Tokens.First().Contract;
                            if (firstContract.Name != null)
                            {
                                return firstContract.Name.ToLower().Contains(searchPattern.ToLower()) ||
                                       firstContract.Address.ToLower().Contains(searchPattern.ToLower());
                            }

                            return firstContract.Address.ToLower().Contains(searchPattern.ToLower());
                        })
                        .Where(c => !DisabledCollectibles!.Contains(c.ContractAddress))
                        .OrderByDescending(c => c.TotalAmount != 0)
                        .ThenBy(c => c.Name)));

            DisabledCollectibles = _app.Account.UserData.DisabledCollectibles ?? Array.Empty<string>();

            _ = ReloadTokenContractsAsync();
        }

        private void OnAtomexClientChanged(object? sender, AtomexClientChangedEventArgs args)
        {
            if (_app.Account != null)
                return;

            _app.AtomexClientChanged -= OnAtomexClientChanged;

            Contracts?.Clear();
        }

        private async void OnBalanceChangedEventHandler(object? sender, BalanceChangedEventArgs args)
        {
            try
            {
                if (args is TokenBalanceChangedEventArgs eventArgs)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () => { await ReloadTokenContractsAsync(); },
                        DispatcherPriority.Background);
                    
                    Log.Debug("Tezos collectibles balance updated for contracts {@Contracts}", string.Join(',', eventArgs.Tokens.Select(t => t.Item1)));
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos collectibles balance updated event handler error");
            }
        }

        private async Task ReloadTokenContractsAsync()
        {
            var contracts = (await _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
                .LocalStorage
                .GetTokenContractsAsync())
                .ToList();

            Contracts = new ObservableCollection<TokenContract>(contracts);
        }

        private async Task LoadCollectibles()
        {
            var nfts = new List<TezosTokenViewModel>();

            foreach (var contract in Contracts!)
                nfts.AddRange(await TezosTokenViewModelCreator.CreateOrGet(_app, contract, isNft: true));

            var collectibles = nfts
                .GroupBy(collectible => collectible.Contract.Address)
                .Select(collectibleGroup => new Collectible
                {
                    Tokens = new ObservableCollection<TezosTokenViewModel>(collectibleGroup
                        .Select(g => g)
                        .OrderByDescending(token => token.TotalAmount != 0)
                        .ThenBy(token => token.TokenBalance.Name)
                    ),
                    OnCollectibleClick = tokens => ShowTezosCollection.Invoke(tokens
                        .OrderByDescending(token => token.TotalAmount != 0)
                        .ThenBy(token => token.TokenBalance.Name))
                });

            InitialCollectibles = new ObservableCollection<Collectible>(collectibles);

            Collectibles = new ObservableCollection<Collectible>(collectibles
                .Where(collectible => !DisabledCollectibles.Contains(collectible.Tokens.First().Contract.Address))
                .OrderByDescending(collectible => collectible.TotalAmount != 0)
                .ThenBy(collectible => collectible.Name));
        }

        public CollectiblesViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }
#if DEBUG
        private void DesignerMode()
        {
        }
#endif
    }
}