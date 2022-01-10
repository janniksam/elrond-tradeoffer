using System.Timers;
using Elrond.TradeOffer.Web.BotWorkflows;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Repositories;
using Timer = System.Timers.Timer;

namespace Elrond.TradeOffer.Web.Services;

public class ElrondTradeStatusPollService : IHostedService
{
    private const double PollInterval = 30000;
    private readonly Func<IOfferRepository> _offerManagerFactory;
    private readonly IBotManager _botManager;
    private readonly IElrondApiService _elrondApiService;
    private Timer? _timer;

    public ElrondTradeStatusPollService(
        Func<IOfferRepository> offerManagerFactory,
        IBotManager botManager,
        IElrondApiService elrondApiService)
    {
        _offerManagerFactory = offerManagerFactory;
        _botManager = botManager;
        _elrondApiService = elrondApiService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(PollInterval);
        _timer.Elapsed += Poll;
        _timer.Start();

        return Task.CompletedTask;
    }

    private async void Poll(object? sender, ElapsedEventArgs e)
    {
        var offerManager = _offerManagerFactory();
        await PollClaimableOffersAsync(offerManager);
        await PollInitiatedOffersAsync(offerManager);
    }

    private async Task PollInitiatedOffersAsync(IOfferRepository offerRepository)
    {
        if (_botManager.Client == null)
        {
            return;
        }

        var bids = await offerRepository.GetInitiatedOffersAsync(CancellationToken.None);
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
                CancellationToken.None);

            await BotNotifications.NotifyOnOfferSendToBlockchainAsync(_botManager.Client, offer, bid,
                CancellationToken.None);
        }
    }

    private async Task PollClaimableOffersAsync(IOfferRepository offerRepository)
    {
        if (_botManager.Client == null)
        {
            return;
        }

        var bids = await offerRepository.GetClaimableOffersAsync(CancellationToken.None);
        foreach (var (bid, offer) in bids)
        {
            var finished = await _elrondApiService.IsOfferFinishedInSmartContractAsync(offer.Network, bid.OfferId);
            if (!finished)
            {
                continue;
            }

            await offerRepository.CompleteOfferAsync(offer.Id, CancellationToken.None);

            await BotNotifications.NotifyOnTradeCompletedAsync(_botManager.Client, offer, bid,
                CancellationToken.None);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        _timer?.Close();
        return Task.CompletedTask;
    }
}