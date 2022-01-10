using Elrond.TradeOffer.Web.Database;

namespace Elrond.TradeOffer.Web.Network;

public class NetworkStrategies : INetworkStrategies
{
    private readonly IConfiguration _configuration;

    public NetworkStrategies(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public INetworkStrategy GetStrategy(ElrondNetwork network)
    {
        return network switch
        { 
            ElrondNetwork.Devnet => new DevnetNetworkStrategy(_configuration),
            ElrondNetwork.Testnet => new TestnetNetworkStrategy(_configuration),
            ElrondNetwork.Mainnet => new MainnetNetworkStrategy(_configuration),
            _ => new TestnetNetworkStrategy(_configuration)
        };
    }
}