using System.Timers;
using Elrond.TradeOffer.Web.BotWorkflows;
using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Repositories;
using Timer = System.Timers.Timer;

namespace Elrond.TradeOffer.Web.Services;

public class ElrondTradeStatusPollService : IHostedService
{
    private readonly int _pollInterval;
    private readonly Func<IOfferRepository> _offerManagerFactory;
    private readonly IBotManager _botManager;
    private readonly IElrondApiService _elrondApiService;
    private readonly IBotNotifications _botNotifications;
    private readonly ILogger<ElrondTradeOfferBotService> _log;
    private Timer? _timer;

    public ElrondTradeStatusPollService(
        IConfiguration configuration,
        Func<IOfferRepository> offerManagerFactory,
        IBotManager botManager,
        IElrondApiService elrondApiService,
        IBotNotifications botNotifications,
        ILogger<ElrondTradeOfferBotService> log)
    {
        _pollInterval = configuration.GetValue<int>("StatusPollInterval");
        _offerManagerFactory = offerManagerFactory;
        _botManager = botManager;
        _elrondApiService = elrondApiService;
        _botNotifications = botNotifications;
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
        var offerManager = _offerManagerFactory();
        try
        {
            await PollClaimableOffersAsync(offerManager, CancellationToken.None);
            await PollInitiatedOffersAsync(offerManager, CancellationToken.None);
            await PollCancelledOffersAsync(offerManager, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _log.LogError(exception, "Error when polling");
        }
    }

    private async Task PollCancelledOffersAsync(IOfferRepository offerManager, CancellationToken ct)
    {
        if (_botManager.Client == null)
        {
            return;
        }

        var bids = await offerManager.GetCancellingOffersAsync(ct);
        foreach (var (bid, offer) in bids)
        {
            var finished = await _elrondApiService.IsOfferFinishedInSmartContractAsync(offer.Network, bid.OfferId);
            if (!finished)
            {
                continue;
            }

            await offerManager.CompleteOfferAsync(offer.Id, ct);
            await _botNotifications.NotifyOnOfferCancelledAsync(_botManager.Client, offer, ct);
        }
    }

    private async Task PollInitiatedOffersAsync(IOfferRepository offerRepository, CancellationToken ct)
    {
        if (_botManager.Client == null)
        {
            return;
        }

        var bids = await offerRepository.GetInitiatedOffersAsync(ct);
        foreach (var (bid, offer) in bids)
        {
            var foundInSc = await _elrondApiService.IsOfferInSmartContractAsync(offer.Network, bid.OfferId);
            if (!foundInSc)
            {
                continue;
            }

            await offerRepository.UpdateBidAsync(
                bid.OfferId,
                bid.CreatorUserId,
                b =>
                {
                    b.State = BidState.ReadyForClaiming;
                    return true;
                },
                ct);

            await _botNotifications.NotifyOnOfferSendToBlockchainAsync(_botManager.Client, offer, bid, ct);
        }
    }

    private async Task PollClaimableOffersAsync(IOfferRepository offerRepository, CancellationToken ct)
    {
        if (_botManager.Client == null)
        {
            return;
        }

        var bids = await offerRepository.GetClaimableOffersAsync(ct);
        foreach (var (bid, offer) in bids)
        {
            var finished = await _elrondApiService.IsOfferFinishedInSmartContractAsync(offer.Network, bid.OfferId);
            if (!finished)
            {
                continue;
            }

            await offerRepository.CompleteOfferAsync(offer.Id, ct);

            await _botNotifications.NotifyOnTradeCompletedAsync(_botManager.Client, offer, bid, ct);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        _timer?.Close();
        return Task.CompletedTask;
    }
}