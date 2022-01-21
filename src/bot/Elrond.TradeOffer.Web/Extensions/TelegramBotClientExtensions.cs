using Elrond.TradeOffer.Web.Utils;
using Telegram.Bot;

namespace Elrond.TradeOffer.Web.Extensions;

public static class TelegramBotClientExtensions
{
    public static async Task<bool> TryDeleteMessageAsync(this ITelegramBotClient client, long chatId, int? messageId, CancellationToken ct)
    {
        if (messageId == null)
        {
            return true;
        }

        try
        {
            await ApiExceptionHelper.RunAndSupressAsync(() => client.DeleteMessageAsync(chatId, messageId.Value, ct));
            return true;
        }
        catch (Exception ex)
        {
            LoggingFactory.LogFactory?.CreateLogger(typeof(TelegramBotClientExtensions)).LogWarning(ex, "Could not delete message.");
            return false;
        }
    }
}