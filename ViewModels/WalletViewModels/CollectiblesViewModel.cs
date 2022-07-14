using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class Collectible : ViewModelBase
    {
        public IEnumerable<TezosTokenViewModel> Tokens { get; set; }
        public string Name => Tokens.First().Contract?.Name ?? Tokens.First().Contract.Address;
        public string PreviewUrl => Tokens.First().NftPreview;

        public int Amount => Tokens
            .Aggregate(0, (result, tokenViewModel) => result + decimal.ToInt32(tokenViewModel.TotalAmount));

        public Action<IEnumerable<TezosTokenViewModel>> OnCollectionClick { get; set; }

        private ReactiveCommand<Unit, Unit>? _onCollectionClickCommand;

        public ReactiveCommand<Unit, Unit> OnCollectionClickCommand => _onCollectionClickCommand ??=
            ReactiveCommand.Create(() => { OnCollectionClick?.Invoke(Tokens); });
    }

    public class CollectiblesViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        [Reactive] private ObservableCollection<TokenContract>? Contracts { get; set; }
        [Reactive] public ObservableCollection<Collectible> Collectibles { get; set; }

        public CollectiblesViewModel(IAtomexApp app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _app.AtomexClientChanged += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;

            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .SubscribeInMainThread(collectibles => _ = LoadCollectibles());

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
                if (args.IsTokenUpdate && args.TokenContract == null)
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

            Collectibles = new ObservableCollection<Collectible>(nftTokens
                .GroupBy(nft => nft.Contract.Address)
                .Select(nftGroup => new Collectible
                {
                    Tokens = nftGroup.Select(g => g),
                    OnCollectionClick = ShowCollection
                })
                .OrderByDescending(collectible => collectible.Amount));
        }

        private void ShowCollection(IEnumerable<TezosTokenViewModel> tokens)
        {
            var a = 5;
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
            // Collectibles = new ObservableCollection<Collectible>
            // {
            //     new()
            //     {
            //         Amount = 3,
            //         Name = "ONIMATA - SSR Card",
            //         Contract = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
            //         TokenId = 129753,
            //     },
            //     new()
            //     {
            //         Amount = 3,
            //         Name = "ONIMATA - SSR Card",
            //         Contract = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
            //         TokenId = 129753,
            //     },
            //     new()
            //     {
            //         Amount = 3,
            //         Name = "ONIMATA - SSR Card",
            //         Contract = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
            //         TokenId = 129753,
            //     },
            //     new()
            //     {
            //         Amount = 3,
            //         Name = "ONIMATA - SSR Card",
            //         Contract = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
            //         TokenId = 129753,
            //     },
            // };
        }
#endif
    }
}