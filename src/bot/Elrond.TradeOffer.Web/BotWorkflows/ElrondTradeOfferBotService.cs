using Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.UserState;
using Elrond.TradeOffer.Web.BotWorkflows.Workflows;
using Elrond.TradeOffer.Web.Network;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Elrond.TradeOffer.Web.BotWorkflows
{
    public sealed class ElrondTradeOfferBotService : IHostedService, IDisposable
    {
        private readonly ITemporaryOfferManager _temporaryOfferManager;
        private readonly ITemporaryBidManager _temporaryBidManager;
        private readonly IElrondApiService _elrondApiService;
        private readonly ITransactionGenerator _transactionGenerator;
        private readonly IBotManager _botManager;
        private readonly IUserContextManager _userContextManager;
        private readonly INetworkStrategies _networkStrategies;
        private readonly IBotNotifications _botNotification;
        private readonly IFeatureStatesManager _featureStatesManager;
        private readonly Func<IOfferRepository> _offerRepositoryFactory;
        private readonly ILogger<ElrondTradeOfferBotService> _logger;
        private readonly Func<IUserRepository> _userRepositoryFactory;
        private readonly CancellationTokenSource _cts;
        
        public ElrondTradeOfferBotService(
            ITemporaryOfferManager temporaryOfferManager,
            ITemporaryBidManager temporaryBidManager,
            IElrondApiService elrondApiService,
            ITransactionGenerator transactionGenerator,
            IBotManager botManager,
            IUserContextManager userContextManager,
            INetworkStrategies networkStrategies,
            IBotNotifications botNotification,
            IFeatureStatesManager featureStatesManager,
            Func<IUserRepository> userRepositoryFactory,
            Func<IOfferRepository> offerRepositoryFactory,
            ILogger<ElrondTradeOfferBotService> logger)
        {
            _temporaryOfferManager = temporaryOfferManager;
            _temporaryBidManager = temporaryBidManager;
            _elrondApiService = elrondApiService;
            _transactionGenerator = transactionGenerator;
            _botManager = botManager;
            _userContextManager = userContextManager;
            _networkStrategies = networkStrategies;
            _botNotification = botNotification;
            _featureStatesManager = featureStatesManager;
            _userRepositoryFactory = userRepositoryFactory;
            _offerRepositoryFactory = offerRepositoryFactory;
            _logger = logger;
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
                _logger.LogError(ex, "Unexpected ApiRequestException.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception.");
            }
        }

        private IEnumerable<IBotProcessor> GetWorkflows()
        {
            var userRepository = _userRepositoryFactory();
            var offerRepository = _offerRepositoryFactory();
            var startmenuWorkflow = new StartMenuWorkflow(userRepository, _featureStatesManager);
            var offerListWorkflow = new OfferListWorkflow(
                userRepository, 
                offerRepository, 
                _transactionGenerator, 
                _elrondApiService, 
                _botNotification, 
                _networkStrategies,
                startmenuWorkflow);

            var botWorkflows = new IBotProcessor[]
            {
                startmenuWorkflow,
                new OfferCreationWorkflow(userRepository, _userContextManager, _temporaryOfferManager, 
                    offerRepository, _elrondApiService, _networkStrategies, startmenuWorkflow),
                offerListWorkflow,
                new BidCreationWorkflow(
                    userRepository, _userContextManager, offerRepository, _temporaryBidManager, 
                    _elrondApiService, offerListWorkflow, _botNotification, _networkStrategies,
                    startmenuWorkflow),
                new ChangeSettingsWorkflow(userRepository, _elrondApiService, _userContextManager, _networkStrategies),
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
            _logger.LogError(exception, "An unhandled telegram exception has occured");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
