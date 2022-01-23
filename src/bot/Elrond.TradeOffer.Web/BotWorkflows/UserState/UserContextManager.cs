using System.Collections.Concurrent;

namespace Elrond.TradeOffer.Web.BotWorkflows.UserState;

public class UserContextManager : IUserContextManager
{
    private readonly ConcurrentDictionary<long, (UserContext, int?, object[] additionalArgs)> _userContext = new();

    public (UserContext Context, int? OldMessageId, object[] AdditionalArgs) Get(long userId)
    {
        return _userContext.TryGetValue(userId, out var state) ? state : (UserContext.None, null, Array.Empty<object>());
    }

    public void AddOrUpdate(long userId, (UserContext, int?, object[]) state)
    {
        _userContext.AddOrUpdate(userId, state, (_,_) => state);
    }
}