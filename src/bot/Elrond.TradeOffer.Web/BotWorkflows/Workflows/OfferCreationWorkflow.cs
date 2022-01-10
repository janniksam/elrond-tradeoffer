using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.UserState;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows
{
    public class OfferCreationWorkflow : IBotProcessor
    {
        private const string PlaceOfferTokenQueryPrefix = "CreateOfferToken_";
        private const string PlaceOfferQuery = "PlaceOffer";
        private readonly IUserRepository _userManager;
        private readonly IUserContextManager _userContextManager;
        private readonly ITemporaryOfferManager _temporaryOfferManager;
        private readonly IOfferRepository _offerRepository;
        private readonly IElrondApiService _elrondApiService;
        private readonly Func<ITelegramBotClient, long, CancellationToken, Task> _backToStart;

        public OfferCreationWorkflow(
            IUserRepository userManager,
            IUserContextManager userContextManager,
            ITemporaryOfferManager temporaryOfferManager, 
            IOfferRepository offerRepository, 
            IElrondApiService elrondApiService,
            Func<ITelegramBotClient, long, CancellationToken, Task> backToStart)
        {
            _userManager = userManager;
            _userContextManager = userContextManager;
            _temporaryOfferManager = temporaryOfferManager;
            _offerRepository = offerRepository;
            _elrondApiService = elrondApiService;
            _backToStart = backToStart;
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
            var context = _userContextManager.Get(message.From.Id);
            if (context == UserContext.EnterOfferAmount)
            {
                return await CreateOfferOnTokenAmountChosenAsync(client, message.From.Id, chatId, messageText, ct);
            }
            
            if (context == UserContext.EnterOfferDescription)
            {
                return await CreateOfferOnDescriptionChosenAsync(client, message.From.Id, chatId, messageText, ct);
            }

            return WorkflowResult.Unhandled();
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

            if (query.Data == CommonQueries.CreateAnOfferQuery)
            {
                await DeleteMessageAsync(client, chatId, previousMessageId, ct);
                return await CreateANewOfferAsync(client, userId, chatId, ct);
            }

            if (query.Data.StartsWith(PlaceOfferTokenQueryPrefix))
            {
                await DeleteMessageAsync(client, chatId, previousMessageId, ct);
                var tokenIdentifier = query.Data[PlaceOfferTokenQueryPrefix.Length..];
                return await CreateOfferOnTokenIdentifierChosenAsync(client, userId, chatId, tokenIdentifier, ct);
            }

            if (query.Data == PlaceOfferQuery)
            {
                await DeleteMessageAsync(client, chatId, previousMessageId, ct);
                await PlaceOfferAsync(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }

            return WorkflowResult.Unhandled();
        }

        private async Task DeleteMessageAsync(ITelegramBotClient client, long chatId, int previousMessageId, CancellationToken ct)
        {
            await client.DeleteMessageAsync(chatId, previousMessageId, ct);
        }

        private async Task<WorkflowResult> CreateANewOfferAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            _temporaryOfferManager.Reset(userId);

            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "You have set an address before you continue.",
                    cancellationToken: ct);
                await _backToStart(client, chatId, ct);
                return WorkflowResult.Handled();
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }

        private async Task<WorkflowResult> CreateOfferOnTokenIdentifierChosenAsync(ITelegramBotClient client, long userId, long chatId, string tokenIdentifier, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "You have set an address before you continue.",
                    cancellationToken: ct);
                await _backToStart(client, chatId, ct);
                return WorkflowResult.Handled();
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            var token = balances.FirstOrDefault(p => p.Token.Identifier == tokenIdentifier)?.Token;
            if (token != null)
            {
                _temporaryOfferManager.SetTokenIdentifier(userId, token);
            }

            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }

        private async Task<WorkflowResult> CreateOfferOnTokenAmountChosenAsync(ITelegramBotClient client, long userId, long chatId, string? tokenAmount, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "You have set an address before you continue.",
                    cancellationToken: ct);
                await _backToStart(client, chatId, ct);
                return WorkflowResult.Handled();
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();

            var regexAmount = new Regex("^[0-9]+(\\.[0-9]+)?$");
            if (tokenAmount == null || !regexAmount.IsMatch(tokenAmount))
            {
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            var temporaryOffer = _temporaryOfferManager.Get(userId);
            if (temporaryOffer.Token == null)
            {
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            var tokenBalance = balances.FirstOrDefault(p => p.Token.Identifier == temporaryOffer.Token.Identifier);
            if (tokenBalance == null)
            {
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            var tokenAmountDecimal = decimal.Parse(tokenAmount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            var amountChosen = TokenAmount.From(tokenAmountDecimal, tokenBalance.Token);
            if (amountChosen.Value > tokenBalance.Amount.Value)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    $"The amount {amountChosen.ToCurrencyString()} is higher than the avaliable amount {tokenBalance.Amount.ToCurrencyString()}.",
                    cancellationToken: ct);
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            if (amountChosen.Value <= BigInteger.Zero)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    $"The amount {amountChosen.ToCurrencyString()} must be bigger than 0.",
                    cancellationToken: ct);
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            _temporaryOfferManager.SetTokenAmount(userId, amountChosen);
            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }


        private async Task<WorkflowResult> CreateOfferOnDescriptionChosenAsync(ITelegramBotClient client, long userId, long chatId, string? description, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "You have set an address before you continue.",
                    cancellationToken: ct);
                await _backToStart(client, chatId, ct);
                return WorkflowResult.Handled();
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            if (string.IsNullOrWhiteSpace(description) || description.Length < 10)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "Invalid description. The description needs to have a length of atleast 10 characters.",
                    cancellationToken: ct);
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            _temporaryOfferManager.SetDescription(userId, description);
            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }

        private async Task PlaceOfferAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "You have set an address before you continue.",
                    cancellationToken: ct);
                await _backToStart(client, chatId, ct);
                return;
            }

            var temporaryOffer = _temporaryOfferManager.Get(userId);
            if (temporaryOffer.Token == null ||
                temporaryOffer.Amount == null ||
                temporaryOffer.Description == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "Incomplete order. Try again.",
                    cancellationToken: ct);
                await _backToStart(client, chatId, ct);
                return;
            }

            await _offerRepository.PlaceAsync(elrondUser, temporaryOffer, chatId, ct);
            _temporaryOfferManager.Reset(userId);

            await client.SendTextMessageAsync(
                chatId,
                "The offer has been placed.",
                cancellationToken: ct);
            await _backToStart(client, chatId, ct);
        }

        private async Task<WorkflowResult> CreateOfferWizard(ITelegramBotClient client, long userId, long chatId,
            IReadOnlyCollection<ElrondToken> balances, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);

            var temporaryOffer = _temporaryOfferManager.Get(userId);
            if (temporaryOffer.Token == null)
            {
                var message =
                    $"You're trying creating an offer for the {elrondUser.Network}. What do you want to offer?";
                var buttons = new List<InlineKeyboardButton[]>();
                foreach (var tokenBalance in balances.OrderBy(p => p.Token.Identifier))
                {
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            $"{tokenBalance.Token.Identifier} (Available: {tokenBalance.Amount.ToCurrencyString()})",
                            $"{PlaceOfferTokenQueryPrefix}{tokenBalance.Token.Identifier}")
                    });
                }

                await client.SendTextMessageAsync(
                    chatId,
                    message,
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: ct);

                return WorkflowResult.Handled();
            }

            if (temporaryOffer.Amount == null)
            {
                var tokenBalanceOfChosenToken =
                    balances.FirstOrDefault(p => p.Token.Identifier == temporaryOffer.Token.Identifier);
                if (tokenBalanceOfChosenToken == null)
                {
                    // this should never happen
                    throw new InvalidDataException(
                        $"Token was not found {temporaryOffer.Token.Identifier} in wallet balance.");
                }

                var message = $"You're trying to create an offer on the {elrondUser.Network}.\n" +
                              $"You chose the token {temporaryOffer.Token.Identifier} (You have {tokenBalanceOfChosenToken.Amount.ToCurrencyString()} in you wallet).\n\n" +
                              $"How many {temporaryOffer.Token.Identifier} would you like to offer?";

                await client.SendTextMessageAsync(
                    chatId,
                    message,
                    ParseMode.Html,
                    cancellationToken: ct);

                return WorkflowResult.Handled(UserContext.EnterOfferAmount);
            }

            if (temporaryOffer.Description == null)
            {
                var message = $"You're trying to create an offer for the {elrondUser.Network}.\n" +
                              $"You chose to offer {temporaryOffer.Amount.ToCurrencyStringWithIdentifier()}.\n\n" +
                              "Please choose a description now, which can help other users to have an idea of what you would like to get out of the trade:";

                await client.SendTextMessageAsync(
                    chatId,
                    message,
                    cancellationToken: ct);

                return WorkflowResult.Handled(UserContext.EnterOfferDescription);
            }

            var summary = "<b><u>Summary of your offer:</u></b>\n\n" +
                          $"<b>Network:</b> {elrondUser.Network}\n" +
                          $"<b>Address:</b> {elrondUser.ShortedAddress}\n" +
                          $"<b>Offer:</b> {temporaryOffer.Amount.ToCurrencyStringWithIdentifier()}\n" +
                          $"<b>Description:</b> {temporaryOffer.Description}\n\n" +
                          "Do you want to place the offer now?";

            await client.SendTextMessageAsync(
                chatId,
                summary,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Place offer", PlaceOfferQuery),
                    InlineKeyboardButton.WithCallbackData("Cancel", CommonQueries.BackToHomeQuery)
                }),
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            return WorkflowResult.Handled();
        }
    }
}
