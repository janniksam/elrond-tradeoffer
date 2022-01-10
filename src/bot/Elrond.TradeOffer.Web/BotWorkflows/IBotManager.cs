using Telegram.Bot;
using Telegram.Bot.Types;

namespace Elrond.TradeOffer.Web.BotWorkflows;

public interface IBotManager
{
    ITelegramBotClient? Client { get; }

    Task StartAsync(
        Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler,
        Func<ITelegramBotClient, Exception, CancellationToken, Task> errorHandler,
        CancellationToken ct);
}