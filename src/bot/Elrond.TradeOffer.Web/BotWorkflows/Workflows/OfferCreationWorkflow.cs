using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.UserState;
using Elrond.TradeOffer.Web.Extensions;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Network;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Services;
using Elrond.TradeOffer.Web.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows
{
    public class OfferCreationWorkflow : IBotProcessor
    {
        private const string PlaceOfferTokenQueryPrefix = "CreateOfferToken_";
        private const string PlaceOfferWantEgldQuery = "CreateOfferWantEgld";
        private const string PlaceOfferWantEsdtQuery = "CreateOfferWantEsdt";
        private const string PlaceOfferWantNothingSpecificQuery = "CreateOfferWantNothingSpecific";
        private const string PlaceOfferQuery = "PlaceOffer";
        private const int MinDescriptionLength = 5;
        private const int MaxDescriptionLength = 50;
        private readonly IUserRepository _userManager;
        private readonly IUserContextManager _userContextManager;
        private readonly ITemporaryOfferManager _temporaryOfferManager;
        private readonly IOfferRepository _offerRepository;
        private readonly IElrondApiService _elrondApiService;
        private readonly INetworkStrategies _networkStrategies;
        private readonly IStartMenuNavigation _startMenuNavigation;

        public OfferCreationWorkflow(
            IUserRepository userManager,
            IUserContextManager userContextManager,
            ITemporaryOfferManager temporaryOfferManager, 
            IOfferRepository offerRepository, 
            IElrondApiService elrondApiService,
            INetworkStrategies networkStrategies,
            IStartMenuNavigation startMenuNavigation)
        {
            _userManager = userManager;
            _userContextManager = userContextManager;
            _temporaryOfferManager = temporaryOfferManager;
            _offerRepository = offerRepository;
            _elrondApiService = elrondApiService;
            _networkStrategies = networkStrategies;
            _startMenuNavigation = startMenuNavigation;
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
            if (context.Context == UserContext.EnterOfferAmount)
            {
                await client.TryDeleteMessageAsync(chatId, context.OldMessageId, ct);
                return await CreateOfferOnTokenAmountChosenAsync(client, message.From.Id, chatId, messageText, ct);
            }

            if (context.Context == UserContext.EnterOfferWantToken)
            {
                await client.TryDeleteMessageAsync(chatId, context.OldMessageId, ct);
                return await CreateOfferOnTokenWantChosenAsync(client, message.From.Id, chatId, messageText, ct);
            }

            if (context.Context == UserContext.EnterOfferWantAmount)
            {
                await client.TryDeleteMessageAsync(chatId, context.OldMessageId, ct);
                return await CreateOfferOnTokenAmountWantChosenAsync(client, message.From.Id, chatId, messageText, ct);
            }

            if (context.Context == UserContext.EnterOfferDescription)
            {
                await client.TryDeleteMessageAsync(chatId, context.OldMessageId, ct);
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
                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                return await CreateANewOfferAsync(client, userId, chatId, ct);
            }

            if (query.Data.StartsWith(PlaceOfferTokenQueryPrefix))
            {
                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                var tokenIdentifier = query.Data[PlaceOfferTokenQueryPrefix.Length..];
                return await CreateOfferOnTokenChosenAsync(client, userId, chatId, tokenIdentifier, ct);
            }

            if (query.Data.Equals(PlaceOfferWantEgldQuery))
            {
                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                return await CreateOfferOnWantEgldChosenAsync(client, userId, chatId, ct);
            }

            if (query.Data.Equals(PlaceOfferWantEsdtQuery))
            {
                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                return await CreateOfferOnWantEsdtChosenAsync(client, userId, chatId, ct);
            }

            if (query.Data.Equals(PlaceOfferWantNothingSpecificQuery))
            {
                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                return await CreateOfferOnWantNothingSpecificChosenAsync(client, userId, chatId, ct);
            }

            if (query.Data == PlaceOfferQuery)
            {
                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await PlaceOfferAsync(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }

            return WorkflowResult.Unhandled();
        }

        private async Task<WorkflowResult> CreateOfferOnWantEsdtChosenAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                return await AddressNotSetAsync(client, userId, chatId, ct);
            }

            _temporaryOfferManager.SetWantSomethingSpecific(userId, true);
            _temporaryOfferManager.SetTokenWant(userId, null);

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }

        private async Task<WorkflowResult> CreateOfferOnWantEgldChosenAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                return await AddressNotSetAsync(client, userId, chatId, ct);
            }

            _temporaryOfferManager.SetWantSomethingSpecific(userId, true);
            _temporaryOfferManager.SetTokenWant(userId, Token.Egld());
            
            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }

        private async Task<WorkflowResult> CreateOfferOnWantNothingSpecificChosenAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                return await AddressNotSetAsync(client, userId, chatId, ct);
            }

            _temporaryOfferManager.SetWantSomethingSpecific(userId, false);
            _temporaryOfferManager.SetTokenWant(userId, null);

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }

        private async Task<WorkflowResult> CreateANewOfferAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            _temporaryOfferManager.Reset(userId);

            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                return await AddressNotSetAsync(client, userId, chatId, ct);
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
                return WorkflowResult.Handled();
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }

        private async Task<WorkflowResult> AddressNotSetAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            await client.SendTextMessageAsync(
                chatId,
                "You have set an address before you continue.",
                cancellationToken: ct);
            await _startMenuNavigation.ShowStartMenuAsync(client, userId, chatId, ct);
            return WorkflowResult.Handled();
        }

        private async Task<WorkflowResult> CreateOfferOnTokenChosenAsync(ITelegramBotClient client, long userId, long chatId, string tokenIdentifier, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                return await AddressNotSetAsync(client, userId, chatId, ct);
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            var token = balances.FirstOrDefault(p => p.Token.Identifier == tokenIdentifier);
            if (token?.Token == null)
            {
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }
            
            _temporaryOfferManager.SetToken(userId, token.Token);
            if (token.Token.IsNft() && token.Amount.Value.IsOne)
            {
                _temporaryOfferManager.SetTokenAmount(userId, token.Amount);
            }

            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }


        private async Task<WorkflowResult> CreateOfferOnTokenWantChosenAsync(ITelegramBotClient client, long userId, long chatId, string? tokenIdentifier, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                return await AddressNotSetAsync(client, userId, chatId, ct);
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            if (string.IsNullOrWhiteSpace(tokenIdentifier))
            {
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            var token = await _elrondApiService.GetTokenAsync(elrondUser.Network, tokenIdentifier);
            if (token == null)
            {
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            _temporaryOfferManager.SetTokenWant(userId, token.Token);
            if (token.Token.IsNft() && token.Amount.Value.IsOne)
            {
                _temporaryOfferManager.SetTokenAmountWant(userId, token.Amount);
                _temporaryOfferManager.SetDescription(userId, "N/A");
            }
            
            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }

        private async Task<WorkflowResult> CreateOfferOnTokenAmountChosenAsync(ITelegramBotClient client, long userId, long chatId, string? tokenAmount, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                return await AddressNotSetAsync(client, userId, chatId, ct);
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

        private async Task<WorkflowResult> CreateOfferOnTokenAmountWantChosenAsync(ITelegramBotClient client, long userId, long chatId, string? tokenAmount, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                return await AddressNotSetAsync(client, userId, chatId, ct);
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            var regexAmount = new Regex("^[0-9]+(\\.[0-9]+)?$");
            if (tokenAmount == null || !regexAmount.IsMatch(tokenAmount))
            {
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            var temporaryOffer = _temporaryOfferManager.Get(userId);
            if (temporaryOffer.TokenWant == null)
            {
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            var tokenAmountDecimal = decimal.Parse(tokenAmount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            var amountChosen = TokenAmount.From(tokenAmountDecimal, temporaryOffer.TokenWant);
            if (amountChosen.Value <= BigInteger.Zero)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    $"The amount {amountChosen.ToCurrencyString()} must be bigger than 0.",
                    cancellationToken: ct);
                return await CreateOfferWizard(client, userId, chatId, balances, ct);
            }

            _temporaryOfferManager.SetTokenAmountWant(userId, amountChosen);
            _temporaryOfferManager.SetDescription(userId, "N/A");
            return await CreateOfferWizard(client, userId, chatId, balances, ct);
        }


        private async Task<WorkflowResult> CreateOfferOnDescriptionChosenAsync(ITelegramBotClient client, long userId, long chatId, string? description, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                return await AddressNotSetAsync(client, userId, chatId, ct);
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            if (string.IsNullOrWhiteSpace(description) || description.Length is < MinDescriptionLength or > MaxDescriptionLength)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    $"Invalid description. The description needs to have a length between {MinDescriptionLength} and {MaxDescriptionLength} characters.",
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
                await _startMenuNavigation.ShowStartMenuAsync(client, userId, chatId, ct);
                return;
            }

            var temporaryOffer = _temporaryOfferManager.Get(userId);
            if (!temporaryOffer.IsFilled)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "Incomplete order. Try again.",
                    cancellationToken: ct);
                await _startMenuNavigation.ShowStartMenuAsync(client, userId, chatId, ct);
                return;
            }

            await _offerRepository.PlaceAsync(elrondUser, temporaryOffer, chatId, ct);
            _temporaryOfferManager.Reset(userId);

            await client.SendTextMessageAsync(
                chatId,
                "The offer has been placed.",
                cancellationToken: ct);
            await _startMenuNavigation.ShowStartMenuAsync(client, userId, chatId, ct);
        }

        private async Task<WorkflowResult> CreateOfferWizard(ITelegramBotClient client, long userId, long chatId,
            IReadOnlyCollection<ElrondToken> balances, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            var networkStrategy = _networkStrategies.GetStrategy(elrondUser.Network);
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
                            $"{tokenBalance.Token} (Available: {tokenBalance.Amount.ToCurrencyString()})",
                            $"{PlaceOfferTokenQueryPrefix}{tokenBalance.Token.Identifier}")
                    });
                }

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.BackToHomeQuery),
                });

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
                              $"You chose the token {temporaryOffer.Token.ToHtmlLink(networkStrategy)} (You have {tokenBalanceOfChosenToken.Amount.ToCurrencyString()} in your wallet).\n\n" +
                              $"How many {temporaryOffer.Token.Identifier} would you like to offer?";

                var sentMessage = await client.SendTextMessageAsync(
                    chatId,
                    message,
                    ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Cancel", CommonQueries.BackToHomeQuery)
                    }),
                    disableWebPagePreview: true,
                    cancellationToken: ct);

                return WorkflowResult.Handled(UserContext.EnterOfferAmount, sentMessage.MessageId);
            }

            if (temporaryOffer.WantSomethingSpecific == null)
            {
                var message = $"You're trying to create an offer for the {elrondUser.Network}.\n" +
                              $"You chose to offer {temporaryOffer.Amount.ToHtmlUrl(networkStrategy)}.\n\n" +
                              "What do you want to have for your offer, something specific?";

                await client.SendTextMessageAsync(
                    chatId,
                    message,
                    ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("EGLD", PlaceOfferWantEgldQuery),
                                InlineKeyboardButton.WithCallbackData("ESDT/NFT", PlaceOfferWantEsdtQuery),
                                InlineKeyboardButton.WithCallbackData("Nothing specific", PlaceOfferWantNothingSpecificQuery),
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Cancel", CommonQueries.BackToHomeQuery)
                            }
                        }),
                    disableWebPagePreview: true,
                    cancellationToken: ct);

                return WorkflowResult.Handled();
            }
            
            if (temporaryOffer.WantSomethingSpecific.Value &&
                temporaryOffer.TokenWant == null)
            {
                var message = $"You're trying to create an offer for the {elrondUser.Network}.\n" +
                              $"You chose to offer {temporaryOffer.Amount.ToHtmlUrl(networkStrategy)}.\n\n" +
                              "Please choose the specific token you want for your offer?\n\n" +
                              "Currently we accept the following format:\n\n" +
                              "- ESDTs: \"RIDE-7d18e9\"\n" +
                              "- NFTs: \"GNOGONS-73222b-0725\"\n\n" +
                              "Please enter a token in the specified format below:";

                var sentMessage = await client.SendTextMessageAsync(
                    chatId,
                    message,
                    ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Cancel", CommonQueries.BackToHomeQuery)
                    }),
                    disableWebPagePreview: true,
                    cancellationToken: ct);

                return WorkflowResult.Handled(UserContext.EnterOfferWantToken, sentMessage.MessageId);
            }

            if (temporaryOffer.WantSomethingSpecific.Value &&
                temporaryOffer.AmountWant == null)
            {
                var wantTokenHtmlLink = temporaryOffer.TokenWant!.ToHtmlLink(networkStrategy);
                var message = $"You're trying to create an offer for the {elrondUser.Network}.\n" +
                              $"You chose to offer {temporaryOffer.Amount.ToHtmlUrl(networkStrategy)}.\n\n" +
                              $"You decided that you want {wantTokenHtmlLink} for your offer.\n\n" +
                              $"Please choose the MINIMUM AMOUNT of {wantTokenHtmlLink} now, that you want for your offer:";

                var sentMessage = await client.SendTextMessageAsync(
                    chatId,
                    message,
                    ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Cancel", CommonQueries.BackToHomeQuery)
                    }),
                    disableWebPagePreview: true,
                    cancellationToken: ct);

                return WorkflowResult.Handled(UserContext.EnterOfferWantAmount, sentMessage.MessageId);
            }

            if (temporaryOffer.Description == null)
            {
                var message = $"You're trying to create an offer for the {elrondUser.Network}.\n" +
                              $"You chose to offer {temporaryOffer.Amount.ToHtmlUrl(networkStrategy)}.\n\n" +
                              "Please choose a description now, which can help other users to have an idea of what you would like to get out of the trade:";

                var sentMessage = await client.SendTextMessageAsync(
                    chatId,
                    message,
                    ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Cancel", CommonQueries.BackToHomeQuery)
                    }),
                    disableWebPagePreview: true,
                    cancellationToken: ct);

                return WorkflowResult.Handled(UserContext.EnterOfferDescription, sentMessage.MessageId);
            }

            var summary = "<b><u>Summary of your offer:</u></b>\n\n" +
                          $"<b>Network:</b> {elrondUser.Network}\n" +
                          $"<b>Address:</b> {elrondUser.ShortedAddress}\n" +
                          $"<b>What do I offer?</b> {temporaryOffer.Amount.ToHtmlUrl(networkStrategy)}\n" +
                          $"<b>What do I want in return?</b> {GetWantedAmountDisplay(temporaryOffer, networkStrategy)}\n" +
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
                disableWebPagePreview: true,
                cancellationToken: ct);

            return WorkflowResult.Handled();
        }

        private static string GetWantedAmountDisplay(TemporaryOffer temporaryOffer, INetworkStrategy networkStrategy)
        {
            return temporaryOffer.AmountWant == null
                ? "Nothing specific"
                : temporaryOffer.AmountWant.ToHtmlUrl(networkStrategy);
        }
    }
}
