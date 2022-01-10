using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;

public interface ITemporaryBidManager
{
    TemporaryBid Get(long userId);

    void SetToken(long userId, Token token);

    void SetTokenAmount(long userId, TokenAmount tokenAmount);

    void SetOfferId(long userId, Guid offerId);

    void Reset(long userId);
}