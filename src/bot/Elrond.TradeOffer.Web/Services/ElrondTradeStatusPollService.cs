using System.Timers;
using Elrond.TradeOffer.Web.BotWorkflows;
using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Repositories;
using Telegram.Bot;
using Timer = System.Timers.Timer;

namespace Elrond.TradeOffer.Web.Services;

public class ElrondTradeStatusPollService : IHostedService
{
    private readonly int _pollInterval;
    private readonly Func<IOfferRepository> _offerManagerFactory;
    private readonly IBotManager _botManager;
    private readonly IElrondApiService _elrondApiService;
    private readonly IBotNotificationsHelper _botNotificationsHelper;
    private readonly ILogger<ElrondTradeOfferBotService> _log;
    private Timer? _timer;

    public ElrondTradeStatusPollService(
        IConfiguration configuration,
        Func<IOfferRepository> offerManagerFactory,
        IBotManager botManager,
        IElrondApiService elrondApiService,
        IBotNotificationsHelper botNotificationsHelper,
        ILogger<ElrondTradeOfferBotService> log)
    {
        _pollInterval = configuration.GetValue<int>("StatusPollInterval");
        _offerManagerFactory = offerManagerFactory;
        _botManager = botManager;
        _elrondApiService = elrondApiService;
        _botNotificationsHelper = botNotificationsHelper;
        _log = log;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(_pollInterval);
        _timer.Elapsed += Poll;
        _timer.Start();

        return Task.CompletedTask;
    }

    private async void Poll(object? sender, ElapsedEventArgs e)
    {
        if (_botManager.Client == null)
        {
            return;
        }

        var offerManager = _offerManagerFactory();
        try
        {
            await PollInitiatedOffersAsync(_botManager.Client, offerManager, CancellationToken.None);
            await PollFinishedStatusAsync(_botManager.Client, offerManager, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _log.LogError(exception, "Error when polling");
        }
    }

    private async Task PollFinishedStatusAsync(ITelegramBotClient client, IOfferRepository offerManager, CancellationToken ct)
    {
        var claimable = await offerManager.GetClaimableOffersAsync(ct);
        var cancelled = await offerManager.GetCancellingOffersAsync(ct);
        var waitingForFinish = claimable.Concat(cancelled).ToArray().GroupBy(p => p.Item2.Network);

        foreach (var offerToCheck in waitingForFinish)
        {
            var statusMapping = await _elrondApiService.IsOfferFinishedInSmartContractAsync(
                offerToCheck.Key,
                offerToCheck.Select(p => p.Item1.OfferId).ToArray());
            foreach (var (offerId, status) in statusMapping)
            {
                switch (status)
                {
                    case OfferFinishStatus.Cancelled:
                    {
                        var initiatedOffer = offerToCheck.First(p => p.Item2.Id == offerId);
                        await offerManager.CompleteOfferAsync(offerId, CancellationToken.None);
                        await _botNotificationsHelper.NotifyOnOfferCancelledAsync(client, initiatedOffer.Offer, CancellationToken.None);
                        break;
                    }
                    case OfferFinishStatus.Completed:
                    {
                        var claimableOffer = offerToCheck.First(p => p.Item2.Id == offerId);
                        await offerManager.CompleteOfferAsync(offerId, CancellationToken.None);
                        await _botNotificationsHelper.NotifyOnTradeCompletedAsync(client, claimableOffer.Offer, claimableOffer.Bid, ct);
                        break;
                    }
                    case OfferFinishStatus.NotFound:
                    {
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }

    private async Task PollInitiatedOffersAsync(ITelegramBotClient client, IOfferRepository offerRepository, CancellationToken ct)
    {
        var initiated = await offerRepository.GetInitiatedOffersAsync(ct);
        var waitingForPayment = initiated.GroupBy(p => p.Item2.Network);

        foreach (var offerToCheck in waitingForPayment)
        {
            var statusMapping = await _elrondApiService.IsOfferInitiatedInSmartContractAsync(
                offerToCheck.Key, 
                offerToCheck.Select(p => p.Item1.OfferId).ToArray());
            foreach (var (offerId, status) in statusMapping)
            {
                if (status)
                {
                    var initiatedOffer = offerToCheck.First(p => p.Item2.Id == offerId);
                    await offerRepository.UpdateBidAsync(
                        initiatedOffer.Bid.OfferId,
                        initiatedOffer.Bid.CreatorUserId,
                        b =>
                        {
                            b.State = BidState.ReadyForClaiming;
                            return true;
                        },
                        ct);

                    await _botNotificationsHelper.NotifyOnOfferSendToBlockchainAsync(client, initiatedOffer.Offer, initiatedOffer.Bid, ct);
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        _timer?.Close();
        return Task.CompletedTask;
    }
}