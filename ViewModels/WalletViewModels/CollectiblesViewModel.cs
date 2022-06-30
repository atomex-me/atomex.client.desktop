using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using ReactiveUI.Fody.Helpers;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class Collectible : ViewModelBase
    {
        public string Image { get; set; }
        public string Name { get; set; }
        public int Amount { get; set; }
    }

    public class CollectiblesViewModel : ViewModelBase
    {
        private readonly IAtomexApp _app;
        [Reactive] private ObservableCollection<TokenContract>? Contracts { get; set; }
        [Reactive] public ObservableCollection<Collectible> Collectibles { get; set; }

        public CollectiblesViewModel(IAtomexApp app)
        {
            _app = app;

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

        private async Task<IEnumerable<TezosTokenViewModel>> LoadCollectibles()
        {
            var tokens = new ObservableCollection<TezosTokenViewModel>();

            foreach (var contract in Contracts!)
                tokens.AddRange(await TezosTokenViewModelCreator.CreateOrGet(_app, contract, SetConversionTab));

            return tokens.OrderByDescending(token => token.CanExchange)
                .ThenByDescending(token => token.TotalAmountInBase);
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
            Collectibles = new ObservableCollection<Collectible>
            {
                new()
                {
                    Image =
                        "https://assets.objkt.media/file/assets-003/KT1KrTm6Aei9zp6UH7zYi5EUx23PAvcoK5B5/3/thumb288",
                    Name = "ONIMATA - SSR Card",
                    Amount = 1
                },
                new()
                {
                    Image =
                        "https://assets.objkt.media/file/assets-003/KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton/4589/thumb288",
                    Name = "ONIMATA - SSR Card",
                    Amount = 2
                },
                new()
                {
                    Image =
                        "https://assets.objkt.media/file/assets-003/KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton/72598/thumb288",
                    Name = "ONIMATA - SSR Card",
                    Amount = 3
                },
                new()
                {
                    Image =
                        "https://assets.objkt.media/file/assets-003/KT1KrTm6Aei9zp6UH7zYi5EUx23PAvcoK5B5/3/thumb288",
                    Name = "ONIMATA - SSR Card",
                    Amount = 2
                },
                new()
                {
                    Image =
                        "https://assets.objkt.media/file/assets-003/KT1KrTm6Aei9zp6UH7zYi5EUx23PAvcoK5B5/3/thumb288",
                    Name = "ONIMATA - SSR Card",
                    Amount = 1
                }
            };
        }
#endif
    }
}