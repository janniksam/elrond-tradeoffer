using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.Network;

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