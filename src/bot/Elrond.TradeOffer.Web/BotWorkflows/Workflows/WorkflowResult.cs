using Elrond.TradeOffer.Web.BotWorkflows.UserState;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows;

public class WorkflowResult
{
    private WorkflowResult(
        bool handled,
        UserContext context = UserContext.None,
        int? oldMessageId = null)
    {
        IsHandled = handled;
        NewUserContext = context;
        OldMessageId = oldMessageId;
    }

    public bool IsHandled { get; }

    public UserContext NewUserContext { get; }
    
    public int? OldMessageId { get; }

    public static WorkflowResult Handled() => new(true); 

    public static WorkflowResult Handled(UserContext context, int oldMessageId) => new(true, context, oldMessageId); 

    public static WorkflowResult Unhandled() => new(false);
}