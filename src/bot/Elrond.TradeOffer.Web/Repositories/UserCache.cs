using System.Collections.Concurrent;
using Elrond.TradeOffer.Web.BotWorkflows.User;
using Elrond.TradeOffer.Web.Database;

namespace Elrond.TradeOffer.Web.Repositories;

public class UserCacheManager : IUserCacheManager
{
    private readonly ConcurrentDictionary<long, ElrondUser> _users = new();

    public void AddOrUpdate(long userId, ElrondUser user)
    {
        _users.AddOrUpdate(
            userId,
            _ => user,
            (_, _) => user);
    }

    public void SetNetwork(long userId, ElrondNetwork network)
    {
        _users.AddOrUpdate(
            userId,
            _ => 
            {
                var user = Default(userId);
                user.Network = network;
                return user;
            },
            (_, user) =>
            {
                user.Network = network;
                return user;
            });
    }

    public void SetAddress(long userId, string address)
    {
        _users.AddOrUpdate(
            userId,
            _ =>
            {
                var user = Default(userId);
                user.Address = address;
                return user;
            },
            (_, user) =>
            {
                user.Address = address;
                return user;
            });
    }

    public ElrondUser? Get(long userId)
    {
        return _users.TryGetValue(userId, out var user) ? user : null;
    }

    public ElrondUser CreateDefault(long userId)
    {
        return _users.GetOrAdd(userId, Default);
    }

    private static ElrondUser Default(long userId)
        => new(userId, null, ElrondNetwork.Devnet, false);
}