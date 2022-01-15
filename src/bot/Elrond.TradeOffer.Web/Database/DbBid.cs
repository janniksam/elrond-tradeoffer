using Microsoft.EntityFrameworkCore;

namespace Elrond.TradeOffer.Web.Database;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
#pragma warning disable CS8618
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

    public Guid OfferId { get; private set; }

    public long CreatorUserId { get; private set; }
    
    public long CreatorChatId { get; private set; }
    
    public BidState State { get; set; }

    public string TokenIdentifier { get; private set; }

    public string TokenName { get; private set; }

    public ulong TokenNonce { get; private set; }

    public int TokenPrecision { get; private set; }

    public string TokenAmount { get; private set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual DbUser? CreatorUser { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual DbOffer Offer { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbBid>()
            .HasKey(b => new { b.OfferId, b.CreatorUserId });

        modelBuilder.Entity<DbBid>()
            .HasOne(p => p.Offer)
            .WithMany(b => b.Bids)
            .HasForeignKey(p => p.OfferId);

        modelBuilder.Entity<DbBid>()
            .HasOne(p => p.CreatorUser)
            .WithMany(b => b.Bids)
            .HasForeignKey(p => p.CreatorUserId);
    }
}