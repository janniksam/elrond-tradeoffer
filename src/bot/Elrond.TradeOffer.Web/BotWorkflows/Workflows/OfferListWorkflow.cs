using System.Text.RegularExpressions;
using Elrond.TradeOffer.Web.BotWorkflows.Bids;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
using Elrond.TradeOffer.Web.BotWorkflows.UserState;
using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Extensions;
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
    public class OfferListWorkflow : IBotProcessor, IOfferNavigation
    {
        private const string InitiateTradeOfferQueryPrefix = "Initiate_";
        private const string CancelOfferQueryPrefix = "CancelOffer_";
        private const string AcceptBidQueryPrefix = "ABid_";
        private const string DeclineBidQueryPrefix = "DBid_";
        private const string RemoveBidQueryPrefix = "RemoveBid_";
        private const string RefreshInitiateStatusQueryPrefix = "RefreshInitiateStatus_";
        private const string SearchOfferQuery = "SearchOffers";
        private const string ShowMyOfferQuery = "ShowMyOffers";
        private const int MaxOffersPerPage = 10;

        private readonly IUserRepository _userManager;
        private readonly IOfferRepository _offerRepository;
        private readonly ITransactionGenerator _transactionGenerator;
        private readonly IElrondApiService _elrondApiService;
        private readonly IBotNotificationsHelper _botNotificationsHelper;
        private readonly INetworkStrategies _networkStrategies;
        private readonly IStartMenuNavigation _startMenuNavigation;
        private readonly IUserContextManager _userContextManager;

        public OfferListWorkflow(
            IUserRepository userManager, 
            IOfferRepository offerRepository,
            ITransactionGenerator transactionGenerator,
            IElrondApiService elrondApiService,
            IBotNotificationsHelper botNotificationsHelper,
            INetworkStrategies networkStrategies,
            IUserContextManager userContextManager,
            IStartMenuNavigation startMenuNavigation)
        {
            _userManager = userManager;
            _offerRepository = offerRepository;
            _transactionGenerator = transactionGenerator;
            _elrondApiService = elrondApiService;
            _botNotificationsHelper = botNotificationsHelper;
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
            
            if (query.Data.StartsWith(CommonQueries.ShowOfferQueryPrefix))
            {
                var offerIdRaw = query.Data[CommonQueries.ShowOfferQueryPrefix.Length..];
                if (!Guid.TryParse(offerIdRaw, out var offerId))
                {
                    return await InvalidOfferIdAsync(client, chatId, ct);
                }

                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await ShowOfferAsync(client, userId, chatId, offerId, ct);
                return WorkflowResult.Handled();
            }

            if (query.Data.StartsWith(CancelOfferQueryPrefix))
            {
                var offerIdRaw = query.Data[CancelOfferQueryPrefix.Length..];
                if (!Guid.TryParse(offerIdRaw, out var offerId))
                {
                    return await InvalidOfferIdAsync(client, chatId, ct);
                }

                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await CancelOfferAsync(client, userId, chatId, offerId, ct);
                return WorkflowResult.Handled();
            }

            if (query.Data.StartsWith(RemoveBidQueryPrefix))
            {
                var offerIdRaw = query.Data[RemoveBidQueryPrefix.Length..];
                if (!Guid.TryParse(offerIdRaw, out var offerId))
                {
                    return await InvalidOfferIdAsync(client, chatId, ct);
                }

                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await RemoveBidAsync(client, userId, chatId, offerId, ct);
                return WorkflowResult.Handled();
            }

            if (query.Data.StartsWith(AcceptBidQueryPrefix))
            {
                var parametersRaw = query.Data[AcceptBidQueryPrefix.Length..].Split('_');
                if (parametersRaw.Length != 2)
                {
                    await client.SendTextMessageAsync(chatId, "Invalid parameters for AcceptBid", cancellationToken: ct);
                    return WorkflowResult.Handled();
                }

                if (!Guid.TryParse(parametersRaw[0], out var offerId))
                {
                    return await InvalidOfferIdAsync(client, chatId, ct);
                }

                if (!long.TryParse(parametersRaw[1], out var bidUserId))
                {
                    await client.SendTextMessageAsync(chatId, "Invalid bidUserId.", cancellationToken: ct);
                    return WorkflowResult.Handled();
                }

                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await AcceptBidAsync(client, userId, chatId, offerId, bidUserId, ct);
                return WorkflowResult.Handled();
            }

            if (query.Data.StartsWith(DeclineBidQueryPrefix))
            {
                var parametersRaw = query.Data[DeclineBidQueryPrefix.Length..].Split('_');
                if (parametersRaw.Length != 2)
                {
                    await client.SendTextMessageAsync(chatId, "Invalid parameters for AcceptBid", cancellationToken: ct);
                    return WorkflowResult.Handled();
                }

                if (!Guid.TryParse(parametersRaw[0], out var offerId))
                {
                    return await InvalidOfferIdAsync(client, chatId, ct);
                }

                if (!long.TryParse(parametersRaw[1], out var bidUserId))
                {
                    await client.SendTextMessageAsync(chatId, "Invalid bidUserId.", cancellationToken: ct);
                    return WorkflowResult.Handled();
                }

                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await DeclineBidAsync(client, userId, chatId, offerId, bidUserId, ct);
                return WorkflowResult.Handled();
            }

            if (query.Data.StartsWith(InitiateTradeOfferQueryPrefix))
            {
                var offerIdRaw = query.Data[InitiateTradeOfferQueryPrefix.Length..];
                if (!Guid.TryParse(offerIdRaw, out var offerId))
                {
                    return await InvalidOfferIdAsync(client, chatId, ct);
                }

                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await InitiateTradeAsync(client, userId, chatId, offerId, ct);
                return WorkflowResult.Handled();
            }

            if (query.Data.StartsWith(RefreshInitiateStatusQueryPrefix))
            {
                var offerIdRaw = query.Data[RefreshInitiateStatusQueryPrefix.Length..];
                if (!Guid.TryParse(offerIdRaw, out var offerId))
                {
                    return await InvalidOfferIdAsync(client, chatId, ct);
                }

                await client.TryDeleteMessageAsync(chatId, previousMessageId, ct);
                await RefreshInitiateStatusAsync(client, userId, chatId, offerId, ct);
                return WorkflowResult.Handled();
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
            var context = _userContextManager.Get(message.From.Id);
            if (context.Context == UserContext.EnterOfferSearchTerm)
            {
                await client.TryDeleteMessageAsync(chatId, context.OldMessageId, ct);
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

        private async Task RefreshInitiateStatusAsync(ITelegramBotClient client, long userId, long chatId, Guid offerId, CancellationToken ct)
        {
            var offer = await _offerRepository.GetAsync(offerId, ct);
            if (offer == null)
            {
                await NoOfferFoundAsync(client, userId, chatId, ct);
                return;
            }

            var acceptedBid = await GetTradeAsync(offerId, ct);
            if (acceptedBid == null)
            {
                await NoAcceptedBidAsync(client, userId, chatId, ct);
                return;
            }

            if (acceptedBid.State != BidState.TradeInitiated)
            {
                await ShowOfferAsync(client, userId, chatId, offerId, ct);
                return;
            }

            var foundInSc = await _elrondApiService.IsOfferInSmartContractAsync(offer.Network, offerId);
            if (foundInSc)
            {
                await _offerRepository.UpdateBidAsync(
                    acceptedBid.OfferId,
                    acceptedBid.CreatorUserId,
                    b =>
                    {
                        b.State = BidState.ReadyForClaiming;
                        return true;
                    },
                    ct);

                await _botNotificationsHelper.NotifyOnOfferSendToBlockchainAsync(client, offer, acceptedBid, ct);
            }

            await ShowOfferAsync(client, userId, chatId, offerId, ct);
        }

        private async Task<Bid?> GetTradeAsync(Guid offerId, CancellationToken ct)
        {
            return (await _offerRepository.GetBidsAsync(offerId, 
                    p => p.State == BidState.Accepted || p.State == BidState.TradeInitiated || p.State == BidState.ReadyForClaiming, ct))
                .SingleOrDefault();
        }

        private async Task InitiateTradeAsync(ITelegramBotClient client, long userId, long chatId, Guid offerId, CancellationToken ct)
        {
            var offer = await _offerRepository.GetAsync(offerId, ct);
            if (offer == null)
            {
                await NoOfferFoundAsync(client, userId, chatId, ct);
                return;
            }

            var acceptedBid = await GetTradeAsync(offerId, ct);
            if (acceptedBid == null)
            {
                await NoAcceptedBidAsync(client, userId, chatId, ct);
                return;
            }

            await _offerRepository.UpdateBidAsync(
                acceptedBid.OfferId,
                acceptedBid.CreatorUserId,
                b =>
                {
                    b.State = BidState.TradeInitiated;
                    return true;
                },
                ct);

            await ShowOfferAsync(client, userId, chatId, offerId, ct);
        }

        private async Task CancelOfferAsync(ITelegramBotClient client, long userId, long chatId, Guid offerId, CancellationToken ct)
        {
            var offer = await _offerRepository.GetAsync(offerId, ct);
            if (offer == null)
            {
                await NoOfferFoundAsync(client, userId, chatId, ct);
                return;
            }

            var acceptedBid = await _offerRepository.GetPendingBidAsync(offerId, ct);
            
            var result = await _offerRepository.CancelAsync(offerId, userId, ct);
            if (result == CancelOfferResult.OfferNotFound)
            {
                await NoOfferFoundAsync(client, userId, chatId, ct);
                return;
            }

            if (result == CancelOfferResult.InvalidUser)
            {
                await client.SendTextMessageAsync(chatId, "Wrong user.", cancellationToken: ct);
                await ShowOfferAsync(client, userId, chatId, offerId, ct);
                return;
            }

            if (acceptedBid != null)
            {
                await ApiExceptionHelper.RunAndSupressAsync(
                    async () =>
                    {
                        await client.SendTextMessageAsync(
                            acceptedBid.CreatorChatId,
                            $"The creator of the {offer.Amount.ToCurrencyStringWithIdentifier()} offer has changed their mind and cancelled the offer.\n\n" +
                            "Sorry, hopefully it will work out for you next time!",
                            cancellationToken: ct);
                    });
            }

            if (result == CancelOfferResult.CreatorNeedsToRetrieveTokens)
            {
                await ShowOfferAsync(client, userId, chatId, offerId, ct);
                return;
            }

            await client.SendTextMessageAsync(chatId,
                "Your offer was cancelled successfully.",
                cancellationToken: ct);
            await ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
        }

        public async Task ShowOfferAsync(ITelegramBotClient client, long userId, long chatId, Guid offerId, CancellationToken ct)
        {
            var offer = await _offerRepository.GetAsync(offerId, ct);
            if (offer == null)
            {
                await NoOfferFoundAsync(client, userId, chatId, ct);
                return;
            }

            var strategy = _networkStrategies.GetStrategy(offer.Network);

            var message = $"Details for offer {offer.Amount.ToHtmlUrl(strategy)}\n\n" +
                          $"Description: {offer.Description}\n\n";

            var offerBids = await _offerRepository.GetBidsAsync(offerId, ct);

            if (offer.CreatorUserId == userId)
            {
                var acceptedOrPendingBid = offerBids.FirstOrDefault(
                    p => p.State is BidState.Accepted or BidState.TradeInitiated or BidState.ReadyForClaiming or BidState.CancelInitiated);

                if (acceptedOrPendingBid != null)
                {
                    if (acceptedOrPendingBid.State == BidState.Accepted)
                    {
                        await BidAcceptedResponseCreatorAsync(client, chatId, message, acceptedOrPendingBid, offer, ct);
                        return;
                    }
                    
                    if (acceptedOrPendingBid.State == BidState.TradeInitiated)
                    {
                        await AcceptedAndReadyForSendResponseAsync(client, message, chatId, acceptedOrPendingBid, offer, ct);
                        return;
                    }

                    if (acceptedOrPendingBid.State == BidState.CancelInitiated)
                    {
                        await ReclaimResponseAsync(client, chatId, message, offer, ct);
                        return;
                    }

                    if (acceptedOrPendingBid.State is BidState.Removed or BidState.RemovedWhileOnBlockchain)
                    {
                        await BidRemovedResponseAsync(client, message, chatId, offer, ct);
                        return;
                    }

                    await AcceptedAndSendToScResponseAsync(client, message, chatId, acceptedOrPendingBid, offer, ct);
                    return;
                }

                var createdBids = offerBids
                    .Where(p => p.State is BidState.Created)
                    .OrderByDescending(p => p.CreatedOn)
                    .Take(3).ToArray();
                if (createdBids.Length > 0)
                {
                    await BidReceivedResponseAsync(client, message, chatId, createdBids, offer, ct);
                    return;
                }

                await NoBidsResponseCreatorAsync(client, message, chatId, offer, ct);
                return;
            }

            var myBid = offerBids.FirstOrDefault(p => p.CreatorUserId == userId);
            if(myBid == null)
            {
                await NoBidsResponseBidderAsync(client, message, chatId, offer, ct);
                return;
            }

            message += $"You bid {myBid.Amount.ToCurrencyStringWithIdentifier()}.\n";
            if (myBid.State == BidState.Created)
            {
                await BidCreatedResponseAsync(client, chatId, message, offer, ct);
                return;
            }

            if (myBid.State is BidState.Accepted or BidState.TradeInitiated)
            {
                await BidAcceptedResponseBidderAsync(client, message, chatId, offer, ct);
                return;
            }

            if(myBid.State == BidState.ReadyForClaiming)
            {
                await FinalizeTradeResponseAsync(client, chatId, offer, myBid, message, ct);
                return;
            }

            if (myBid.State == BidState.Declined)
            {
                await BidDeclinedResponseAsync(client, chatId, message, offer, ct);
            }
        }

        private async Task ReclaimResponseAsync(ITelegramBotClient client, long chatId, string message, Offer offer, CancellationToken ct) 
        {
            message +=
                "Cancellation of the order was requested.\n\n" +
                "Since you already sent your tokens to the smart contract, you can reclaim them now:";

            var reclaimUrl = await _transactionGenerator.GenerateReclaimUrlAsync(offer);
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("Reclaim tokens", reclaimUrl),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(chatId, message, ParseMode.Html,
                disableWebPagePreview: true,
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static async Task BidAcceptedResponseCreatorAsync(ITelegramBotClient client, long chatId, string message,
            Bid acceptedOrPendingBid, Offer offer, CancellationToken ct)
        {
            message +=
                $"You have accepted an offer: {acceptedOrPendingBid.Amount.ToCurrencyStringWithIdentifier()}\n" +
                "If you wish to proceed, you need to initiate the transaction and then transfer your tokens to the smart contract.";
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🚀 Initiate trade", $"{InitiateTradeOfferQueryPrefix}{offer.Id}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Cancel offer", $"{CancelOfferQueryPrefix}{offer.Id}"),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(
                chatId,
                message,
                ParseMode.Html,
                disableWebPagePreview: true,
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private async Task AcceptedAndReadyForSendResponseAsync(ITelegramBotClient client, string message, long chatId, Bid acceptedOrPendingBid, Offer offer, CancellationToken ct)
        {
            message +=
                "You have initiated the trade.\n" +
                $"You need to send your {offer.Amount.ToCurrencyStringWithIdentifier()} to the smart contract now.\n\n" +
                "You have already sent the tokens? Press \"Refresh transaction status\" to check for a new status.";
            var sendOfferTokensUrl = await _transactionGenerator.GenerateInitiateTradeUrlAsync(offer, acceptedOrPendingBid);
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl($"✉ Send {offer.Amount.ToCurrencyStringWithIdentifier()}", sendOfferTokensUrl),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Refresh transaction status",
                        $"{RefreshInitiateStatusQueryPrefix}{offer.Id}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Cancel offer", $"{CancelOfferQueryPrefix}{offer.Id}"),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(chatId, message, ParseMode.Html,
                disableWebPagePreview: true,
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static async Task BidRemovedResponseAsync(ITelegramBotClient client, string message, long chatId, Offer offer, CancellationToken ct)
        {
            //todo Improve logic here
            message +=
                "The bid you received has been removed.\n\n";

            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Cancel offer", $"{CancelOfferQueryPrefix}{offer.Id}"),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(chatId, message, ParseMode.Html,
                disableWebPagePreview: true, 
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static async Task AcceptedAndSendToScResponseAsync(ITelegramBotClient client, string message, long chatId,
            Bid acceptedOrPendingBid, Offer offer, CancellationToken ct)
        {
            message +=
                $"You have accepted the bid for your {acceptedOrPendingBid.Amount.ToCurrencyStringWithIdentifier()} and sent your tokens to the smart contract.\n\n" +
                "Please wait for the other party to finalize the exchange or cancel your offer to get back your tokens.";

            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Cancel offer", $"{CancelOfferQueryPrefix}{offer.Id}"),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(chatId, message, ParseMode.Html,
                disableWebPagePreview: true, 
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static async Task BidReceivedResponseAsync(ITelegramBotClient client, string message, long chatId, IReadOnlyCollection<Bid> createdBids, Offer offer, CancellationToken ct)
        {
            message +=
                "You received the following bids. You can either accept or decline them.";
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var createdBid in createdBids)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData($"✅ Accept: {createdBid.Amount.ToCurrencyStringWithIdentifier()}",
                        $"{AcceptBidQueryPrefix}{offer.Id}_{createdBid.CreatorUserId}"),
                    InlineKeyboardButton.WithCallbackData($"❌ Decline: {createdBid.Amount.ToCurrencyStringWithIdentifier()}",
                        $"{DeclineBidQueryPrefix}{offer.Id}_{createdBid.CreatorUserId}"),
                });
            }

            buttons.Add(
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Cancel offer", $"{CancelOfferQueryPrefix}{offer.Id}"),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                });

            await client.SendTextMessageAsync(chatId, message, ParseMode.Html,
                disableWebPagePreview: true, 
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static async Task NoBidsResponseCreatorAsync(ITelegramBotClient client, string message, long chatId, Offer offer, CancellationToken ct)
        {
            message += "Currently there are no bids.";
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Cancel offer", $"{CancelOfferQueryPrefix}{offer.Id}"),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(chatId, message, ParseMode.Html,
                disableWebPagePreview: true, 
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static async Task NoBidsResponseBidderAsync(ITelegramBotClient client, string message, long chatId, Offer offer, CancellationToken ct)
        {
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Place bid", $"{CommonQueries.PlaceBidQueryPrefix}{offer.Id}"),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(chatId, message, ParseMode.Html,
                disableWebPagePreview: true, 
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static async Task BidCreatedResponseAsync(ITelegramBotClient client, long chatId, string baseMessage, Offer offer, CancellationToken ct)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            baseMessage +=
                "Your bid has neither been accepted nor declined yet.\nPlease wait for the creator of this offer to accept/decline your bid.";
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("✂ Update bid", $"{CommonQueries.PlaceBidQueryPrefix}{offer.Id}"),
                InlineKeyboardButton.WithCallbackData("✖ Remove bid", $"{RemoveBidQueryPrefix}{offer.Id}"),
            });
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
            });

            await client.SendTextMessageAsync(chatId, baseMessage, ParseMode.Html,
                disableWebPagePreview: true,
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static async Task BidAcceptedResponseBidderAsync(ITelegramBotClient client, string baseMessage, long chatId, Offer offer, CancellationToken ct)
        {
            baseMessage += "Your bid was accepted. Please wait for the offer creator to initiate the exchange now.";
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✖ Remove bid", $"{RemoveBidQueryPrefix}{offer.Id}"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(chatId, baseMessage, ParseMode.Html,
                disableWebPagePreview: true,
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static async Task BidDeclinedResponseAsync(ITelegramBotClient client, long chatId, string message, Offer offer, CancellationToken ct)
        {
            message += "Your bid was declined. Place a new one.";
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Place new bid", $"{CommonQueries.PlaceBidQueryPrefix}{offer.Id}"),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(chatId, message, ParseMode.Html,
                disableWebPagePreview: true, 
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private async Task FinalizeTradeResponseAsync(
            ITelegramBotClient client, long chatId, Offer offer, Bid myBid, string message, CancellationToken ct)
        {
            var finalizeTradeUrl = await _transactionGenerator.GenerateFinalizeTradeUrlAsync(offer, myBid);
            message +=
                "Your bid was accepted and the created of the offer transfered the tokens to the smart contract.\n\nYou can finalize the trade now.";
            
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("🚀 Finalize trade", finalizeTradeUrl),
                    InlineKeyboardButton.WithCallbackData("Back", CommonQueries.ViewOffersQuery)
                }
            };

            await client.SendTextMessageAsync(
                chatId,
                message,
                ParseMode.Html,
                disableWebPagePreview: true,
                disableNotification: true,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
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

        private async Task RemoveBidAsync(ITelegramBotClient client, long userId, long chatId, Guid offerId, CancellationToken ct)
        {
            var success = await _offerRepository.RemoveBidAsync(offerId, userId, ct);
            if (success)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "Your bid was removed successfully.",
                    cancellationToken: ct);
                await ShowOfferAsync(client, userId, chatId, offerId, ct);
                return;
            }

            await client.SendTextMessageAsync(
                chatId,
                "Could not remove your bid.",
                cancellationToken: ct);
            await ShowOfferAsync(client, userId, chatId, offerId, ct);
        }

        private async Task AcceptBidAsync(ITelegramBotClient client, long userId, long chatId, Guid offerId, long bidUserId, CancellationToken ct)
        {
            var offer = await _offerRepository.GetAsync(offerId, ct);
            if (offer == null)
            {
                await client.SendTextMessageAsync(chatId, "Offer was not found.", cancellationToken: ct);
                await ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
                return;
            }

            var bid = await _offerRepository.GetBidAsync(offerId, bidUserId, ct);
            if (bid == null)
            {
                await client.SendTextMessageAsync(chatId, "Bid was not found.", cancellationToken: ct);
                await ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
                return;
            }

            var (success, declinedBids) = await _offerRepository.AcceptAsync(offerId, bidUserId, ct);
            if (!success)
            {
                await client.SendTextMessageAsync(chatId, $"Could not accept the bid {bid.Amount.ToCurrencyStringWithIdentifier()}.", cancellationToken: ct);
            }
            else
            {
                await _botNotificationsHelper.NotifyOnBidAccepted(client, chatId, offer, bid, declinedBids, ct);
            }

            await ShowOfferAsync(client, userId, chatId, offerId, ct);
        }

        private async Task DeclineBidAsync(ITelegramBotClient client, long userId, long chatId, Guid offerId, long bidUserId, CancellationToken ct)
        {
            var bid = await _offerRepository.GetBidAsync(offerId, bidUserId, ct);
            if (bid == null)
            {
                await client.SendTextMessageAsync(chatId, "Bid was not found.", cancellationToken: ct);
                await ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
                return;
            }

            var success = await _offerRepository.UpdateBidAsync(offerId, bidUserId, b =>
            {
                if (b.State is not (BidState.Created or BidState.Accepted or BidState.Declined))
                {
                    return false;
                }

                b.State = BidState.Declined;
                return true;
            }, ct);

            if (!success)
            {
                await client.SendTextMessageAsync(chatId, $"Could not decline the bid {bid.Amount.ToCurrencyStringWithIdentifier()}.", cancellationToken: ct);
            }
            else
            {
                await _botNotificationsHelper.NotifyOnBidDeclined(client, chatId, bid, ct);
            }

            await ShowOfferAsync(client, userId, chatId, offerId, ct);
        }

        private async Task NoOfferFoundAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            await client.SendTextMessageAsync(chatId, "Offer was not found.", cancellationToken: ct);
            await ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
        }

        private async Task NoAcceptedBidAsync(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            await client.SendTextMessageAsync(chatId, "Offer has no accepted bid.", cancellationToken: ct);
            await ShowOffersAsync(client, userId, chatId, OfferFilter.None(), ct);
        }

        private static async Task<WorkflowResult> InvalidOfferIdAsync(ITelegramBotClient client, long chatId, CancellationToken ct)
        {
            await client.SendTextMessageAsync(chatId, "Invalid offer id.", cancellationToken: ct);
            return WorkflowResult.Handled();
        }
    }
}
