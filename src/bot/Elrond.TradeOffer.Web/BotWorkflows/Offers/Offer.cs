using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.Offers;

public class Offer
{
    private Offer(
        Guid id,
        DateTime createdOn,
        DateTime updatedOn,
        ElrondNetwork network,
        long creatorUserId,
        long creatorChatId,
        TokenAmount amount,
        string description)
    {
        Id = id;
        Network = network;
        CreatorUserId = creatorUserId;
        CreatorChatId = creatorChatId;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
        Amount = amount;
        Description = description;
    }
    
    public Guid Id { get; }

    public DateTime CreatedOn { get; }

    public DateTime UpdatedOn { get; }
    
    public ElrondNetwork Network { get; }
    
    public long CreatorUserId { get; }
    
    public long CreatorChatId { get; }
    
    public TokenAmount Amount { get; }

    public string Description { get; }

    public static Offer From(DbOffer dbOffer)
    {
        return new Offer(
            dbOffer.Id,
            dbOffer.CreatedOn,
            dbOffer.UpdatedOn,
            dbOffer.Network,
            dbOffer.CreatorUserId,
            dbOffer.CreatorChatId,
            TokenAmount.From(dbOffer.TokenAmount,
                new Token(dbOffer.TokenName, dbOffer.TokenTicker, dbOffer.TokenNonce, dbOffer.TokenPrecision)),
            dbOffer.Description);
    }
}