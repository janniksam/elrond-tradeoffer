namespace Elrond.TradeOffer.Web.Repositories;

public enum RemoveBidResult
{
    RemovedCreatedOrDeclined,
    RemovedAccepted,
    RemovedWhileOnBlockchain,
    Failed,
    FailedBecauseInitiated,
}