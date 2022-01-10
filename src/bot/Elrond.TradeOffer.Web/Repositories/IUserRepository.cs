using Elrond.TradeOffer.Web.BotWorkflows.User;

namespace Elrond.TradeOffer.Web.Repositories;

public interface IUserRepository
{
    Task AddOrUpdateAsync(ElrondUser user, CancellationToken ct);
    Task<ElrondUser> GetAsync(long userId, CancellationToken ct);
}