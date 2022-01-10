using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;

public interface ITemporaryOfferManager
{
    TemporaryOffer Get(long userId);

    void SetTokenIdentifier(long userId, Token tokenidentifier);

    void SetTokenAmount(long userId, TokenAmount tokenAmount);

    void SetDescription(long userId, string description);
 
    void Reset(long userId);
}