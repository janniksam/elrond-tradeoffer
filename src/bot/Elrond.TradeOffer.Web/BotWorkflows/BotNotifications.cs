using Elrond.TradeOffer.Web.BotWorkflows.Bids;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Network;
using Elrond.TradeOffer.Web.Utils;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows
{
    public class BotNotifications : IBotNotifications
    {
        private readonly INetworkStrategies _networkStrategies;

        public BotNotifications(INetworkStrategies networkStrategies)
        {
            _networkStrategies = networkStrategies;
        }

        public async Task NotifyOnOfferSendToBlockchainAsync(ITelegramBotClient client, Offer offer, Bid bid, CancellationToken ct)
        {
            var networkStrategy = _networkStrategies.GetStrategy(offer.Network);
            var offeredAmount = offer.Amount.ToHtmlWithIdentifierUrl(networkStrategy);
            var bidAmount = bid.Amount.ToHtmlWithIdentifierUrl(networkStrategy);
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
                        InlineKeyboardButton.WithCallbackData("View offer and proceed",
                            $"{CommonQueries.ShowOfferQuery(bid.OfferId)}"),
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
                        InlineKeyboardButton.WithCallbackData("View offer",
                            $"{CommonQueries.ShowOfferQuery(bid.OfferId)}"),
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
                    $"You trade offer has been cancelled. You claimed back {offer.Amount.ToHtmlWithIdentifierUrl(strategy)}.",
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
            var offeredTokens = offer.Amount.ToHtmlWithIdentifierUrl(strategy);
            var acceptedTokens = acceptedBid.Amount.ToHtmlWithIdentifierUrl(strategy);
            await client.SendTextMessageAsync(
                chatId, $"You accepted the bid of {acceptedTokens} for the offer of {offeredTokens} was accepted.",
                ParseMode.Html,
                disableWebPagePreview: true,
                cancellationToken: ct);

            await ApiExceptionHelper.RunAndSupressAsync(async () =>
            {
                await client.SendTextMessageAsync(
                    acceptedBid.CreatorChatId,
                    $"Your bid of {acceptedTokens} for the offer of {offeredTokens} has been accepted.",
                    ParseMode.Html,
                    disableWebPagePreview: true,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Check it out",
                            $"{CommonQueries.ShowOfferQuery(acceptedBid.OfferId)}"),
                    }),
                    cancellationToken: ct);
            });

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

            await ApiExceptionHelper.RunAndSupressAsync(() =>
                client.SendTextMessageAsync(bid.CreatorChatId,
                    $"Your bid of {bid.Amount.ToCurrencyStringWithIdentifier()} has been declined.",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Check it out",
                            $"{CommonQueries.ShowOfferQuery(bid.OfferId)}"),
                    }),
                    cancellationToken: ct));
        }

        public async Task NotifyOnBidPlacedAsync(ITelegramBotClient client, Offer offer, long bidChatId, TokenAmount bidAmount, CancellationToken ct)
        {
            var strategy = _networkStrategies.GetStrategy(offer.Network);
            var bidTokens = bidAmount.ToHtmlWithIdentifierUrl(strategy);
            var offeredTokens = offer.Amount.ToHtmlWithIdentifierUrl(strategy);

            await client.SendTextMessageAsync(
                bidChatId,
                $"You placed a bid of {bidTokens} for the offer of {offeredTokens}.",
                ParseMode.Html,
                disableWebPagePreview: true,
                cancellationToken: ct);

            await ApiExceptionHelper.RunAndSupressAsync(() =>
                client.SendTextMessageAsync(
                    offer.CreatorChatId,
                    $"You received a bid ({bidTokens}) for your offer of {offeredTokens}.",
                    ParseMode.Html,
                    disableWebPagePreview: true,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Check it out",
                            $"{CommonQueries.ShowOfferQuery(offer.Id)}"),
                    }),
                    cancellationToken: ct));
        }
    }
}
