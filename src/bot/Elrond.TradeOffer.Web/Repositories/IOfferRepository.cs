using System.Linq.Expressions;
using Elrond.TradeOffer.Web.BotWorkflows.Bids;
using Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
using Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.User;
using Elrond.TradeOffer.Web.Database;

namespace Elrond.TradeOffer.Web.Repositories;

public interface IOfferRepository
{
    Task<Guid> PlaceAsync(ElrondUser user, TemporaryOffer temporaryOffer, long chatId, CancellationToken ct);

    Task<Offer?> GetAsync(Guid offerId, CancellationToken ct);

    Task<IReadOnlyCollection<Offer>> GetOffersAsync(ElrondNetwork network, OfferFilter filter, int maxAmount, CancellationToken ct);
        
    Task<CancelOfferResult> CancelAsync(Guid orderId, long userId, CancellationToken ct);
    
    Task<bool> PlaceBidAsync(TemporaryBid temporaryOffer, long chatId, CancellationToken ct);
    
    Task<bool> RemoveBidAsync(Guid offerId, long userId, CancellationToken ct);
    
    Task<IReadOnlyCollection<Bid>> GetBidsAsync(Guid offerId, Expression<Func<DbBid, bool>> predicate, CancellationToken ct);

    Task<IReadOnlyCollection<(Bid, Offer)>> GetInitiatedOffersAsync(CancellationToken ct);

    Task<IReadOnlyCollection<(Bid, Offer)>> GetClaimableOffersAsync(CancellationToken ct);
    
    Task<IReadOnlyCollection<(Bid, Offer)>> GetCancellingOffersAsync(CancellationToken ct);

    Task<IReadOnlyCollection<Bid>> GetBidsAsync(Guid offerId, CancellationToken ct);
    
    Task<bool> UpdateBidAsync(Guid offerId, long userId, Func<DbBid, bool> updateFunc, CancellationToken ct);

    Task<Bid?> GetBidAsync(Guid offerId, long userId, CancellationToken ct);
    
    Task CompleteOfferAsync(Guid offerId, CancellationToken ct);

    Task<(bool sucessfullyAccepted, IReadOnlyCollection<Bid> declinedBids)> AcceptAsync(Guid offerId, long bidUserId, CancellationToken ct);

    Task<Bid?> GetPendingBidAsync(Guid offerId, CancellationToken ct);
}