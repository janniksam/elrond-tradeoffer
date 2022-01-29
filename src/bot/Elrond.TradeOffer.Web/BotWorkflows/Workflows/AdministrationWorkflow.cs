using Elrond.TradeOffer.Web.Extensions;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows;

public class AdministrationWorkflow : IBotProcessor
{
    private const string ToggleDevNetQuery = "ToggleDevNet";
    private const string ToggleTestNetQuery = "ToggleTestNet";
    private const string ToggleMainNetQuery = "ToggleMainNet";
    private readonly IUserRepository _userRepository;
    private readonly IFeatureStatesManager _featureStatesManager;
    
    public AdministrationWorkflow(IUserRepository userRepository, IFeatureStatesManager featureStatesManager)
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

        if (query.Data == CommonQueries.AdministrationQuery)
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

    public Task<WorkflowResult> ProcessMessageAsync(ITelegramBotClient client, Message message, CancellationToken ct)
    {
        return Task.FromResult(WorkflowResult.Unhandled());
    }
}