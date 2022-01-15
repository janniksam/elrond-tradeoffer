namespace Elrond.TradeOffer.Web.Repositories;

public class OfferFilter
{
    private OfferFilter()
    {
        IsUnfiltered = true;
    }

    private OfferFilter(bool onlyMyOwn, long userId)
    {
        OnlyMyOwn = onlyMyOwn;
        UserId = userId;
    }

    private OfferFilter(string searchTerm)
    {
        SearchTerm = searchTerm;
    }
    
    public bool IsUnfiltered { get; }
    
    public bool OnlyMyOwn { get; }

    public long? UserId { get; }

    public string? SearchTerm { get; }

    public static OfferFilter None() => new();

    public static OfferFilter OwnOffers(long userId) => new(true, userId);

    public static OfferFilter WithSearchTerm(string searchTerm) => new(searchTerm);
}