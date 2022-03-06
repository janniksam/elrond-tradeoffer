using Telegram.Bot;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows;

public interface IOfferDetailNavigation
{
    Task ShowOfferAsync(ITelegramBotClient client, long userId, long chatId, Guid offerId, CancellationToken ct);
}