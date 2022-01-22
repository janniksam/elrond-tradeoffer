using Elrond.TradeOffer.Web.Services;

namespace Elrond.TradeOffer.Web.Network;

public class DevnetNetworkStrategy : NetworkStrategy
{
    private readonly IFeatureStatesManager _featureStatesManager;
    private readonly string _smartContractAddress;

    public DevnetNetworkStrategy(IConfiguration configuration, IFeatureStatesManager featureStatesManager) :
        base("https://devnet-wallet.elrond.com", Erdcsharp.Configuration.Network.DevNet)
    {
        _featureStatesManager = featureStatesManager;
        _smartContractAddress = configuration.GetValue<string>("SmartContractAddressDev");
    }

    public override Task<bool> IsNetworkEnabledAsync(CancellationToken ct) => _featureStatesManager.GetDevNetEnabledAsync(ct);

    public override string GetSmartContractAddress()
    {
        return _smartContractAddress;
    }

    public override string GetTokenUrlFormat()
    {
        return "https://devnet-explorer.elrond.com/tokens/{0}";
    }

    public override string GetNftUrlFormat()
    {
        return "https://devnet-explorer.elrond.com/nfts/{0}";
    }

    public override string GetApiGateway()
    {
        return "https://devnet-api.elrond.com";
    }
}