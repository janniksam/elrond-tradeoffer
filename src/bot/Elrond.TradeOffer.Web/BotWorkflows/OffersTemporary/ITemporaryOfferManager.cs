using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;

public interface ITemporaryOfferManager
{
    TemporaryOffer Get(long userId);

    void SetToken(long userId, Token token);

    void SetTokenAmount(long userId, TokenAmount tokenAmount);

    void SetWantSomethingSpecific(long userId, bool wantSomethingSpecific);

    void SetTokenWant(long userId, Token? tokenWant);

    void SetTokenAmountWant(long userId, TokenAmount amountWant);
    
    void SetDescription(long userId, string description);
 
    void Reset(long userId);
}