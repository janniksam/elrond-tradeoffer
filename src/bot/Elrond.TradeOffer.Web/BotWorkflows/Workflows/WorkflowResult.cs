using Elrond.TradeOffer.Web.BotWorkflows.UserState;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows;

public class WorkflowResult
{
    private WorkflowResult(
        bool handled,
        UserContext context = UserContext.None,
        int? oldMessageId = null,
        params object[] additionalArgs)
    {
        IsHandled = handled;
        NewUserContext = context;
        OldMessageId = oldMessageId;
        AdditionalArgs = additionalArgs;
    }

    public bool IsHandled { get; }

    public UserContext NewUserContext { get; }
    
    public int? OldMessageId { get; }

    public object[] AdditionalArgs { get; }

    public static WorkflowResult Handled() => new(true); 

    public static WorkflowResult Handled(UserContext context, int oldMessageId, params object[] additionalInformation) => new(true, context, oldMessageId, additionalInformation); 

    public static WorkflowResult Unhandled() => new(false);
}