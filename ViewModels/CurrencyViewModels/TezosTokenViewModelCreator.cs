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
        private static readonly ConcurrentDictionary<KeyValuePair<string, decimal>, TezosTokenViewModel> Instances =
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
                .GroupBy(walletAddress => walletAddress.TokenBalance.TokenId);

            var tokensViewModels = tokenGroups
                .Select(walletAddressGroup =>
                    walletAddressGroup.Skip(1).Aggregate(walletAddressGroup.First(), (result, walletAddress) =>
                    {
                        result.TokenBalance.ParsedBalance = result.TokenBalance.GetTokenBalance() +
                                                            walletAddress.TokenBalance.GetTokenBalance();

                        return result;
                    }))
                .Select(walletAddress => new TezosTokenViewModel
                {
                    AtomexApp = atomexApp,
                    SetConversionTab = setConversionTab,
                    TezosConfig = tezosAccount.Config,
                    TokenBalance = walletAddress.TokenBalance,
                    TotalAmount = walletAddress.TokenBalance.GetTokenBalance(),
                    Address = walletAddress.Address,
                    Contract = contract,
                });

            var tokens = new List<TezosTokenViewModel>();

            foreach (var tokenViewModel in tokensViewModels)
            {
                var kv = new KeyValuePair<string, decimal>(
                    tokenViewModel.Contract.Address,
                    tokenViewModel.TokenBalance.TokenId);

                if (Instances.TryGetValue(kv, out var cachedTokenViewModel))
                {
                    tokens.Add(cachedTokenViewModel);
                    continue;
                }

                tokenViewModel.SubscribeToUpdates();
                // await tokenViewModel.UpdateAsync();
                tokenViewModel.UpdateQuotesInBaseCurrency(atomexApp.QuotesProvider);
                Instances.TryAdd(kv, tokenViewModel);

                tokens.Add(tokenViewModel);
            }

            return tokens.Where(token => !token.TokenBalance.IsNft);
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