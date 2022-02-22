using System.Text;
using System.Text.Encodings.Web;

namespace Elrond.TradeOffer.Web.Network
{
    public abstract class NetworkStrategy : INetworkStrategy
    {
        private readonly string _walletUrl;

        protected NetworkStrategy(string walletUrl, Erdcsharp.Configuration.Network network)
        {
            _walletUrl = walletUrl;
            Network = network;
        }

        public string GetTransactionUrl(TransactionRequest request, string? callbackUrl = null)
        {
            return $"{_walletUrl}/hook/transaction/?{BuildTransactionUrl(request, callbackUrl)}";
        }

        public abstract string GetTokenUrlFormat();

        public abstract string GetApiGateway();

        public abstract string GetNftUrlFormat();

        private static string BuildTransactionUrl(TransactionRequest request, string? callbackUrl)
        {
            StringBuilder builder = new();
            builder.Append($"value={request.Value.Value}");
            builder.Append($"&gasLimit={request.GasLimit}");
            builder.Append($"&gasPrice={request.GasPrice}");
            builder.Append($"&data={request.Data}");

            if (request.Receiver != null)
            {
                builder.Append($"&receiver={request.Receiver}");
            }

            if (request.Nonce != null)
            {
                builder.Append($"&nonce={request.Nonce}");
            }

            if (callbackUrl != null)
            {
                var callbackUrlEncoded = UrlEncoder.Default.Encode(callbackUrl);
                builder.Append($"&callbackUrl={callbackUrlEncoded}");
            }

            return builder.ToString();
        }

        public Erdcsharp.Configuration.Network Network { get; }

        public async Task<bool> IsNetworkReadyAsync(CancellationToken ct)
        {
            return IsNetworkAvailable() && await IsNetworkEnabledAsync(ct);
        }

        public bool IsNetworkAvailable() => !string.IsNullOrWhiteSpace(GetSmartContractAddress());
        
        public abstract Task<bool> IsNetworkEnabledAsync(CancellationToken ct);

        public abstract string GetSmartContractAddress();
    }
}
