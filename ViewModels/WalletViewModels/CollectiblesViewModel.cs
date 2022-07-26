using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.ViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

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
        public IBitmap? BitmapIcon => null;
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
        [Reactive] public ObservableCollection<Collectible> Collectibles { get; set; }
        public ObservableCollection<Collectible> InitialCollectibles { get; set; }
        private Action<IEnumerable<TezosTokenViewModel>> ShowTezosCollection { get; }

        public CollectiblesViewModel(IAtomexApp app, Action<IEnumerable<TezosTokenViewModel>> showTezosCollection)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            ShowTezosCollection = showTezosCollection ?? throw new ArgumentNullException(nameof(showTezosCollection));

            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;

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

            DisabledCollectibles = _app.Account.UserData.DisabledCollectibles ?? Array.Empty<string>();
            _ = ReloadTokenContractsAsync();
        }

        private void OnAtomexClientChanged(object sender, AtomexClientChangedEventArgs args)
        {
            if (_app.Account != null) return;

            _app.AtomexClientChanged -= OnAtomexClientChanged;
            Contracts?.Clear();
        }

        private async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (args.IsTokenUpdate &&
                    (args.TokenContract == null || (Contracts != null && Contracts.Select(c => c.Address)
                        .Contains(args.TokenContract))))
                {
                    await Dispatcher.UIThread.InvokeAsync(async () => { await ReloadTokenContractsAsync(); },
                        DispatcherPriority.Background);
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
                    .DataRepository
                    .GetTezosTokenContractsAsync())
                .ToList();

            Contracts = new ObservableCollection<TokenContract>(contracts);
        }

        private async Task LoadCollectibles()
        {
            var nftTokens = new List<TezosTokenViewModel>();

            foreach (var contract in Contracts!)
                nftTokens.AddRange(await TezosTokenViewModelCreator.CreateOrGet(_app, contract, isNft: true));

            var collectibles = nftTokens
                .GroupBy(nft => nft.Contract.Address)
                .Select(nftGroup => new Collectible
                {
                    Tokens = nftGroup.Select(g => g),
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