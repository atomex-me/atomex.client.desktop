using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Common;
using Newtonsoft.Json;

namespace Atomex.Client.Desktop.Api
{
    public class WertApi
    {
        public class Body
        {
            [JsonProperty(PropertyName = "ticker")]
            public decimal Ticker { get; set; }

            [JsonProperty(PropertyName = "fee_percent")]
            public decimal FeePercent { get; set; }

            [JsonProperty(PropertyName = "currency_amount")]
            public decimal CurrencyAmount { get; set; }

            [JsonProperty(PropertyName = "fee_amount")]
            public decimal FeeAmount { get; set; }

            [JsonProperty(PropertyName = "commodity_amount")]
            public decimal CommodityAmount { get; set; }

            [JsonProperty(PropertyName = "purchase_amount")]
            public decimal PurchaseAmount { get; set; }

            [JsonProperty(PropertyName = "miner_fee")]
            public decimal MinerFee { get; set; }
        }

        public class WertConvertResponse
        {
            [JsonProperty(PropertyName = "status")]
            public string Status { get; set; }

            [JsonProperty(PropertyName = "body")] public Body Body { get; set; }
        }

        internal class WertRequestData
        {
            [JsonProperty(PropertyName = "from")] public string From { get; set; }

            [JsonProperty(PropertyName = "to")] public string To { get; set; }

            [JsonProperty(PropertyName = "amount")]
            public decimal Amount { get; set; }
        }

        public string BaseUrl => App.Account.Network == Core.Network.MainNet
            ? "https://widget.wert.io/"
            : "https://sandbox.wert.io/";

        private string ConvertUrl => "api/v3/convert";

        private string _partnerIdHeader => App.Account.Network == Core.Network.MainNet
            ? "atomex"
            : "01F298K3HP4DY326AH1NS3MM3M";
        
        protected IAtomexApp App { get; }

        private const int MinDelayBetweenRequestMs = 1000;

        private static readonly RequestLimitControl RequestLimitControl = new(MinDelayBetweenRequestMs);


        public WertApi(IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
        }

        public async Task<Result<WertConvertResponse>> GetConvertRates(
            string from,
            string to,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            await RequestLimitControl
                .Wait(cancellationToken)
                .ConfigureAwait(false);

            var requestContent = JsonConvert.SerializeObject(new WertRequestData
            {
                From = from,
                To = to,
                Amount = amount
            });
            
            var headers = new HttpRequestHeaders
            {
                new KeyValuePair<string, IEnumerable<string>>("x-partner-id", new[] {_partnerIdHeader})
            };

            return await HttpHelper.PostAsyncResult<WertConvertResponse>(
                    baseUri: BaseUrl,
                    requestUri: ConvertUrl,
                    headers: headers,
                    content: new StringContent(requestContent, Encoding.UTF8, "application/json"),
                    responseHandler: (response, content) => JsonConvert.DeserializeObject<WertConvertResponse>(content),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}