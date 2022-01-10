using Elrond.TradeOffer.Web.Database;

namespace Elrond.TradeOffer.Web.Network;

public interface INetworkStrategies
{
    INetworkStrategy GetStrategy(ElrondNetwork network);
}