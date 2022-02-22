namespace Elrond.TradeOffer.Web.BotWorkflows.UserState;

public enum UserContext
{
    None,
    EnterOfferAmount,
    EnterOfferWantToken,
    EnterOfferWantAmount,
    EnterOfferDescription,
    EnterBidAmount,
    EnterWalletAddress,
    EnterOfferSearchTerm,
    EnterDeclineBidReason,
    EnterOfferCode
}