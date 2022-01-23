namespace Elrond.TradeOffer.Web.BotWorkflows.UserState;

public interface IUserContextManager
{
    (UserContext Context, int? OldMessageId, object[] AdditionalArgs) Get(long userId);

    void AddOrUpdate(long userId, (UserContext Context, int? OldMessageId, object[] additionalArgs) state);
}