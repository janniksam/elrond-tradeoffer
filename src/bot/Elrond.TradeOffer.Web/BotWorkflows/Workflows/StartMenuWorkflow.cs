using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows;

public class StartMenuWorkflow : IBotProcessor
{
    private const string AboutQuery = "about";
    private const string AboutText = "Made with 💚 by [janniksam](https://twitter.com/janniksamc/)\n\n" +
                                      "**Socials**\n" +
                                      "[Telegram Bot Discussion Group](https://t.me/elrondTradeOffer)\n" +
                                      "[Twitter \\(Coming soon\\)](https://twitter.com)\n\n" +
                                      "**Source\\-Code**:\n" +
                                      "[GitHub Repository](https://github.com/janniksam/elrond-tradeoffer)";

    public async Task<WorkflowResult> ProcessCallbackQueryAsync(ITelegramBotClient client, CallbackQuery query, CancellationToken ct)
    {
        if (query.Message == null ||
            query.Data == null)
        {
            return WorkflowResult.Unhandled();
        }

        var chatId = query.Message.Chat.Id;
        var previousMessageId = query.Message.MessageId;

        if (query.Data == AboutQuery)
        {
            await DeleteMessageAsync(client, chatId, previousMessageId, ct);
            await ShowAboutAsync(client, chatId, ct);
            return WorkflowResult.Handled();
        }

        if (query.Data == CommonQueries.BackToHomeQuery)
        {
            await DeleteMessageAsync(client, chatId, previousMessageId, ct);
            await StartPage(client, chatId, ct);
            return WorkflowResult.Handled();
        }

        return WorkflowResult.Unhandled();
    }

    public async Task<WorkflowResult> ProcessMessageAsync(ITelegramBotClient client, Message message, CancellationToken ct)
    {
        if (message.From == null)
        {
            return WorkflowResult.Unhandled();
        }

        if (message.Type != MessageType.Text)
        {
            return WorkflowResult.Unhandled();
        }

        var messageText = message.Text;
        var chatId = message.Chat.Id;

        if (messageText is "/start" or "/menu")
        {
            await StartPage(client, chatId, ct);
            return WorkflowResult.Handled();
        }

        return WorkflowResult.Unhandled();
    }

    public static async Task StartPage(ITelegramBotClient client, long chatId, CancellationToken ct)
    {
        InlineKeyboardMarkup keyboardMarkup = new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Place an offer", CommonQueries.CreateAnOfferQuery),
                    InlineKeyboardButton.WithCallbackData("🔎 View offers", CommonQueries.ViewOffersQuery)
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔧 Change network or address", CommonQueries.ChangeNetworkOrAddressQuery),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("About the author", AboutQuery)
                },
            });

        await client.SendTextMessageAsync(
            chatId,
            "Welcome to the Elrond Trade Offer Bot.\n\n" +
            "This bot allows you to safely exchange tokens between two parties. One party will make an offer, the other parties can place bids.\n\n" +
            "If both parties accept the conditions and come to an agreement, a Smart Contract will be used to play the middleman and exchange the tokens according to the conditions both parties have agreed on.\n\n" +
            "Please choose an action:",
            replyMarkup: keyboardMarkup,
            cancellationToken: ct);
    }

    private static async Task ShowAboutAsync(ITelegramBotClient client, long chatId, CancellationToken ct)
    {
        await client.SendTextMessageAsync(chatId,
            AboutText,
            ParseMode.MarkdownV2,
            disableWebPagePreview: true,
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("Back", CommonQueries.BackToHomeQuery)),
            cancellationToken: ct);
    }

    private static async Task DeleteMessageAsync(ITelegramBotClient client, long chatId, int previousMessageId, CancellationToken ct)
    {
        await client.DeleteMessageAsync(chatId, previousMessageId, ct);
    }
}