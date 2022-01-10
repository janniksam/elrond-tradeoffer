using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.Bids;

public class Bid
{
    private Bid(Guid offerId, long creatorUserId, long creatorChatId, BidState bidState, DateTime createdAt, TokenAmount amount)
    {
        OfferId = offerId;
        CreatorUserId = creatorUserId;
        CreatorChatId = creatorChatId;
        Amount = amount;
        CreatedAt = createdAt;
        State = bidState;
    }

    public Guid OfferId { get; }

    public DateTime CreatedAt { get; }

    public long CreatorUserId { get; }

    public long CreatorChatId { get; }

    public TokenAmount Amount { get; }

    public BidState State { get; set; }

    public static Bid From(DbBid dbBid)
    {
        return new Bid(
            dbBid.OfferId,
            dbBid.CreatorUserId,
            dbBid.CreatorChatId,
            dbBid.State,
            dbBid.CreatedAt,
            TokenAmount.From(dbBid.TokenAmount,
                new Token(dbBid.TokenName, dbBid.TokenIdentifier, dbBid.TokenNonce, dbBid.TokenPrecision)));
    }
}