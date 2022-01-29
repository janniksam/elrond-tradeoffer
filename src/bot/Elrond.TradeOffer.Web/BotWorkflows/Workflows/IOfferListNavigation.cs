using Elrond.TradeOffer.Web.Repositories;
using Telegram.Bot;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows;

public interface IOfferListNavigation
{
    Task ShowOffersAsync(ITelegramBotClient client, long userId, long chatId, OfferFilter filter, CancellationToken ct);
}