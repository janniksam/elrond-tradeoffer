namespace Elrond.TradeOffer.Web.Database;

public class DbUser
{
    public DbUser(long id, string? address, ElrondNetwork network)
    {
        Id = id;
        Address = address;
        Network = network;
    }

    public long Id { get; set; }
    
    public string? Address { get; set; }

    public ElrondNetwork Network { get; set; }

    public virtual IEnumerable<DbOffer> Offers { get; set; } = null!;

    public virtual IEnumerable<DbBid> Bids { get; set; } = null!;
}