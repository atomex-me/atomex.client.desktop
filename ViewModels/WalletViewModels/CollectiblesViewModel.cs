using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    // public class Collectible : ViewModelBase
    // {
    //     public string Image { get; set; }
    //     public string Name { get; set; }
    //     public int Amount { get; set; }
    // }

    public class CollectiblesViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        [Reactive] private ObservableCollection<TokenContract>? Contracts { get; set; }
        [Reactive] public ObservableCollection<TezosTokenViewModel> Collectibles { get; set; }
        private Action<CurrencyConfig> SetConversionTab { get; }

        public CollectiblesViewModel(IAtomexApp app)
        {
            _app = app;

            this.WhenAnyValue(vm => vm.Contracts)
                .WhereNotNull()
                .SubscribeInMainThread(collectibles => _ = LoadCollectibles());

            _ = ReloadTokenContractsAsync();
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
            var collectibles = new ObservableCollection<TezosTokenViewModel>();

            foreach (var contract in Contracts!)
                collectibles.AddRange(await TezosTokenViewModelCreator.CreateOrGet(_app, contract, true));

            Collectibles = new ObservableCollection<TezosTokenViewModel>(collectibles
                .OrderByDescending(token => token.TotalAmountInBase));
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
            Collectibles = new ObservableCollection<TezosTokenViewModel>
            {
                new()
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton"
                    },
                    TokenBalance = new TokenBalance()
                    {
                        TokenId = 129753,
                        Balance = "1",
                        Symbol = "OBJKT",
                        Name = "ONIMATA - SSR Card",

                    }
                },
                new()
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton"
                    },
                    TokenBalance = new TokenBalance()
                    {
                        TokenId = 129753,
                        Balance = "1",
                        Symbol = "OBJKT",
                        Name = "ONIMATA - SSR Card",

                    }
                },
                new()
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton"
                    },
                    TokenBalance = new TokenBalance()
                    {
                        TokenId = 129753,
                        Balance = "1",
                        Symbol = "OBJKT",
                        Name = "ONIMATA - SSR Card",

                    }
                },
                new()
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton"
                    },
                    TokenBalance = new TokenBalance()
                    {
                        TokenId = 129753,
                        Balance = "1",
                        Symbol = "OBJKT",
                        Name = "ONIMATA - SSR Card",

                    }
                },
                new()
                {
                    Contract = new TokenContract
                    {
                        Address = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton"
                    },
                    TokenBalance = new TokenBalance()
                    {
                        TokenId = 129753,
                        Balance = "1",
                        Symbol = "OBJKT",
                        Name = "ONIMATA - SSR Card",

                    }
                },
            };
        }
#endif
    }
}