namespace Elrond.TradeOffer.Web.Database;

public enum BidState
{
    Created = 0,
    Accepted = 1,
    Declined = 2,
    Removed = 3,
    RemovedWhileOnBlockchain = 4,
    CancelInitiated = 5,
    TradeInitiated = 6,
    ReadyForClaiming = 7,
}