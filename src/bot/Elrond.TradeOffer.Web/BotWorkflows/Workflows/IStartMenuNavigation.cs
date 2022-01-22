using Telegram.Bot;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows;

public interface IStartMenuNavigation
{
    Task ShowStartMenuAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct);
}