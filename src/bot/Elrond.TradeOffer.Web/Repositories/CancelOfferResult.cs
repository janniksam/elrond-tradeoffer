namespace Elrond.TradeOffer.Web.Repositories;

public enum CancelOfferResult
{
    Success,
    InvalidUser,
    CreatorNeedsToRetrieveTokens,
    OfferNotFound
}