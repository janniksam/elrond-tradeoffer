using Elrond.TradeOffer.Web.BotWorkflows.Bids;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;

namespace Elrond.TradeOffer.Web.Services;

public interface ITransactionGenerator
{
    Task<string> GenerateInitiateTradeUrlAsync(Offer offer, Bid acceptedBid);
    Task<string> GenerateFinalizeTradeUrlAsync(Offer offer, Bid acceptedBid);
    Task<string> GenerateReclaimUrlAsync(Offer offer);
}