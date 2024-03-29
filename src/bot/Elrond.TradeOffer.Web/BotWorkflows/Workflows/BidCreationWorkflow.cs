﻿using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
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
    public class BidCreationWorkflow : IBotProcessor
    {
        private const string PlaceBidTokenQueryPrefix = "CreateBidToken_";
        private const string PlaceBidQuery = "PlaceBidFinalize";

        private readonly IUserRepository _userManager;
        private readonly IUserContextManager _userContextManager;
        private readonly IOfferRepository _offerRepository;
        private readonly ITemporaryBidManager _temporaryBidManager;
        private readonly IElrondApiService _elrondApiService;
        private readonly IOfferListNavigation _offerListNavigation;
        private readonly IOfferDetailNavigation _offerDetailNavigation;
        private readonly IBotNotificationsHelper _botNotificationsHelper;
        private readonly INetworkStrategies _networkStrategies;
        private readonly IStartMenuNavigation _startMenuNavigation;

        public BidCreationWorkflow(
            IUserRepository userManager,
            IUserContextManager userContextManager,
            IOfferRepository offerRepository,
            ITemporaryBidManager temporaryBidManager,
            IElrondApiService elrondApiService,
            IOfferListNavigation offerListNavigation,
            IOfferDetailNavigation offerDetailNavigation,
            IBotNotificationsHelper botNotificationsHelper,
            INetworkStrategies networkStrategies,
            IStartMenuNavigation startMenuNavigation)
        {
            _userManager = userManager;
            _userContextManager = userContextManager;
            _offerRepository = offerRepository;
            _temporaryBidManager = temporaryBidManager;
            _elrondApiService = elrondApiService;
            _offerListNavigation = offerListNavigation;
            _offerDetailNavigation = offerDetailNavigation;
            _botNotificationsHelper = botNotificationsHelper;
            _networkStrategies = networkStrategies;
            _startMenuNavigation = startMenuNavigation;
            _startMenuNavigation = startMenuNavigation;
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

            if (query.Data.StartsWith(CommonQueries.PlaceBidQueryPrefix))
            {
                var offerIdRaw = query.Data[CommonQueries.PlaceBidQueryPrefix.Length..];
                if (!Guid.TryParse(offerIdRaw, out var offerId))
                {
                    await client.SendTextMessageAsync(chatId, "Invalid offer id.", cancellationToken: ct);
                    await _startMenuNavigation.ShowStartMenuAsync(client, userId, chatId, ct);
                    return WorkflowResult.Unhandled();
                }

                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                return await CreateANewBidAsync(client, userId, chatId, offerId, ct);
            }

            if (query.Data.StartsWith(PlaceBidTokenQueryPrefix))
            {
                var tokenIdentifier = query.Data[PlaceBidTokenQueryPrefix.Length..];
                return await CreateBidOnTokenChosenAsync(client, userId, chatId, tokenIdentifier, ct);
            }

            if (query.Data == PlaceBidQuery)
            {
                await PlaceBidAsync(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }

            return WorkflowResult.Unhandled();
        }

        private async Task<WorkflowResult> CreateANewBidAsync(ITelegramBotClient client, long userId, long chatId, Guid offerId, CancellationToken ct)
        {
            _temporaryBidManager.Reset(userId);

            var offer = await _offerRepository.GetAsync(offerId, ct);
            if (offer == null)
            {
                await OfferNotFoundRedirectAsync(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }
            
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "You have set an address before you continue.",
                    cancellationToken: ct);
                await _offerDetailNavigation.ShowOfferAsync(client, userId, chatId, offerId, ct);
                return WorkflowResult.Handled();
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            _temporaryBidManager.SetOfferId(userId, offerId);
            if (offer.AmountWant != null)
            {
                var networkStrategy = _networkStrategies.GetStrategy(elrondUser.Network);
                
                var balanceForDesiredToken =
                    balances.FirstOrDefault(p => p.Token.Identifier == offer.AmountWant.Token.Identifier);
                if (balanceForDesiredToken == null ||
                    balanceForDesiredToken.Amount.Value < offer.AmountWant.Value)
                {
                    var myBalance = 
                        balanceForDesiredToken?.Amount.ToHtmlUrl(networkStrategy) ?? 
                        TokenAmount.From(0m, offer.AmountWant.Token).ToHtmlUrl(networkStrategy);

                    await client.SendTextMessageAsync(
                        chatId,
                        $"You don't have enough {offer.AmountWant.Token.ToHtmlLink(networkStrategy)}.\n" +
                        $"You have {myBalance}.\n" +
                        $"The creator of the offer wants to have at least {offer.AmountWant.ToHtmlUrl(networkStrategy)}.",
                        ParseMode.Html,
                        cancellationToken: ct);
                    await _offerDetailNavigation.ShowOfferAsync(client, userId, chatId, offerId, ct);
                    return WorkflowResult.Handled();
                }
                
                _temporaryBidManager.SetToken(userId, offer.AmountWant.Token);
            }
            
            return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
        }

        private async Task OfferNotFoundRedirectAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            await client.SendTextMessageAsync(
                chatId,
                "Offer not found.",
                cancellationToken: ct);
            await _offerListNavigation.ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
        }

        private async Task<WorkflowResult> CreateBidOnTokenChosenAsync(ITelegramBotClient client, long userId, long chatId,
            string tokenIdentifier, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "You have set an address before you continue.",
                    cancellationToken: ct);
                await _startMenuNavigation.ShowStartMenuAsync(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }

            var temporaryBid = _temporaryBidManager.Get(userId);
            if (temporaryBid.OfferId == null)
            {
                throw new InvalidOperationException("OfferId is NULL.");
            }

            var offer = await _offerRepository.GetAsync(temporaryBid.OfferId.Value, ct);
            if (offer == null)
            {
                await OfferNotFoundRedirectAsync(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();
            var token = balances.FirstOrDefault(p => p.Token.Identifier == tokenIdentifier);
            if (token?.Token == null)
            {
                return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
            }

            _temporaryBidManager.SetToken(userId, token.Token);

            if (token.Token.IsNft() && token.Amount.Value.IsOne)
            {
                _temporaryBidManager.SetTokenAmount(userId, token.Amount);
            }

            return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
        }

        private async Task PlaceBidAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
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

            var temporaryBid = _temporaryBidManager.Get(userId);
            if (temporaryBid.Token == null ||
                temporaryBid.Amount == null ||
                temporaryBid.OfferId == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "Incomplete order. Try again.",
                    cancellationToken: ct);
                await _offerListNavigation.ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
                return;
            }

            var placedSuccessfully = await _offerRepository.PlaceBidAsync(temporaryBid, chatId, ct);

            _temporaryBidManager.Reset(userId);
            
            if (!placedSuccessfully)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "Bid cannot be placed/updated. Try again.",
                    cancellationToken: ct);
                await _offerDetailNavigation.ShowOfferAsync(client, userId, chatId, temporaryBid.OfferId.Value, ct);
                return;
            }

            var offer = await _offerRepository.GetAsync(temporaryBid.OfferId.Value, ct);
            if (offer == null)
            {
                return;
            }

            await _botNotificationsHelper.NotifyOnBidPlacedAsync(client, offer, chatId, temporaryBid.Amount, ct);
            await _offerDetailNavigation.ShowOfferAsync(client, userId, chatId, temporaryBid.OfferId.Value, ct);
        }

        private async Task<WorkflowResult> PlaceBidWizard(ITelegramBotClient client, long userId, long chatId, Offer offer, IReadOnlyCollection<ElrondToken> balances, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            var networkStrategy = _networkStrategies.GetStrategy(elrondUser.Network);
            var temporaryBid = _temporaryBidManager.Get(userId);
            if (temporaryBid.OfferId == null)
            {
                throw new InvalidOperationException("OfferId is NULL.");
            }

            if (temporaryBid.Token == null)
            {
                var message = $"You're trying to place a bid for the offer of {offer.Amount.ToHtmlUrl(networkStrategy)}.\n\n" +
                              "What tokens do you want to bid?";
                var buttons = new List<InlineKeyboardButton[]>();
                foreach (var tokenBalance in balances.OrderBy(p => p.Token.Identifier))
                {
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            $"{tokenBalance.Token} (Available: {tokenBalance.Amount.ToCurrencyString()})",
                            $"{PlaceBidTokenQueryPrefix}{tokenBalance.Token.Identifier}")
                    });
                }

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ShowOfferQuery(offer.Id)), 
                });

                await client.SendTextMessageAsync(
                    chatId,
                    message,
                    ParseMode.Html,
                    disableWebPagePreview: true,
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: ct);
                return WorkflowResult.Handled();
            }

            if (temporaryBid.Amount == null)
            {
                var tokenBalanceOfChosenToken = balances.FirstOrDefault(p => p.Token.Identifier == temporaryBid.Token.Identifier);
                if (tokenBalanceOfChosenToken == null)
                {
                    throw new InvalidDataException(
                        $"Token was not found {temporaryBid.Token.Identifier} in wallet balance.");
                }

                var message = $"You're trying to place a bid for the offer of {offer.Amount.ToHtmlUrl(networkStrategy)}.\n";
                if (offer.AmountWant == null)
                {
                    message +=
                        $"You chose the token {temporaryBid.Token.ToHtmlLink(networkStrategy)} (You have {tokenBalanceOfChosenToken.Amount.ToCurrencyString()}).\n\n" +
                        "Please choose a token amount for your bid:";
                }
                else
                {
                    var wantedTokenAmount = offer.AmountWant.ToHtmlUrl(networkStrategy);
                    message += $"The creator of this offer wants to have at least {wantedTokenAmount}.\n\n";
                    message +=
                        $"Please enter a token amount of at least {wantedTokenAmount} that you wanna bid on this offer:";
                }

                var sentMessage = await client.SendTextMessageAsync(
                    chatId,
                    message,
                    ParseMode.Html,
                    disableWebPagePreview: true,
                    cancellationToken: ct);

                return WorkflowResult.Handled(UserContext.EnterBidAmount, sentMessage.MessageId);
            }
            else
            {

                var message = $"<b><u>Summary of your bid for the offer of {offer.Amount.ToHtmlUrl(networkStrategy)}:</u></b>\n\n" +
                              $"<b>Network:</b> {elrondUser.Network}\n" +
                              $"<b>Address:</b> {elrondUser.ShortedAddress}\n" +
                              $"<b>Your bid:</b> {temporaryBid.Amount.ToHtmlUrl(networkStrategy)}\n\n" +
                              "Do you want to place the bid now?";

                await client.SendTextMessageAsync(
                    chatId,
                    message,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Place bid", PlaceBidQuery),
                        InlineKeyboardButton.WithCallbackData("Cancel", CommonQueries.ViewOffersQuery)
                    }),
                    parseMode: ParseMode.Html,
                    disableWebPagePreview: true,
                    cancellationToken: ct);

                return WorkflowResult.Handled();
            }
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
            var (context, oldMessageId, _) = _userContextManager.Get(message.From.Id);
            if (context == UserContext.EnterBidAmount)
            {
                await client.TryDeleteMessageAsync(chatId, oldMessageId, ct);
                return await CreateBidOnTokenAmountChosenAsync(client, message.From.Id, chatId, messageText, ct);
            }

            return WorkflowResult.Unhandled();
        }

        private async Task<WorkflowResult> CreateBidOnTokenAmountChosenAsync(ITelegramBotClient client, long userId, long chatId, string? tokenAmount, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            if (elrondUser.Address == null)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "You have set an address before you continue.",
                    cancellationToken: ct);
                await _startMenuNavigation.ShowStartMenuAsync(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }

            var temporaryBid = _temporaryBidManager.Get(userId);
            if (temporaryBid.OfferId == null)
            {
                throw new InvalidOperationException("OfferId is NULL.");
            }

            var offer = await _offerRepository.GetAsync(temporaryBid.OfferId.Value, ct);
            if (offer == null)
            {
                await OfferNotFoundRedirectAsync(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }

            var balances = (await _elrondApiService.GetBalancesAsync(elrondUser.Address, elrondUser.Network)).ToArray();

            var regexAmount = new Regex("^[0-9]+(\\.[0-9]+)?$");
            if (tokenAmount == null || !regexAmount.IsMatch(tokenAmount))
            {
                return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
            }

            var temporaryOffer = _temporaryBidManager.Get(userId);
            if (temporaryOffer.Token == null)
            {
                return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
            }

            var tokenBalance = balances.FirstOrDefault(p => p.Token.Identifier == temporaryOffer.Token.Identifier);
            if (tokenBalance == null)
            {
                return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
            }

            var tokenAmountDecimal = decimal.Parse(tokenAmount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            var amountChosen = TokenAmount.From(tokenAmountDecimal, tokenBalance.Token);
            if (amountChosen.Value > tokenBalance.Amount.Value)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    $"The amount {amountChosen.ToCurrencyString()} is higher than the avaliable amount {tokenBalance.Amount.ToCurrencyString()}.",
                    cancellationToken: ct);
                return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
            }

            if (amountChosen.Value <= BigInteger.Zero)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    $"The amount {amountChosen.ToCurrencyString()} must be bigger than 0.",
                    cancellationToken: ct);
                return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
            }

            if (offer.AmountWant != null &&
                amountChosen.Value < offer.AmountWant.Value)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    $"The amount {amountChosen.ToCurrencyString()} is smaller than the desired amount of {offer.AmountWant.ToCurrencyString()}.",
                    cancellationToken: ct);
                return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
            }

            _temporaryBidManager.SetTokenAmount(userId, amountChosen);
            return await PlaceBidWizard(client, userId, chatId, offer, balances, ct);
        }
    }
}
