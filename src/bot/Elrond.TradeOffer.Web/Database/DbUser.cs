using Microsoft.EntityFrameworkCore;

namespace Elrond.TradeOffer.Web.Database;

// ReSharper disable UnusedAutoPropertyAccessor.Local
public class DbUser : BaseEntity
{
    public DbUser(long id, string? address, ElrondNetwork network)
    {
        Id = id;
        Address = address;
        Network = network;
    }

    public long Id { get; private set; }
    
    public string? Address { get; set; }

    public ElrondNetwork Network { get; set; }
    
    public bool IsAdmin { get; private set; }

    public virtual IEnumerable<DbOffer> Offers { get; set; } = null!;

    public virtual IEnumerable<DbBid> Bids { get; set; } = null!;

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbUser>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<DbUser>()
            .Property(c => c.Id)
            .ValueGeneratedNever();
    }
}