namespace Elrond.TradeOffer.Web.BotWorkflows.UserState;

public interface IUserContextManager
{
    UserContext Get(long userId);
    void AddOrUpdate(long userId, UserContext state);
}