using Elrond.TradeOffer.Web.Extensions;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows;

public class StartMenuWorkflow : IBotProcessor, IStartMenuNavigation
{
    private readonly IUserRepository _userRepository;
    private readonly IFeatureStatesManager _featureStatesManager;
    private const string AboutQuery = "about";
    private const string AdministrationQuery = "administration";
    private const string ToggleDevNetQuery = "ToggleDevNet";
    private const string ToggleTestNetQuery = "ToggleTestNet";
    private const string ToggleMainNetQuery = "ToggleMainNet";
    private const string AboutText = "Made with 💚 by [janniksam](https://twitter.com/janniksamc/)\n\n" +
                                     "**Socials**\n" +
                                     "[Telegram Bot Discussion Group](https://t.me/elrondTradeOffer)\n" +
                                     "[Twitter \\(Coming soon\\)](https://twitter.com)\n\n" +
                                     "**Source\\-Code**:\n" +
                                     "[GitHub Repository](https://github.com/janniksam/elrond-tradeoffer)";

    public StartMenuWorkflow(IUserRepository userRepository, IFeatureStatesManager featureStatesManager)
    {
        _userRepository = userRepository;
        _featureStatesManager = featureStatesManager;
    }

    public async Task<WorkflowResult> ProcessCallbackQueryAsync(ITelegramBotClient client, CallbackQuery query, CancellationToken ct)
    {
        if (query.Message == null ||
            query.Data == null)
        {
            return WorkflowResult.Unhandled();
        }

        var userId = query.From.Id;
        var chatId = query.Message.Chat.Id;
        var previousMessageId = query.Message.MessageId;

        if (query.Data == AboutQuery)
        {
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
            await ShowAboutAsync(client, chatId, ct);
            return WorkflowResult.Handled();
        }

        if (query.Data == CommonQueries.BackToHomeQuery)
        {
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
            await ShowStartMenuAsync(client, userId, chatId, ct);
            return WorkflowResult.Handled();
        }

        if (query.Data == AdministrationQuery)
        {
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
            await ShowAdministrationAsync(client, userId, chatId, ct);
            return WorkflowResult.Handled();
        }

        if (query.Data == ToggleDevNetQuery)
        {
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
            await ToggleDevNetAsync(client, userId, chatId, ct);
            return WorkflowResult.Handled();
        }

        if (query.Data == ToggleTestNetQuery)
        {
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
            await ToggleTestNetAsync(client, userId, chatId, ct);
            return WorkflowResult.Handled();
        }

        if (query.Data == ToggleMainNetQuery)
        {
            await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
            await ToggleMainNetAsync(client, userId, chatId, ct);
            return WorkflowResult.Handled();
        }

        return WorkflowResult.Unhandled();
    }

    private async Task ToggleDevNetAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
    {
        var user = await _userRepository.GetAsync(userId, ct);
        if (!user.IsAdmin)
        {
            return;
        }

        var networkEnabled = await _featureStatesManager.GetDevNetEnabledAsync(ct);
        await _featureStatesManager.SetDevNetStateAsync(!networkEnabled, userId, ct);
        await ShowAdministrationAsync(client, userId, chatId, ct);
    }

    private async Task ToggleTestNetAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
    {
        var user = await _userRepository.GetAsync(userId, ct);
        if (!user.IsAdmin)
        {
            return;
        }

        var networkEnabled = await _featureStatesManager.GetTestNetEnabledAsync(ct);
        await _featureStatesManager.SetTestNetStateAsync(!networkEnabled, userId, ct);
        await ShowAdministrationAsync(client, userId, chatId, ct);
    }

    private async Task ToggleMainNetAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
    {
        var user = await _userRepository.GetAsync(userId, ct);
        if (!user.IsAdmin)
        {
            return;
        }

        var networkEnabled = await _featureStatesManager.GetMainNetEnabledAsync(ct);
        await _featureStatesManager.SetMainNetStateAsync(!networkEnabled, userId, ct);
        await ShowAdministrationAsync(client, userId, chatId, ct);
    }

    private async Task ShowAdministrationAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
    {
        var user = await _userRepository.GetAsync(userId, ct);
        if (!user.IsAdmin)
        {
            return;
        }

        var buttons = new List<InlineKeyboardButton[]>
        {
            new[] { InlineKeyboardButton.WithCallbackData("Toggle DevNet", ToggleDevNetQuery) },
            new[] { InlineKeyboardButton.WithCallbackData("Toggle TestNet", ToggleTestNetQuery) },
            new[] { InlineKeyboardButton.WithCallbackData("Toggle MainNet", ToggleMainNetQuery) },
            new[] { InlineKeyboardButton.WithCallbackData("Back", CommonQueries.BackToHomeQuery) }
        };

        var devNetEnabled = await _featureStatesManager.GetDevNetEnabledAsync(ct);
        var testNetEnabled = await _featureStatesManager.GetTestNetEnabledAsync(ct);
        var mainNetEnabled = await _featureStatesManager.GetMainNetEnabledAsync(ct);

        var message = "<u><b>Administration</b></u>\n" +
                      $"DevNet enabled = {devNetEnabled}\n" +
                      $"TestNet enabled = {testNetEnabled}\n" +
                      $"MainNet enabled = {mainNetEnabled}";

        await client.SendTextMessageAsync(chatId, message,
            ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
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
        var userId = message.From.Id;
        var chatId = message.Chat.Id;

        if (messageText is "/start" or "/menu")
        {
            await ShowStartMenuAsync(client, userId, chatId, ct);
            return WorkflowResult.Handled();
        }

        return WorkflowResult.Unhandled();
    }

    public async Task ShowStartMenuAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
    {
        var user = await _userRepository.GetAsync(userId, ct);
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("➕ Place an offer", CommonQueries.CreateAnOfferQuery),
                InlineKeyboardButton.WithCallbackData("🔎 View offers", CommonQueries.ViewOffersQuery)
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔧 Change network or address", CommonQueries.ChangeNetworkOrAddressQuery),
            }
        };
        

        if (user.IsAdmin)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("👮 Administration", AdministrationQuery),
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("💡 About the author", AboutQuery)
        });

        await client.SendTextMessageAsync(
            chatId,
            "Welcome to the Elrond Trade Offer Bot.\n\n" +
            "This bot allows you to safely exchange tokens between two parties. One party will make an offer, the other parties can place bids.\n\n" +
            "If both parties accept the conditions and come to an agreement, a Smart Contract will be used to play the middleman and exchange the tokens according to the conditions both parties have agreed on.\n\n" +
            "Please choose an action:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
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
}