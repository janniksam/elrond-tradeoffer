using Microsoft.EntityFrameworkCore;

namespace Elrond.TradeOffer.Web.Database;

// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618
public class DbFeatureState : BaseEntity
{
    public string Id { get; set; }

    public string Value { get; set; }

    public long? ChangedById { get; set; }

    public virtual DbUser? ChangedBy { get; set; }

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFeatureState>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<DbFeatureState>()
            .Property(c => c.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<DbFeatureState>()
            .HasOne(p => p.ChangedBy)
            .WithMany()
            .HasForeignKey(p => p.ChangedById);
    }
}