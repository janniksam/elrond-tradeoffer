using System.Collections.Concurrent;
using Elrond.TradeOffer.Web.Models;

namespace Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;

public class TemporaryOfferManager : ITemporaryOfferManager
{
    private readonly ConcurrentDictionary<long, TemporaryOffer> _temporaryOffer = new();
    
    public void Reset(long userId)
    {
        _temporaryOffer.TryRemove(userId, out _);
    }

    public TemporaryOffer Get(long userId)
    {
        return _temporaryOffer.GetOrAdd(
            userId,
            _ => new TemporaryOffer());
    }

    public void SetTokenIdentifier(long userId, Token tokenidentifier)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryOffer { Token = tokenidentifier },
            (_, user) =>
            {
                user.Token = tokenidentifier;
                return user;
            });
    }

    public void SetTokenAmount(long userId, TokenAmount tokenAmount)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryOffer { Amount = tokenAmount },
            (_, user) =>
            {
                user.Amount = tokenAmount;
                return user;
            });
    }

    public void SetDescription(long userId, string description)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryOffer { Description = description },
            (_, user) =>
            {
                user.Description = description;
                return user;
            });
    }
}