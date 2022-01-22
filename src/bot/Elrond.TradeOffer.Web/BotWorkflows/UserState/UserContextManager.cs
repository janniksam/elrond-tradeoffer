using System.Collections.Concurrent;

namespace Elrond.TradeOffer.Web.BotWorkflows.UserState;

public class UserContextManager : IUserContextManager
{
    private readonly ConcurrentDictionary<long, (UserContext, int?)> _userContext = new();

    public (UserContext Context, int? OldMessageId) Get(long userId)
    {
        return _userContext.TryGetValue(userId, out var state) ? state : (UserContext.None, null);
    }

    public void AddOrUpdate(long userId, (UserContext, int?) state)
    {
        _userContext.AddOrUpdate(userId, state, (_,_) => state);
    }
}