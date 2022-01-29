using Elrond.TradeOffer.Web.BotWorkflows.UserState;
using Elrond.TradeOffer.Web.Extensions;
using Elrond.TradeOffer.Web.Network;
using Elrond.TradeOffer.Web.Repositories;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows
{
    public class OfferListWorkflow : IBotProcessor, IOfferListNavigation
    {
        private const string SearchOfferQuery = "SearchOffers";
        private const string ShowMyOfferQuery = "ShowMyOffers";
        private const int MaxOffersPerPage = 10;

        private readonly IUserRepository _userManager;
        private readonly IOfferRepository _offerRepository;
        private readonly INetworkStrategies _networkStrategies;
        private readonly IStartMenuNavigation _startMenuNavigation;
        private readonly IUserContextManager _userContextManager;

        public OfferListWorkflow(
            IUserRepository userManager, 
            IOfferRepository offerRepository,
            INetworkStrategies networkStrategies,
            IUserContextManager userContextManager,
            IStartMenuNavigation startMenuNavigation)
        {
            _userManager = userManager;
            _offerRepository = offerRepository;
            _networkStrategies = networkStrategies;
            _startMenuNavigation = startMenuNavigation;
            _userContextManager = userContextManager;
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
           
            if (query.Data == CommonQueries.ViewOffersQuery)
            {
                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
                return WorkflowResult.Handled();
            }

            if (query.Data == ShowMyOfferQuery)
            {
                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await ShowOffersAsync(client, userId, chatId, OfferFilter.OwnOffers(userId), ct);
                return WorkflowResult.Handled();
            }

            if (query.Data == SearchOfferQuery)
            {
                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                return await EnterSearchTermAsync(client, chatId, ct);
            }
            
            return WorkflowResult.Unhandled();
        }

        private async Task<WorkflowResult> EnterSearchTermAsync(ITelegramBotClient client, long chatId, CancellationToken ct)
        {
            var sentMessage = await client.SendTextMessageAsync(
                chatId,
                "Please enter a search term: (e.g. \"mex\" or \"ride-7d18e9\")",
                cancellationToken: ct);

            return WorkflowResult.Handled(UserContext.EnterOfferSearchTerm, sentMessage.MessageId);
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
            var (context, oldMessageId, _) = _userContextManager.Get(message.From.Id);
            if (context == UserContext.EnterOfferSearchTerm)
            {
                await client.TryDeleteMessageAsync(chatId, oldMessageId, ct);
                return await SearchOffersAsync(client, userId, chatId, messageText, ct);
            }

            return WorkflowResult.Unhandled();
        }

        private async Task<WorkflowResult> SearchOffersAsync(ITelegramBotClient client, long userId, long chatId, string? messageText, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                await client.SendTextMessageAsync(chatId, "You need to supply a search term.", cancellationToken: ct);
                await ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
                return WorkflowResult.Handled();
            }

            var regEx = new Regex("^[A-Za-z0-9\\-]+$");
            if (!regEx.IsMatch(messageText))
            {
                await client.SendTextMessageAsync(chatId, 
                    "The search term only allows alphanumeric characters and the \"-\"-character.", cancellationToken: ct);
                await ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
                return WorkflowResult.Handled();
            }

            await ShowOffersAsync(client, userId, chatId, OfferFilter.WithSearchTerm(messageText), ct);
            return WorkflowResult.Handled();
        }

        public async Task ShowOffersAsync(ITelegramBotClient client, long userId, long chatId, OfferFilter filter, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "You have set an address before you continue.",
                    cancellationToken: ct);
                await _startMenuNavigation.ShowStartMenuAsync(client, userId, chatId, ct);
                return;
            }

            var networkStrategy = _networkStrategies.GetStrategy(elrondUser.Network);
            var networkReady = await networkStrategy.IsNetworkReadyAsync(ct);
            if (!networkReady)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    $"The network {elrondUser.Network} is currently not available.",
                    cancellationToken: ct);
                await _startMenuNavigation.ShowStartMenuAsync(client, userId, chatId, ct);
                return;
            }

            var offers = await _offerRepository.GetOffersAsync(elrondUser.Network, filter, MaxOffersPerPage, ct);

            var buttons = new List<InlineKeyboardButton[]>();
            
            var message = $"The following offers were found ({elrondUser.Network}, latest 10):";
            if (offers.Count == 0)
            {
                message += "\n\nNo offers were found.";
            }
            else
            {
                foreach (var offer in offers)
                {
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData($"{offer.Amount.ToCurrencyStringWithIdentifier()} - {offer.Description}", CommonQueries.ShowOfferQuery(offer.Id))
                    });
                }
            }

            if (filter.IsUnfiltered)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔍 Search for term", SearchOfferQuery),
                    InlineKeyboardButton.WithCallbackData("🙋 Show my offers", ShowMyOfferQuery)
                });
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Back", CommonQueries.BackToHomeQuery) });
            }
            else
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Back to unfiltered list", CommonQueries.ViewOffersQuery) });
            }

            await client.SendTextMessageAsync(
                chatId, 
                message,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }
    }
}
