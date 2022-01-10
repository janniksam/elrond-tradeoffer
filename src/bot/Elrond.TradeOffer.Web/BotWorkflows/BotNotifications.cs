using Elrond.TradeOffer.Web.BotWorkflows.Bids;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Utils;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows
{
    public static class BotNotifications
    {
        public static async Task NotifyOnOfferSendToBlockchainAsync(ITelegramBotClient client, Offer offer, Bid bid, CancellationToken ct)
        {
            var offeredAmount = offer.Amount.ToCurrencyStringWithIdentifier();
            var bidAmount = bid.Amount.ToCurrencyStringWithIdentifier();
            await ApiExceptionHelper.RunAndSupressAsync(() =>
            {
                return client.SendTextMessageAsync(
                    bid.CreatorChatId,
                    $"The creator of the offer has sent the {offeredAmount} to the smart contract.\n\n" +
                    $"To complete the trade (you bid {bidAmount}) now, please proceed by pressing the button below:",
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
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("View offer",
                            $"{CommonQueries.ShowOfferQuery(bid.OfferId)}"),
                    }),
                    cancellationToken: ct);
            });
        }

        public static async Task NotifyOnTradeCompletedAsync(ITelegramBotClient client, Offer offer, Bid bid, CancellationToken ct)
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
        
        public static async Task NotifyOnBidAccepted(ITelegramBotClient client, long chatId, Bid bid, CancellationToken ct)
        {
            await client.SendTextMessageAsync(
                chatId, $"The bid of {bid.Amount.ToCurrencyStringWithIdentifier()} was accepted.",
                cancellationToken: ct);

            await ApiExceptionHelper.RunAndSupressAsync(() =>
            {
                return client.SendTextMessageAsync(
                    bid.CreatorChatId,
                    $"Your bid of {bid.Amount.ToCurrencyStringWithIdentifier()} has been accepted.",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Check it out",
                            $"{CommonQueries.ShowOfferQuery(bid.OfferId)}"),
                    }),
                    cancellationToken: ct);
            });
        }

        public static async Task NotifyOnBidDeclined(ITelegramBotClient client, long chatId, Bid bid, CancellationToken ct)
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

        public static async Task NotifyOnBidPlacedAsync(ITelegramBotClient client, Offer offer, long bidChatId, TokenAmount bidAmount, CancellationToken ct)
        {
            var bidTokens = bidAmount.ToCurrencyStringWithIdentifier();

            await client.SendTextMessageAsync(
                bidChatId,
                $"The bid of {bidTokens} has been placed.",
                cancellationToken: ct);

            await ApiExceptionHelper.RunAndSupressAsync(() =>
                client.SendTextMessageAsync(
                    offer.CreatorChatId,
                    $"You received a bid ({bidTokens}) for your offer {offer.Amount.ToCurrencyStringWithIdentifier()}.",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Check it out",
                            $"{CommonQueries.ShowOfferQuery(offer.Id)}"),
                    }),
                    cancellationToken: ct));
        }
    }
}
