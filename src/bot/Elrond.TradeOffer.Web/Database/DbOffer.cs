using Microsoft.EntityFrameworkCore;

namespace Elrond.TradeOffer.Web.Database;

#pragma warning disable CS8618
public class DbOffer : BaseEntity
{
    public DbOffer(
        Guid id,
        ElrondNetwork network,
        long creatorUserId,
        long creatorChatId,
        string description,
        string tokenId,
        string tokenName,
        ulong tokenNonce,
        int tokenPrecision,
        string tokenAmount,
        bool wantsSomethingSpecific,
        string? tokenIdWant,
        string? tokenNameWant,
        ulong? tokenNonceWant,
        int? tokenPrecisionWant, 
        string? tokenAmountWant)
    {
        Id = id;
        Network = network;
        CreatorUserId = creatorUserId;
        CreatorChatId = creatorChatId;
        Description = description;
        TokenId = tokenId;
        TokenName = tokenName;
        TokenNonce = tokenNonce;
        TokenPrecision = tokenPrecision;
        TokenAmount = tokenAmount;

        WantsSomethingSpecific = wantsSomethingSpecific;
        TokenIdWant = tokenIdWant;
        TokenNameWant = tokenNameWant;
        TokenNonceWant = tokenNonceWant;
        TokenPrecisionWant = tokenPrecisionWant;
        TokenAmountWant = tokenAmountWant;
    }

    public Guid Id { get; private set; }
    
    public ElrondNetwork Network { get; private set; }

    public long CreatorUserId { get; private set; }

    public long CreatorChatId { get; private set; }
    
    public string Description { get; private set; }

    public string TokenId { get; private set; }
    
    public string TokenName { get; private set; }

    public ulong TokenNonce { get; private set; }

    public int TokenPrecision { get; private set; }

    public string TokenAmount { get; private set; }

    public bool WantsSomethingSpecific { get; private set; }

    public string? TokenIdWant { get; private set; }
    
    public string? TokenNameWant { get; private set; }

    public ulong? TokenNonceWant { get; private set; }
    
    public int? TokenPrecisionWant { get; private set; }

    public string? TokenAmountWant { get; private set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual IEnumerable<DbBid> Bids { get; set; }
    
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual DbUser? CreatorUser { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbOffer>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<DbOffer>()
            .HasOne(p => p.CreatorUser)
            .WithMany(b => b.Offers)
            .HasForeignKey(p => p.CreatorUserId);
    }
}