using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;

public class TemporaryBid
{
    public TemporaryBid(long creatorUserId)
    {
        CreatorUserId = creatorUserId;
        BidState = BidState.Created;
    }

    public long CreatorUserId { get; }
    
    public Guid? OfferId { get; set; }

    public Token? Token { get; set; }

    public TokenAmount? Amount { get; set; }

    public BidState BidState { get; set; }
}