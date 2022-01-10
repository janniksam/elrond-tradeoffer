using Elrond.TradeOffer.Web.BotWorkflows.UserState;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows;

public class WorkflowResult
{
    private WorkflowResult(bool handled, UserContext context = UserContext.None)
    {
        IsHandled = handled;
        NewUserContext = context;
    }

    public bool IsHandled { get; }

    public UserContext NewUserContext { get; }

    public static WorkflowResult Handled(UserContext context = UserContext.None) => new(true, context); 

    public static WorkflowResult Unhandled() => new(false);
}