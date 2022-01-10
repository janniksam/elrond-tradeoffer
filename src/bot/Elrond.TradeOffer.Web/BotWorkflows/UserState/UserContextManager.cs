using System.Collections.Concurrent;

namespace Elrond.TradeOffer.Web.BotWorkflows.UserState;

public class UserContextManager : IUserContextManager
{
    private readonly ConcurrentDictionary<long, UserContext> _userContext = new();

    public UserContext Get(long userId)
    {
        return _userContext.TryGetValue(userId, out var state) ? state : UserContext.None;
    }

    public void AddOrUpdate(long userId, UserContext state)
    {
        _userContext.AddOrUpdate(userId, state, (_,_) => state);
    }
}