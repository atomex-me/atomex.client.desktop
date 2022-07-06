using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Atomex.Blockchain.Tezos;
using Atomex.Core;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public static class TezosTokenViewModelCreator
    {
        private static readonly ConcurrentDictionary<(string, decimal), TezosTokenViewModel> Instances =
            new();

        public static async Task<IEnumerable<TezosTokenViewModel>> CreateOrGet(
            IAtomexApp atomexApp,
            TokenContract contract,
            Action<CurrencyConfig> setConversionTab)
        {
            var tezosAccount = atomexApp.Account.GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

            var tokenWalletAddresses = await tezosAccount
                .DataRepository
                .GetTezosTokenAddressesByContractAsync(contract.Address);

            var tokenGroups = tokenWalletAddresses
                .Where(walletAddress => !walletAddress.TokenBalance.IsNft) // skip NFT
                .GroupBy(walletAddress => walletAddress.TokenBalance.TokenId);

            var resultTokens = new List<TezosTokenViewModel>();

            foreach (var tokenGroup in tokenGroups)
            {
                if (Instances.TryGetValue((contract.Address, tokenGroup.Key), out var cachedTokenViewModel))
                {
                    resultTokens.Add(cachedTokenViewModel);
                    continue;
                }

                var tokenBalance = tokenGroup
                    .Select(w => w.TokenBalance)
                    .Aggregate(new TokenBalance { ParsedBalance = 0 }, (result, tb) =>
                    {
                        result.ParsedBalance  += tb.GetTokenBalance();
                        result.Balance         = result.ParsedBalance.ToString();
                        result.ArtifactUri   ??= tb.ArtifactUri;
                        result.Contract      ??= tb.Contract;
                        result.ContractAlias ??= tb.ContractAlias;
                        result.Creators      ??= tb.Creators;
                        result.Decimals        = tb.Decimals;
                        result.Description   ??= tb.Description;
                        result.DisplayUri    ??= tb.DisplayUri;
                        result.Name          ??= tb.Name;
                        result.Standard      ??= tb.Standard;
                        result.Symbol        ??= tb.Symbol;
                        result.ThumbnailUri  ??= tb.ThumbnailUri;
                        result.TokenId         = tb.TokenId;
                        return result;
                    });

                var tokenViewModel = new TezosTokenViewModel
                {
                    AtomexApp        = atomexApp,
                    SetConversionTab = setConversionTab,
                    TezosConfig      = tezosAccount.Config,
                    TokenBalance     = tokenBalance,
                    TotalAmount      = tokenBalance.GetTokenBalance(),
                    Contract         = contract,
                };

                tokenViewModel.UpdateQuotesInBaseCurrency(atomexApp.QuotesProvider);
                tokenViewModel.SubscribeToUpdates();

                Instances.TryAdd((contract.Address, tokenGroup.Key), tokenViewModel);

                resultTokens.Add(tokenViewModel);
            }

            return resultTokens;
        }

        public static void Reset()
        {
            foreach (var tokenViewModel in Instances.Values)
            {
                tokenViewModel.Dispose();
            }

            Instances.Clear();
        }
    }
}