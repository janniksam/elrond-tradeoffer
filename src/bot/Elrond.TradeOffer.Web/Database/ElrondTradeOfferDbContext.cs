using Microsoft.EntityFrameworkCore;

namespace Elrond.TradeOffer.Web.Database;

public class ElrondTradeOfferDbContext: DbContext
{
    public ElrondTradeOfferDbContext(DbContextOptions<ElrondTradeOfferDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        DbOffer.OnModelCreating(modelBuilder);
        DbBid.OnModelCreating(modelBuilder);
        DbUser.OnModelCreating(modelBuilder);
        DbFeatureState.OnModelCreating(modelBuilder);

        EnableRowVersion<DbBid>(modelBuilder);
        EnableRowVersion<DbOffer>(modelBuilder);
        EnableRowVersion<DbUser>(modelBuilder);
        EnableRowVersion<DbFeatureState>(modelBuilder);
    }

    private static void EnableRowVersion<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        modelBuilder.Entity<T>()
            .Property(p => p.RowVersion)
            .IsRowVersion();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        UpdateCreatedOnUpdatedOn();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateCreatedOnUpdatedOn()
    {
        var addedEntities = ChangeTracker.Entries<BaseEntity>().Where(p => p.State == EntityState.Added).ToList();
        addedEntities.ForEach(p =>
        {
            var now = DateTime.UtcNow;
            p.Property(nameof(BaseEntity.CreatedOn)).CurrentValue = now;
            p.Property(nameof(BaseEntity.UpdatedOn)).CurrentValue = now;
        });

        var editedEntities = ChangeTracker.Entries<BaseEntity>().Where(p => p.State == EntityState.Modified).ToList();
        editedEntities.ForEach(p =>
        {
            p.Property(nameof(BaseEntity.UpdatedOn)).CurrentValue = DateTime.UtcNow;
        });
    }

    public virtual DbSet<DbOffer> Offers => Set<DbOffer>();

    public virtual DbSet<DbBid> Bids => Set<DbBid>();

    public virtual DbSet<DbUser> Users => Set<DbUser>();

    public virtual DbSet<DbFeatureState> FeatureStates => Set<DbFeatureState>();
}