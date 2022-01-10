using Elrond.TradeOffer.Web.BotWorkflows.User;
using Elrond.TradeOffer.Web.Database;

namespace Elrond.TradeOffer.Web.Repositories;

public interface IUserCacheManager
{
    void AddOrUpdate(long userId, ElrondUser user);

    void SetNetwork(long userId, ElrondNetwork network);

    void SetAddress(long userId, string address);

    ElrondUser? Get(long userId);
    
    ElrondUser CreateDefault(long userId);
}