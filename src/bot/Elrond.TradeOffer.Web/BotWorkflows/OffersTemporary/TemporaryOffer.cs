using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;

public class TemporaryOffer
{
    public Token? Token { get; set; }

    public TokenAmount? Amount { get; set; }

    public bool? WantSomethingSpecific { get; set; }

    public Token? TokenWant { get; set; }

    public TokenAmount? AmountWant { get; set; }

    public string? Description { get; set; }

    public bool IsFilled
    {
        get
        {
            if (Token == null ||
                Amount == null ||
                Description == null ||
                WantSomethingSpecific == null)
            {
                return false;
            }

            if (WantSomethingSpecific.Value &&
                (AmountWant == null || TokenWant == null))
            {
                return false;
            }

            return true;
        }
    }
}