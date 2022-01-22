using Elrond.TradeOffer.Web.BotWorkflows.Bids;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
using Elrond.TradeOffer.Web.Models;
using Telegram.Bot;

namespace Elrond.TradeOffer.Web.BotWorkflows;

public interface IBotNotificationsHelper
{
    Task NotifyOnOfferSendToBlockchainAsync(ITelegramBotClient client, Offer offer, Bid bid, CancellationToken ct);
    Task NotifyOnTradeCompletedAsync(ITelegramBotClient client, Offer offer, Bid bid, CancellationToken ct);
    Task NotifyOnOfferCancelledAsync(ITelegramBotClient client, Offer offer, CancellationToken ct);

    Task NotifyOnBidAccepted(
        ITelegramBotClient client, 
        long chatId, 
        Offer offer,
        Bid acceptedBid, 
        IEnumerable<Bid> declinedBids,
        CancellationToken ct);

    Task NotifyOnBidDeclined(ITelegramBotClient client, long chatId, Bid bid, CancellationToken ct);
    Task NotifyOnBidPlacedAsync(ITelegramBotClient client, Offer offer, long bidChatId, TokenAmount bidAmount, CancellationToken ct);
}