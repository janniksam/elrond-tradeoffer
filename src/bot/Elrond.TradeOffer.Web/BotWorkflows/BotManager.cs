using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace Elrond.TradeOffer.Web.BotWorkflows;

public class BotManager : IBotManager
{
    public BotManager(IConfiguration configuration)
    {
        var token = configuration.GetValue<string>("TelegramBotToken");
        Client = new TelegramBotClient(token);
    }

    public ITelegramBotClient Client { get; }
    
    public async Task StartAsync(
        Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler,
        Func<ITelegramBotClient, Exception, CancellationToken, Task> errorHandler,
        CancellationToken ct)
    {
        var receiverOptions = new ReceiverOptions();
        await Client.GetMeAsync(ct);

        Client.StartReceiving(
            updateHandler,
            errorHandler,
            receiverOptions,
            ct);
    }
}