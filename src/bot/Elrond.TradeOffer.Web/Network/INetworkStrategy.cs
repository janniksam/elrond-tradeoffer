namespace Elrond.TradeOffer.Web.Network;

public interface INetworkStrategy
{
    Erdcsharp.Configuration.Network Network { get; }

    bool IsNetworkAvailable();

    string GetSmartContractAddress();

    string GetTransactionUrl(TransactionRequest request, string? callbackUrl = null);
    
    string GetTokenUrlFormat();

    string GetApiGateway();
}