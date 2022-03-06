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

    public void SetToken(long userId, Token token)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryOffer { Token = token },
            (_, temporaryOffer) =>
            {
                temporaryOffer.Token = token;
                return temporaryOffer;
            });
    }

    public void SetTokenAmount(long userId, TokenAmount tokenAmount)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryOffer { Amount = tokenAmount },
            (_, temporaryOffer) =>
            {
                temporaryOffer.Amount = tokenAmount;
                return temporaryOffer;
            });
    }

    public void SetWantSomethingSpecific(long userId, bool wantSomethingSpecific)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryOffer
            {
                WantSomethingSpecific = wantSomethingSpecific
            },
            (_, temporaryOffer) =>
            {
                temporaryOffer.WantSomethingSpecific = wantSomethingSpecific;
                return temporaryOffer;
            });
    }

    public void SetTokenWant(long userId, Token? tokenWant)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryOffer
            {
                TokenWant = tokenWant,
            },
            (_, temporaryOffer) =>
            {
                temporaryOffer.TokenWant = tokenWant;
                return temporaryOffer;
            });
    }

    public void SetTokenAmountWant(long userId, TokenAmount amountWant)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryOffer { AmountWant = amountWant },
            (_, temporaryOffer) =>
            {
                temporaryOffer.AmountWant = amountWant;
                return temporaryOffer;
            });
    }

    public void SetDescription(long userId, string description)
    {
        _temporaryOffer.AddOrUpdate(
            userId,
            _ => new TemporaryOffer { Description = description },
            (_, temporaryOffer) =>
            {
                temporaryOffer.Description = description;
                return temporaryOffer;
            });
    }
}