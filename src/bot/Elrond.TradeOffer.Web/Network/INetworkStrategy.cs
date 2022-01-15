namespace Elrond.TradeOffer.Web.Network;

public interface INetworkStrategy
{
    Erdcsharp.Configuration.Network Network { get; }

    bool IsNetworkAvailable();

    Task<bool> IsNetworkEnabledAsync(CancellationToken ct);
    
    Task<bool> IsNetworkReadyAsync(CancellationToken ct);

    string GetSmartContractAddress();

    string GetTransactionUrl(TransactionRequest request, string? callbackUrl = null);
    
    string GetTokenUrlFormat();

    string GetApiGateway();
    
    string GetNftUrlFormat();
}