using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Services;

namespace Elrond.TradeOffer.Web.Network;

public class NetworkStrategies : INetworkStrategies
{
    private readonly IConfiguration _configuration;
    private readonly IFeatureStatesManager _featureStatesManager;

    public NetworkStrategies(
        IConfiguration configuration,
        IFeatureStatesManager featureStatesManager)
    {
        _configuration = configuration;
        _featureStatesManager = featureStatesManager;
    }

    public INetworkStrategy GetStrategy(ElrondNetwork network)
    {
        return network switch
        { 
            ElrondNetwork.Devnet => new DevnetNetworkStrategy(_configuration, _featureStatesManager),
            ElrondNetwork.Testnet => new TestnetNetworkStrategy(_configuration, _featureStatesManager),
            ElrondNetwork.Mainnet => new MainnetNetworkStrategy(_configuration, _featureStatesManager),
            _ => new TestnetNetworkStrategy(_configuration, _featureStatesManager)
        };
    }
}