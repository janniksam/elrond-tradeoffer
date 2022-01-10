namespace Elrond.TradeOffer.Web.Models;

public class ElrondToken
{
    public ElrondToken(Token token, string balance)
    {
        Token = token;
        Amount = TokenAmount.From(balance, token);
    }

    public Token Token { get; set; }

    public TokenAmount Amount { get; set; }
}