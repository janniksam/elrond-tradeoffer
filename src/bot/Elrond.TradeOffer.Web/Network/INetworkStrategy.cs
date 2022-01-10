namespace Elrond.TradeOffer.Web.Network;

public interface INetworkStrategy
{
    Erdcsharp.Configuration.Network Network { get; }

    string GetSmartContractAddress();

    string GetTransactionUrl(TransactionRequest request, string? callbackUrl = null);
}