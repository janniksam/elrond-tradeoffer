using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.Bids;

public class Bid
{
    private Bid(
        Guid offerId, 
        long creatorUserId, 
        long creatorChatId,
        BidState bidState, 
        DateTime createdOn,
        DateTime updatedOn,
        TokenAmount amount)
    {
        OfferId = offerId;
        CreatorUserId = creatorUserId;
        CreatorChatId = creatorChatId;
        Amount = amount;
        State = bidState;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
    }

    public Guid OfferId { get; }

    public long CreatorUserId { get; }

    public long CreatorChatId { get; }

    public TokenAmount Amount { get; }

    public BidState State { get; }

    public DateTime CreatedOn { get; }

    public DateTime UpdatedOn { get; }

    public static Bid From(DbBid dbBid)
    {
        return new Bid(
            dbBid.OfferId,
            dbBid.CreatorUserId,
            dbBid.CreatorChatId,
            dbBid.State,
            dbBid.CreatedOn,
            dbBid.UpdatedOn,
            TokenAmount.From(dbBid.TokenAmount,
                new Token(dbBid.TokenName, dbBid.TokenId, dbBid.TokenNonce, dbBid.TokenPrecision)));
    }
}