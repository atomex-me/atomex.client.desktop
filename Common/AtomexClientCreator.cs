using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Atomex.Client.Abstract;
using Atomex.Client.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.Services;

namespace Atomex.Client.Desktop.Common
{
    public static class AtomexClientCreator
    {
        public static IAtomexClient Create(
            IConfiguration configuration,
            Network network,
            ClientType platformType,
            AuthMessageSigner? authMessageSigner = null,
            ILogger? logger = null)
        {
            var clientType = configuration[$"AtomexClients:{network}:DefaultClientType"];

            return clientType switch
            {
                nameof(WebSocketAtomexClient) => new WebSocketAtomexClient(
                    authTokenBaseUrl: configuration[$"AtomexClients:{network}:WebSocketAtomexClient:AuthTokenBaseUrl"],
                    exchangeUrl: configuration[$"AtomexClients:{network}:WebSocketAtomexClient:Exchange:Url"],
                    marketDataUrl: configuration[$"AtomexClients:{network}:WebSocketAtomexClient:MarketData:Url"],
                    authMessageSigner: authMessageSigner,
                    log: logger
                ),

                nameof(WebSocketAtomexClientLegacy) => new WebSocketAtomexClientLegacy(
                    exchangeUrl: configuration[$"AtomexClients:{network}:WebSocketAtomexClientLegacy:Exchange:Url"],
                    marketDataUrl: configuration[$"AtomexClients:{network}:WebSocketAtomexClientLegacy:MarketData:Url"],
                    clientType: platformType,
                    authMessageSigner: authMessageSigner,
                    log: logger
                ),

                _ => throw new ArgumentNullException(nameof(clientType)),
            };
        }
    }
}