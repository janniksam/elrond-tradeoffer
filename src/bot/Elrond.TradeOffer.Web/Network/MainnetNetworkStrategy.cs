using Elrond.TradeOffer.Web.Services;

namespace Elrond.TradeOffer.Web.Network;

public class MainnetNetworkStrategy : NetworkStrategy
{
    private readonly IFeatureStatesManager _featureStatesManager;
    private readonly string _smartContractAddress;

    public MainnetNetworkStrategy(IConfiguration configuration, IFeatureStatesManager featureStatesManager) :
        base("https://wallet.elrond.com", Erdcsharp.Configuration.Network.MainNet)
    {
        _featureStatesManager = featureStatesManager;
        _smartContractAddress = configuration.GetValue<string>("SmartContractAddressMain");
    }
    
    public override Task<bool> IsNetworkEnabledAsync(CancellationToken ct) => _featureStatesManager.GetMainNetEnabledAsync(ct);

    public override string GetSmartContractAddress()
    {
        return _smartContractAddress;
    }

    public override string GetTokenUrlFormat()
    {
        return "https://explorer.elrond.com/tokens/{0}";
    }

    public override string GetApiGateway()
    {
        return "https://api.elrond.com";
    }
}