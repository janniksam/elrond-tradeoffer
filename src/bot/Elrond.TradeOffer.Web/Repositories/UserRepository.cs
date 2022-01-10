using Elrond.TradeOffer.Web.BotWorkflows.User;
using Elrond.TradeOffer.Web.Database;
using Microsoft.EntityFrameworkCore;

namespace Elrond.TradeOffer.Web.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbContextFactory<ElrondTradeOfferDbContext> _dbContextFactory;
    private readonly IUserCacheManager _userCacheManager;

    public UserRepository(
        IDbContextFactory<ElrondTradeOfferDbContext> dbContextFactory,
        IUserCacheManager userCacheManager)
    {
        _dbContextFactory = dbContextFactory;
        _userCacheManager = userCacheManager;
    }

    public async Task AddOrUpdateAsync(ElrondUser user, CancellationToken ct)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        var dbUser = await dbContext.Users.FirstOrDefaultAsync(p => p.Id == user.UserId, ct);
        if (dbUser == null)
        {
            dbUser = new DbUser(user.UserId, user.Address, user.Network);
            dbContext.Users.Add(dbUser);
        }
        else
        {
            dbUser.Address = user.Address;
            dbUser.Network = user.Network;
        }

        await dbContext.SaveChangesAsync(ct);
        _userCacheManager.AddOrUpdate(dbUser.Id, ElrondUser.From(dbUser));
    }

    public async Task<ElrondUser> GetAsync(long userId, CancellationToken ct)
    {
        var user = _userCacheManager.Get(userId);
        if (user != null)
        {
            return user;
        }
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        var dbUser = await dbContext.Users.FirstOrDefaultAsync(p => p.Id == userId, ct);
        if (dbUser == null)
        {
            return _userCacheManager.CreateDefault(userId);
        }

        var userFromDb = ElrondUser.From(dbUser);
        _userCacheManager.AddOrUpdate(dbUser.Id, userFromDb);
        return userFromDb;
    }
}