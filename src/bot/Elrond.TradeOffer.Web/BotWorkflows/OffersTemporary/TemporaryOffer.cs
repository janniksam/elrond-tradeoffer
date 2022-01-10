using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;

public class TemporaryOffer
{
    public Token? Token { get; set; }

    public TokenAmount? Amount { get; set; }

    public string? Description { get; set; }
}