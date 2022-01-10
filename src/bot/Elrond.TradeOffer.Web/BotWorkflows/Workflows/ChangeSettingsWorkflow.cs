using Elrond.TradeOffer.Web.BotWorkflows.UserState;
using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Extensions;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Services;
using Erdcsharp.Domain.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Elrond.TradeOffer.Web.BotWorkflows.Workflows
{
    public class ChangeSettingsWorkflow : IBotProcessor
    {
        private readonly IUserRepository _userManager;
        private readonly IElrondApiService _elrondApiService;
        private readonly IUserContextManager _userContextManager;
        private const string SetDevNetQuery = "setDevNet";
        private const string SetTestNetQuery = "setTestNet";
        private const string SetMainNetQuery = "setMainNet";
        private const string ChangeAddressQuery = "changeAddress";

        public ChangeSettingsWorkflow(
            IUserRepository userManager,
            IElrondApiService elrondApiService,
            IUserContextManager userContextManager)
        {
            _userManager = userManager;
            _elrondApiService = elrondApiService;
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

            if (query.Data == CommonQueries.ChangeNetworkOrAddressQuery)
            {
                await DeleteMessageAsync(client, chatId, previousMessageId, ct);
                await ChangeNetworkOrAddress(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }

            if (query.Data == SetDevNetQuery)
            {
                var elrondUser = await _userManager.GetAsync(userId, ct);
                elrondUser.Network = ElrondNetwork.Devnet;
                await _userManager.AddOrUpdateAsync(elrondUser, ct);

                await DeleteMessageAsync(client, chatId, previousMessageId, ct);
                await ChangeNetworkOrAddress(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }
            if (query.Data == SetTestNetQuery)
            {
                var elrondUser = await _userManager.GetAsync(userId, ct);
                elrondUser.Network = ElrondNetwork.Testnet;
                await _userManager.AddOrUpdateAsync(elrondUser, ct);

                await DeleteMessageAsync(client, chatId, previousMessageId, ct);
                await ChangeNetworkOrAddress(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }
            if (query.Data == SetMainNetQuery)
            {
                var elrondUser = await _userManager.GetAsync(userId, ct);
                elrondUser.Network = ElrondNetwork.Mainnet;
                await _userManager.AddOrUpdateAsync(elrondUser, ct);

                await DeleteMessageAsync(client, chatId, previousMessageId, ct);
                await ChangeNetworkOrAddress(client, userId, chatId, ct);
                return WorkflowResult.Handled();
            }
            if (query.Data == ChangeAddressQuery)
            {
                await DeleteMessageAsync(client, chatId, previousMessageId, ct);
                return await AskForWalletAddress(client, chatId, ct);
            }

            return WorkflowResult.Unhandled();
        }

        private async Task DeleteMessageAsync(ITelegramBotClient client, long chatId, int previousMessageId, CancellationToken ct)
        {
            await client.DeleteMessageAsync(chatId, previousMessageId, ct);
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

            var context = _userContextManager.Get(message.From.Id);
            if (context == UserContext.EnterWalletAddress)
            {
                return await ChangeAddressAsync(client, message, ct);
            }

            return WorkflowResult.Unhandled();
        }

        private async Task ChangeNetworkOrAddress(ITelegramBotClient client, long userId, long chatId, CancellationToken ct)
        {
            var elrondUser = await _userManager.GetAsync(userId, ct);
            var replyMessage = $"<b>Your current address:</b> {elrondUser.ShortedAddress}\n" +
                               $"<b>Network:</b> {elrondUser.Network}\n\n" +
                               "Choose your action:";

            var buttons = new List<InlineKeyboardButton[]>();
            if (elrondUser.Network == ElrondNetwork.Devnet)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Use testnet", SetTestNetQuery),
                    InlineKeyboardButton.WithCallbackData("Use mainnet", SetMainNetQuery)
                });
            }
            else if (elrondUser.Network == ElrondNetwork.Testnet)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Use devnet", SetDevNetQuery),
                    InlineKeyboardButton.WithCallbackData("Use mainnet", SetMainNetQuery)
                });
            }
            else if (elrondUser.Network == ElrondNetwork.Mainnet)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Use devnet", SetDevNetQuery),
                    InlineKeyboardButton.WithCallbackData("Use testnet", SetTestNetQuery)
                });
            }

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("⚡ Change wallet address", ChangeAddressQuery),
                InlineKeyboardButton.WithCallbackData("Back", CommonQueries.BackToHomeQuery)
            });

            await client.SendTextMessageAsync(
                chatId,
                replyMessage,
                ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private async Task<WorkflowResult> ChangeAddressAsync(ITelegramBotClient client, Message message, CancellationToken ct)
        {
            if (message.From == null)
            {
                return WorkflowResult.Unhandled();
            }

            var walletAddress = message.Text?.Trim();
            if (string.IsNullOrWhiteSpace(walletAddress))
            {
                return await AskForWalletAddress(client, message.Chat.Id, ct);
            }

            var chatId = message.Chat.Id;
            try
            {
                var elrondUser = await _userManager.GetAsync(message.From.Id, ct);
                var _ = await _elrondApiService.GetAccountAsync(elrondUser.Network, walletAddress);
                elrondUser.Address = walletAddress;
                await _userManager.AddOrUpdateAsync(elrondUser, ct);

                await client.SendTextMessageAsync(
                    chatId,
                    $"Wallet address has been updated to {walletAddress}",
                    cancellationToken: ct);
                await ChangeNetworkOrAddress(client, message.From.Id, chatId, ct);
                return WorkflowResult.Handled();
            }
            catch (GatewayException)
            {
                await client.SendTextMessageAsync(
                    chatId,
                    "Could not update wallet address. Is this a valid bech32 address?",
                    cancellationToken: ct);

                return await AskForWalletAddress(client, chatId, ct);
            }
        }

        private static async Task<WorkflowResult> AskForWalletAddress(ITelegramBotClient client, long chatId, CancellationToken ct)
        {
            await client.SendTextMessageAsync(
                chatId,
                "What is your wallet address? (erd...)",
                cancellationToken: ct);

            return WorkflowResult.Handled(UserContext.EnterWalletAddress);
        }
    }
}
