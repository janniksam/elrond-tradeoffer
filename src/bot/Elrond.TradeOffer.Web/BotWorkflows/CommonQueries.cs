namespace Elrond.TradeOffer.Web.BotWorkflows;

internal static class CommonQueries
{
    public const string ViewOffersQuery = "ViewOffers";
    public const string ShowOfferQueryPrefix = "ShowOffer_";
    public const string CreateAnOfferQuery = "CreateAnOffer";
    public const string PlaceBidQueryPrefix = "PlaceBid_";
    public const string ChangeNetworkOrAddressQuery = "changeNetworkOrAddress";
    public const string BackToHomeQuery = "BackToHome";
    public const string AdministrationQuery = "administration";

    public static string ShowOfferQuery(Guid offerId) => $"{ShowOfferQueryPrefix}{offerId}";
}