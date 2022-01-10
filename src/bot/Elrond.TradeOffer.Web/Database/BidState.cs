namespace Elrond.TradeOffer.Web.Database;

public enum BidState
{
    Created = 0,
    Accepted = 1,
    Declined = 2,
    Removed = 3,
    Cancel = 4,
    TradeInitiated = 5,
    ReadyForClaiming = 6,
}