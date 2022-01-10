#pragma warning disable CS8618
namespace Elrond.TradeOffer.Web.Database;

public class DbOffer
{
    public DbOffer(
        Guid id,
        DateTime createdAt,
        ElrondNetwork network,
        long creatorUserId,
        long creatorChatId, 
        string description,
        string tokenIdentifier, 
        string tokenName, 
        ulong tokenNonce,
        int tokenPrecision, 
        string tokenAmount)
    {
        Id = id;
        CreatedAt = createdAt;
        Network = network;
        CreatorUserId = creatorUserId;
        CreatorChatId = creatorChatId;
        Description = description;
        TokenIdentifier = tokenIdentifier;
        TokenName = tokenName;
        TokenNonce = tokenNonce;
        TokenPrecision = tokenPrecision;
        TokenAmount = tokenAmount;
    }

    public Guid Id { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public ElrondNetwork Network { get; set; }

    public long CreatorUserId { get; set; }

    public long CreatorChatId { get; set; }
    
    public string Description { get; set; }

    public string TokenIdentifier { get; set; }

    public string TokenName { get; set; }

    public ulong TokenNonce { get; set; }

    public int TokenPrecision { get; set; }

    public string TokenAmount { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual IEnumerable<DbBid> Bids { get; set; }
    
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual DbUser? CreatorUser { get; set; }
    
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}