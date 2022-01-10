using Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.UserState;
using Elrond.TradeOffer.Web.BotWorkflows.Workflows;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Elrond.TradeOffer.Web.BotWorkflows
{
    public class ElrondTradeOfferBotService : IHostedService, IDisposable
    {
        private readonly ITemporaryOfferManager _temporaryOfferManager;
        private readonly ITemporaryBidManager _temporaryBidManager;
        private readonly IElrondApiService _elrondApiService;
        private readonly ITransactionGenerator _transactionGenerator;
        private readonly IBotManager _botManager;
        private readonly IUserContextManager _userContextManager;
        private readonly Func<IOfferRepository> _offerRepositoryFactory;
        private readonly Func<IUserRepository> _userRepositoryFactory;
        private readonly CancellationTokenSource _cts;
        
        public ElrondTradeOfferBotService(
            ITemporaryOfferManager temporaryOfferManager,
            ITemporaryBidManager temporaryBidManager,
            IElrondApiService elrondApiService,
            ITransactionGenerator transactionGenerator,
            IBotManager botManager,
            IUserContextManager userContextManager,
            Func<IUserRepository> userRepositoryFactory,
            Func<IOfferRepository> offerRepositoryFactory)
        {
            _temporaryOfferManager = temporaryOfferManager;
            _temporaryBidManager = temporaryBidManager;
            _elrondApiService = elrondApiService;
            _transactionGenerator = transactionGenerator;
            _botManager = botManager;
            _userContextManager = userContextManager;
            _userRepositoryFactory = userRepositoryFactory;
            _offerRepositoryFactory = offerRepositoryFactory;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _botManager.StartAsync(HandleUpdateAsync, HandleErrorAsync, _cts.Token);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
        {
            try
            {
                var botWorkflows = GetWorkflows();

                if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.From != null)
                {
                    foreach (var botProcessor in botWorkflows)
                    {
                        var workflowResult = await botProcessor.ProcessCallbackQueryAsync(client, update.CallbackQuery, ct);
                        if (!workflowResult.IsHandled)
                        {
                            continue;
                        }

                        var userId = update.CallbackQuery.From.Id;
                        _userContextManager.AddOrUpdate(userId, workflowResult.NewUserContext);

                        await AnswerCallbackAsync(client, update, ct);
                        return;
                    }

                    return;
                }

                if (update.Type == UpdateType.Message &&
                    update.Message?.From != null)
                {
                    foreach (var botProcessor in botWorkflows)
                    {
                        var workflowResult = await botProcessor.ProcessMessageAsync(client, update.Message, ct);
                        if (!workflowResult.IsHandled)
                        {
                            continue;
                        }

                        var fromId = update.Message.From.Id;
                        _userContextManager.AddOrUpdate(fromId, workflowResult.NewUserContext);
                        return;
                    }
                }
            }
            catch (ApiRequestException ex)
            {
            }
            catch (Exception ex)
            {
            }
        }

        private IBotProcessor[] GetWorkflows()
        {
            var userRepository = _userRepositoryFactory();
            var offerRepository = _offerRepositoryFactory();
            var startmenuWorkflow = new StartMenuWorkflow();
            var offerListWorkflow = new OfferListWorkflow(
                userRepository, offerRepository, _transactionGenerator, _elrondApiService);

            var botWorkflows = new IBotProcessor[]
            {
                startmenuWorkflow,
                new OfferCreationWorkflow(userRepository, _userContextManager, _temporaryOfferManager, 
                    offerRepository, _elrondApiService, startmenuWorkflow.StartPage),
                offerListWorkflow,
                new BidCreationWorkflow(
                    userRepository, _userContextManager, offerRepository, _temporaryBidManager, 
                    _elrondApiService, offerListWorkflow, startmenuWorkflow.StartPage),
                new ChangeSettingsWorkflow(userRepository, _elrondApiService, _userContextManager),
            };

            return botWorkflows;
        }

        private static async Task AnswerCallbackAsync(ITelegramBotClient client, Update update, CancellationToken ct)
        {
            if (update.CallbackQuery == null)
            {
                return;
            }

            try
            {
                await client.AnswerCallbackQueryAsync(update.CallbackQuery.Id, cancellationToken: ct);
            }
            catch (ApiRequestException)
            {
                // can crash on server-restarts
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
