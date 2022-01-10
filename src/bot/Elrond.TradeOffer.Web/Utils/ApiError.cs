namespace Elrond.TradeOffer.Web.Utils;

[Flags]
public enum ApiError
{
    All = 0,
    ChatNotFound = 1,
    UserNotFound = 2,
    UserIsDeactivated = 4,
    BotWasKicked = 8,
    BotBlockedByUser = 16,
}