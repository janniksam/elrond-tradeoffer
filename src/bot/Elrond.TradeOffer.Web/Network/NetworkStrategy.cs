using System.Text;
using System.Text.Encodings.Web;
using Elrond.TradeOffer.Web.Models;

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

        private static string BuildTransactionUrl(TransactionRequest request, string? callbackUrl)
        {
            StringBuilder builder = new($"receiver={request.Receiver}&value={request.Value.Value}");
            builder.Append($"&gasLimit={request.GasLimit}");
            builder.Append($"&gasPrice={request.GasPrice}");
            builder.Append($"&data={request.Data}");

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

        public abstract string GetSmartContractAddress();
    }

    public class TransactionRequest
    {
        public TransactionRequest(string receiver, TokenAmount value, int gasLimit, int gasPrice, string data, long? nonce = null)
        {
            Receiver = receiver;
            Value = value;
            GasLimit = gasLimit;
            GasPrice = gasPrice;
            Data = data;
            Nonce = nonce;
        }

        public string Receiver { get; set; }
        public TokenAmount Value { get; set; }
        public int GasLimit { get; set; }
        public int GasPrice { get; set; }
        public string Data { get; set; }
        public long? Nonce { get; set; }
    }
}
