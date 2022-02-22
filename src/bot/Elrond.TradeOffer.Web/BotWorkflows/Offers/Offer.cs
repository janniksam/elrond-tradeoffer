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
        TokenAmount? tokenWant,
        string description)
    {
        Id = id;
        Network = network;
        CreatorUserId = creatorUserId;
        CreatorChatId = creatorChatId;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
        Amount = amount;
        AmountWant = tokenWant;
        Description = description;
    }
    
    public Guid Id { get; }

    public DateTime CreatedOn { get; }

    public DateTime UpdatedOn { get; }
    
    public ElrondNetwork Network { get; }
    
    public long CreatorUserId { get; }
    
    public long CreatorChatId { get; }
    
    public TokenAmount Amount { get; }

    public TokenAmount? AmountWant { get; }

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
            GetTokensOffered(dbOffer), 
            GetTokenAmountWanted(dbOffer),
            dbOffer.Description);
    }

    private static TokenAmount GetTokensOffered(DbOffer dbOffer)
    {
        return TokenAmount.From(
            dbOffer.TokenAmount,
            new Token(dbOffer.TokenName, dbOffer.TokenId, dbOffer.TokenNonce, dbOffer.TokenPrecision));
    }

    private static TokenAmount? GetTokenAmountWanted(DbOffer dbOffer)
    {
        if (dbOffer.WantsSomethingSpecific &&
            dbOffer.TokenNameWant != null &&
            dbOffer.TokenAmountWant != null &&
            dbOffer.TokenIdWant != null &&
            dbOffer.TokenNonceWant != null &&
            dbOffer.TokenPrecisionWant != null)
        {
            return TokenAmount.From(
                dbOffer.TokenAmountWant,
                new Token(dbOffer.TokenNameWant, dbOffer.TokenIdWant, dbOffer.TokenNonceWant.Value, dbOffer.TokenPrecisionWant.Value));
        }

        return null;
    }
}