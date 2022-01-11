#pragma warning disable CS8618
namespace Elrond.TradeOffer.Web.Database;

public class DbBid : BaseEntity
{
    public DbBid(
        Guid offerId,
        long creatorUserId, 
        long creatorChatId,
        BidState state,
        string tokenIdentifier, 
        string tokenName,
        ulong tokenNonce, 
        int tokenPrecision,
        string tokenAmount)
    {
        OfferId = offerId;
        CreatorUserId = creatorUserId;
        CreatorChatId = creatorChatId;
        State = state;
        TokenIdentifier = tokenIdentifier;
        TokenName = tokenName;
        TokenNonce = tokenNonce;
        TokenPrecision = tokenPrecision;
        TokenAmount = tokenAmount;
    }

    public Guid OfferId { get; set; }

    public long CreatorUserId { get; set; }
    
    public long CreatorChatId { get; set; }
    
    public BidState State { get; set; }

    public string TokenIdentifier { get; set; }

    public string TokenName { get; set; }

    public ulong TokenNonce { get; set; }

    public int TokenPrecision { get; set; }

    public string TokenAmount { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual DbUser? CreatorUser { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual DbOffer Offer { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}