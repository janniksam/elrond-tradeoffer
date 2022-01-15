using System.Linq.Expressions;
using Elrond.TradeOffer.Web.BotWorkflows.Bids;
using Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
using Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.User;
using Elrond.TradeOffer.Web.Database;
using Microsoft.EntityFrameworkCore;

namespace Elrond.TradeOffer.Web.Repositories
{
    public class SqlOfferRepository : IOfferRepository
    {
        private readonly IDbContextFactory<ElrondTradeOfferDbContext> _dbContextFactory;
        private readonly ILogger<SqlOfferRepository> _logger;

        public SqlOfferRepository(
            IDbContextFactory<ElrondTradeOfferDbContext> dbContextFactory,
            ILogger<SqlOfferRepository> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<Guid> PlaceAsync(ElrondUser user, TemporaryOffer temporaryOffer, long chatId, CancellationToken ct)
        {
            if (temporaryOffer.Amount == null ||
                temporaryOffer.Description == null ||
                temporaryOffer.Token == null)
            {
                throw new ArgumentException($"{nameof(temporaryOffer)} not filled correctly.", nameof(temporaryOffer));
            }
            if (user.Address == null)
            {
                throw new ArgumentException($"{nameof(user)} not filled correctly.", nameof(user));
            }

            var dbOffer = new DbOffer(
                Guid.NewGuid(),
                user.Network,
                user.UserId,
                chatId,
                temporaryOffer.Description,
                temporaryOffer.Token.Ticker,
                temporaryOffer.Token.Name,
                temporaryOffer.Token.Nonce,
                temporaryOffer.Token.DecimalPrecision,
                temporaryOffer.Amount.Value.ToString());

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            dbContext.Offers.Add(dbOffer);
            await dbContext.SaveChangesAsync(ct);
            
            return dbOffer.Id;
        }

        public async Task<Offer?> GetAsync(Guid offerId, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            return await GetAsync(offerId, dbContext, ct);
        }

        private async Task<Offer?> GetAsync(Guid offerId, ElrondTradeOfferDbContext dbContext,CancellationToken ct)
        {
            var dbOffer = await dbContext.Offers.FindAsync(new object[] { offerId }, ct);
            if (dbOffer == null)
            {
                return null;
            }

            return Offer.From(dbOffer);
        }

        public async Task<IReadOnlyCollection<Offer>> GetAllOffersAsync(ElrondNetwork network, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var dbOffers = await dbContext.Offers.Where(p => p.Network == network).ToArrayAsync(ct);
            return dbOffers.Select(Offer.From).ToArray();
        }

        public async Task<bool> PlaceBidAsync(TemporaryBid temporaryBid, long chatId, CancellationToken ct)
        {
            if (temporaryBid.OfferId == null ||
                temporaryBid.Amount == null ||
                temporaryBid.Token == null)
            {
                throw new ArgumentException("Incomplete temporaryBid", nameof(temporaryBid));
            }

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var existingBid = dbContext.Bids.FirstOrDefault(p => p.OfferId == temporaryBid.OfferId &&
                                                                 p.CreatorUserId == temporaryBid.CreatorUserId);
            if (existingBid != null)
            {
                if (existingBid.State is BidState.Created or BidState.Accepted or BidState.Declined)
                {
                    dbContext.Remove(existingBid);
                }
                else
                {
                    return false;
                }
            }
            
            var dbBid = new DbBid(
                temporaryBid.OfferId.Value, 
                temporaryBid.CreatorUserId, 
                chatId, 
                temporaryBid.BidState,
                temporaryBid.Token.Ticker,
                temporaryBid.Token.Name,
                temporaryBid.Token.Nonce,
                temporaryBid.Token.DecimalPrecision,
                temporaryBid.Amount.Value.ToString());
            dbContext.Bids.Add(dbBid);
            await dbContext.SaveChangesAsync(ct);
            
            return true;
        }

        public async Task<bool> RemoveBidAsync(Guid offerId, long userId, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var bid = await dbContext.Bids.FindAsync(BidQueryKey(offerId, userId), ct);
            if (bid == null)
            {
                return false;
            }

            if (bid.State is BidState.Created or BidState.Accepted or BidState.Declined)
            {
                dbContext.Remove(bid);
                await dbContext.SaveChangesAsync(ct);
                return true;
            }

            if(bid.State is BidState.ReadyForClaiming)
            {
                bid.State = BidState.RemovedWhileOnBlockchain;
                await dbContext.SaveChangesAsync(ct);
                return true;
            }

            return false;
        }

        public async Task<IReadOnlyCollection<Bid>> GetBidsAsync(Guid offerId, Expression<Func<DbBid, bool>> bidPredicate, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var bids = await dbContext.Bids
                .Where(bidPredicate)
                .Where(p => p.OfferId == offerId)
                .ToArrayAsync(ct);
            return bids.Select(Bid.From).ToArray();
        }

        public async Task<IReadOnlyCollection<(Bid, Offer)>> GetInitiatedOffersAsync(CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            
            var bids = await dbContext.Bids
                .Include(p => p.Offer)
                .Where(p => p.State == BidState.TradeInitiated)
                .ToArrayAsync(ct);
            return bids.Select(p => (Bid.From(p), Offer.From(p.Offer))).ToArray();
        }

        public async Task<IReadOnlyCollection<(Bid, Offer)>> GetClaimableOffersAsync(CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            var bids = await dbContext.Bids
                .Include(p => p.Offer)
                .Where(p => p.State == BidState.ReadyForClaiming)
                .ToArrayAsync(ct);
            return bids.Select(p => (Bid.From(p), Offer.From(p.Offer))).ToArray();
        }

        public async Task<IReadOnlyCollection<(Bid, Offer)>> GetCancellingOffersAsync(CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            var bids = await dbContext.Bids
                .Include(p => p.Offer)
                .Where(p => p.State == BidState.CancelInitiated)
                .ToArrayAsync(ct);

            return bids.Select(p => (Bid.From(p), Offer.From(p.Offer))).ToArray();
        }

        public async Task<IReadOnlyCollection<Bid>> GetBidsAsync(Guid offerId, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var bids = await dbContext.Bids
                .Where(p => p.OfferId == offerId)
                .ToArrayAsync(ct);
            return bids.Select(Bid.From).ToArray();
        }

        public async Task<bool> UpdateBidAsync(Guid offerId, long userId, Func<DbBid, bool> updateBid, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var bid = await dbContext.Bids.FindAsync(BidQueryKey(offerId, userId), ct);
            if (bid == null)
            {
                return false;
            }

            if (!updateBid(bid))
            {
                return false;
            }

            await dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<Bid?> GetBidAsync(Guid offerId, long userId, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var dbBid = await dbContext.Bids.FindAsync(BidQueryKey(offerId, userId), ct);
            if (dbBid == null)
            {
                return null;
            }

            return Bid.From(dbBid);
        }

        public async Task CompleteOfferAsync(Guid offerId, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var transaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var dbOffer = await dbContext.Offers.Include(p => p.Bids).FirstOrDefaultAsync(p => p.Id == offerId, ct);
                if (dbOffer == null)
                {
                    return;
                }

                dbContext.Bids.RemoveRange(dbOffer.Bids);
                dbContext.Offers.Remove(dbOffer);
                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error while completing the offer.");
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<(bool sucessfullyAccepted, IReadOnlyCollection<Bid> declinedBids)> AcceptAsync(Guid offerId, long bidUserId, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var offer = await dbContext.Offers.Include(p => p.Bids).FirstOrDefaultAsync(p => p.Id == offerId, ct);
            if (offer == null)
            {
                return (false, Array.Empty<Bid>());
            }

            if (offer.Bids.All(p => p.CreatorUserId != bidUserId && p.State is BidState.Created))
            {
                return (false, Array.Empty<Bid>());
            }

            var declinedBids = new List<Bid>();
            foreach (var offerBid in offer.Bids)
            {
                if (offerBid.CreatorUserId == bidUserId)
                {
                    offerBid.State = BidState.Accepted;
                }
                else
                {
                    offerBid.State = BidState.Declined;
                    declinedBids.Add(Bid.From(offerBid));
                }
            }

            await dbContext.SaveChangesAsync(ct);
            return (true, declinedBids.ToArray());
        }

        public async Task<Bid?> GetPendingBidAsync(Guid offerId, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var acceptedBid = await dbContext.Bids.FirstOrDefaultAsync(
                p => p.OfferId == offerId && 
                     p.State == BidState.Accepted || p.State == BidState.TradeInitiated, 
                ct);
            if (acceptedBid == null)
            {
                return null;
            }

            return Bid.From(acceptedBid);
        }

        private static object[] BidQueryKey(Guid offerId, long userId)
        {
            return new object[] { offerId, userId };
        }

        public async Task<CancelOfferResult> CancelAsync(Guid offerId, long userId, CancellationToken ct)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var offer = await dbContext.Offers.Include(p => p.Bids).FirstOrDefaultAsync(p => p.Id == offerId, ct);
                if (offer == null)
                {
                    return CancelOfferResult.OfferNotFound;
                }

                if (offer.CreatorUserId != userId)
                {
                    return CancelOfferResult.InvalidUser;
                }

                foreach (var bid in offer.Bids)
                {
                    if (bid.State is BidState.CancelInitiated or
                                     BidState.ReadyForClaiming or 
                                     BidState.Removed or 
                                     BidState.RemovedWhileOnBlockchain)
                    {
                        bid.State = BidState.CancelInitiated;
                        await dbContext.SaveChangesAsync(ct);
                        await transaction.CommitAsync(ct);
                        return CancelOfferResult.CreatorNeedsToRetrieveTokens;
                    }
                    
                    dbContext.Remove(bid);
                }

                dbContext.Remove(offer);
                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return CancelOfferResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cancelling the offer.");
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }
}
