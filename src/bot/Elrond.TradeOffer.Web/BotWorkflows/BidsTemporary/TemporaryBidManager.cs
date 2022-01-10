using System.Collections.Concurrent;
using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;

public class TemporaryBidManager : ITemporaryBidManager
{
    private readonly ConcurrentDictionary<long, TemporaryBid> _temporaryOffer = new();

    public void Reset(long userId)
    {
        _temporaryOffer.TryRemove(userId, out _);
    }

    public TemporaryBid Get(long userId)
    {
        return _temporaryOffer.GetOrAdd(
            userId,
            _ => new TemporaryBid(userId));
    }

    public void SetOfferId(long userId, Guid offerId)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryBid(userId) { OfferId = offerId },
            (_, bid) =>
            {
                bid.OfferId = offerId;
                return bid;
            });
    }

    public void SetToken(long userId, Token token)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryBid(userId) { Token = token },
            (_, user) =>
            {
                user.Token = token;
                return user;
            });
    }

    public void SetTokenAmount(long userId, TokenAmount tokenAmount)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryBid(userId) { Amount = tokenAmount },
            (_, user) =>
            {
                user.Amount = tokenAmount;
                return user;
            });
    }
}