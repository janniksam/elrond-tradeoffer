using Elrond.TradeOffer.Web.BotWorkflows.Bids;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Network;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Utils;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows
{
    public class BotNotificationsHelper : IBotNotificationsHelper
    {
        private readonly INetworkStrategies _networkStrategies;

        public BotNotificationsHelper(INetworkStrategies networkStrategies)
        {
            _networkStrategies = networkStrategies;
        }

        public async Task NotifyOnOfferSendToBlockchainAsync(ITelegramBotClient client, Offer offer, Bid bid, CancellationToken ct)
        {
            var networkStrategy = _networkStrategies.GetStrategy(offer.Network);
            var offeredAmount = offer.Amount.ToHtmlUrl(networkStrategy);
            var bidAmount = bid.Amount.ToHtmlUrl(networkStrategy);
            await ApiExceptionHelper.RunAndSupressAsync(() =>
            {
                return client.SendTextMessageAsync(
                    bid.CreatorChatId,
                    $"The creator of the offer has sent the {offeredAmount} to the smart contract.\n\n" +
                    $"To complete the trade (you bid {bidAmount}) now, please proceed by pressing the button below:",
                    ParseMode.Html,
                    disableWebPagePreview: true,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("View offer and proceed", $"{CommonQueries.ShowOfferQuery(bid.OfferId)}"),
                    }),
                    cancellationToken: ct);
            });

            await ApiExceptionHelper.RunAndSupressAsync(() =>
            {
                return client.SendTextMessageAsync(
                    offer.CreatorChatId,
                    $"You sent {offeredAmount} to the smart contract.\n\n" +
                    $"Please wait for the bidder of {bidAmount} to complete the trade.\n" +
                    "You will be notified, when the bidder has completed the trade.",
                    ParseMode.Html,
                    disableWebPagePreview: true,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("View offer", $"{CommonQueries.ShowOfferQuery(bid.OfferId)}"),
                    }),
                    cancellationToken: ct);
            });
        }

        public async Task NotifyOnTradeCompletedAsync(ITelegramBotClient client, Offer offer, Bid bid, CancellationToken ct)
        {
            var offeredAmount = offer.Amount.ToCurrencyStringWithIdentifier();
            var bidAmount = bid.Amount.ToCurrencyStringWithIdentifier();
            await ApiExceptionHelper.RunAndSupressAsync(
                () => client.SendTextMessageAsync(
                    bid.CreatorChatId,
                    $"You completed the trade and received your {offeredAmount} from the smart contract.",
                    cancellationToken: ct));

            await ApiExceptionHelper.RunAndSupressAsync(() =>
                client.SendTextMessageAsync(
                    offer.CreatorChatId,
                    $"You trade offer has been filled. {bidAmount} were sent to your wallet, by the smart contract.",
                    cancellationToken: ct));
        }

        public async Task NotifyOnOfferCancelledAsync(ITelegramBotClient client, Offer offer, CancellationToken ct)
        {
            var strategy = _networkStrategies.GetStrategy(offer.Network);
            await ApiExceptionHelper.RunAndSupressAsync(() =>
                client.SendTextMessageAsync(
                    offer.CreatorChatId,
                    $"You trade offer has been cancelled. You claimed back your {offer.Amount.ToHtmlUrl(strategy)}.",
                    ParseMode.Html,
                    disableWebPagePreview: true,
                    cancellationToken: ct));
        }

        public async Task NotifyOnBidAccepted(
            ITelegramBotClient client, 
            long chatId, 
            Offer offer,
            Bid acceptedBid, 
            IEnumerable<Bid> declinedBids,
            CancellationToken ct)
        {
            var strategy = _networkStrategies.GetStrategy(offer.Network);
            var offeredTokens = offer.Amount.ToHtmlUrl(strategy);
            var acceptedTokens = acceptedBid.Amount.ToHtmlUrl(strategy);
     
            await client.SendTextMessageAsync(
                chatId, $"You accepted the bid of {acceptedTokens} for the offer of {offeredTokens} was accepted.",
                ParseMode.Html,
                disableWebPagePreview: true,
                cancellationToken: ct);

            await SendNotificationAsync(
                client,
                acceptedBid.CreatorChatId,
                $"Your bid of {acceptedTokens} for the offer of {offeredTokens} has been accepted.",
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("View offer", $"{CommonQueries.ShowOfferQuery(acceptedBid.OfferId)}"),
                }, ct);

            foreach (var declinedBid in declinedBids)
            {
                await ApiExceptionHelper.RunAndSupressAsync(async () =>
                {
                    await client.SendTextMessageAsync(
                        declinedBid.CreatorChatId,
                        $"Another bid for the offered {offeredTokens} was accepted.\n\n" +
                        "Sorry, I hope you have more luck next time!",
                        ParseMode.Html,
                        disableWebPagePreview: true,
                        cancellationToken: ct);
                });
            }
        }

        public async Task NotifyOnBidDeclined(ITelegramBotClient client, long chatId, Bid bid, CancellationToken ct)
        {
            await client.SendTextMessageAsync(chatId, $"You declined the bid of {bid.Amount.ToCurrencyStringWithIdentifier()}.",
                cancellationToken: ct);

            await SendNotificationAsync(
                client,
                bid.CreatorChatId,
                $"Your bid of {bid.Amount.ToCurrencyStringWithIdentifier()} has been declined.",
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("View offer", $"{CommonQueries.ShowOfferQuery(bid.OfferId)}"),
                }, ct);
        }

        public async Task NotifyOnBidPlacedAsync(ITelegramBotClient client, Offer offer, long bidChatId, TokenAmount bidAmount, CancellationToken ct)
        {
            var strategy = _networkStrategies.GetStrategy(offer.Network);
            var bidTokens = bidAmount.ToHtmlUrl(strategy);
            var offeredTokens = offer.Amount.ToHtmlUrl(strategy);
           
            await client.SendTextMessageAsync(
                bidChatId,
                $"You placed a bid of {bidTokens} for the offer of {offeredTokens}.",
                ParseMode.Html,
                disableWebPagePreview: true,
                cancellationToken: ct);
            
            await SendNotificationAsync(
                client,
                offer.CreatorChatId,
                $"You received a bid ({bidTokens}) for your offer of {offeredTokens}.",
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("View offer", $"{CommonQueries.ShowOfferQuery(offer.Id)}")
                }, ct);
        }

        public async Task NotifyOfferCreatorOnBidRemovedAsync(ITelegramBotClient client, long chatId, Offer offer, RemoveBidResult removeBidResult, CancellationToken ct)
        {
            await client.SendTextMessageAsync(
                chatId,
                "Your bid was removed successfully.",
                cancellationToken: ct);

            switch (removeBidResult)
            {
                case RemoveBidResult.RemovedAccepted:
                    await SendNotificationAsync(
                        client,
                        offer.CreatorChatId,
                        $"The bid you accepted for your offer of {offer.Amount.ToCurrencyStringWithIdentifier()} has been removed.",
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("View offer", $"{CommonQueries.ShowOfferQuery(offer.Id)}"),
                        }, ct);
                    break;
                case RemoveBidResult.RemovedWhileOnBlockchain:
                    await SendNotificationAsync(
                        client,
                        offer.CreatorChatId,
                        $"The bid you accepted for your offer of {offer.Amount.ToCurrencyStringWithIdentifier()} has been removed. Reclaim your tokens now:",
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("View offer", $"{CommonQueries.ShowOfferQuery(offer.Id)}"),
                        }, ct);
                    break;
            }
        }

        private static async Task SendNotificationAsync(ITelegramBotClient client, long chatId, string htmlMessage,
            InlineKeyboardButton[] buttons, CancellationToken ct)
        {
            await ApiExceptionHelper.RunAndSupressAsync(() =>
                client.SendTextMessageAsync(
                    chatId,
                    htmlMessage,
                    ParseMode.Html,
                    disableWebPagePreview: true,
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: ct));
        }
    }
}
