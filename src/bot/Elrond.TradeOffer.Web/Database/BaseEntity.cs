namespace Elrond.TradeOffer.Web.Database;

public abstract class BaseEntity
{
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
}