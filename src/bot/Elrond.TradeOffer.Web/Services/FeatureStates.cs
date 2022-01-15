using Elrond.TradeOffer.Web.Database;
using Microsoft.EntityFrameworkCore;

namespace Elrond.TradeOffer.Web.Services
{
    public class FeatureStatesManager : IFeatureStatesManager
    {
        private const string DevNetEnabledFeatureStateId = "DevNetEnabled";
        private const string TestNetEnabledFeatureStateId = "TestNetEnabled"; 
        private const string MainNetEnabledFeatureStateId = "MainNetEnabled";

        private readonly IDbContextFactory<ElrondTradeOfferDbContext> _dbContextFactory;
        private bool? _devNetEnabled;
        private bool? _testNetEnabled;
        private bool? _mainNetEnabled;

        public FeatureStatesManager(IDbContextFactory<ElrondTradeOfferDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<bool> GetDevNetEnabledAsync(CancellationToken ct)
        {
            if (_devNetEnabled.HasValue)
            {
                return _devNetEnabled.Value;
            }

            var state = await GetStateOrNullAsync(DevNetEnabledFeatureStateId, ct);
            if (state == null)
            {
                _devNetEnabled = false;
                return false;
            }

            _devNetEnabled = bool.Parse(state);
            return _devNetEnabled.Value;
        }
        
        public async Task<bool> GetTestNetEnabledAsync(CancellationToken ct)
        {
            if (_testNetEnabled.HasValue)
            {
                return _testNetEnabled.Value;
            }

            var state = await GetStateOrNullAsync(TestNetEnabledFeatureStateId, ct);
            if (state == null)
            {
                _testNetEnabled = false;
                return false;
            }

            _testNetEnabled = bool.Parse(state);
            return _testNetEnabled.Value;
        }
        
        public async Task<bool> GetMainNetEnabledAsync(CancellationToken ct)
        {
            if (_mainNetEnabled.HasValue)
            {
                return _mainNetEnabled.Value;
            }


            var state = await GetStateOrNullAsync(MainNetEnabledFeatureStateId, ct);
            if (state == null)
            {
                _mainNetEnabled = false;
                return false;
            }

            _mainNetEnabled = bool.Parse(state);
            return _mainNetEnabled.Value;
        }

        private async Task<string?> GetStateOrNullAsync(string stateId, CancellationToken ct)
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var featureState = await dbContext.FeatureStates.FirstOrDefaultAsync(p => p.Id == stateId, ct);
            return featureState?.Value;
        }

        public async Task SetDevNetStateAsync(bool enabled, long userId, CancellationToken ct)
        {
            await AddOrUpdateAsync(DevNetEnabledFeatureStateId, enabled.ToString(), userId, ct);
            _devNetEnabled = enabled;
        }

        public async Task SetTestNetStateAsync(bool enabled, long userId, CancellationToken ct)
        {
            await AddOrUpdateAsync(TestNetEnabledFeatureStateId, enabled.ToString(), userId, ct);
            _testNetEnabled = enabled;
        }

        public async Task SetMainNetStateAsync(bool enabled, long userId, CancellationToken ct)
        {
            await AddOrUpdateAsync(MainNetEnabledFeatureStateId, enabled.ToString(), userId, ct);
            _mainNetEnabled = enabled;
        }

        private async Task AddOrUpdateAsync(string stateId, string enabled, long userId, CancellationToken ct)
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var featureState = await dbContext.FeatureStates.FirstOrDefaultAsync(p => p.Id == stateId, ct);
            if (featureState == null)
            {
                await dbContext.AddAsync(new DbFeatureState
                {
                    Id = stateId,
                    Value = enabled,
                    ChangedById = userId
                }, ct);
            }
            else
            {
                featureState.Value = enabled;
            }

            await dbContext.SaveChangesAsync(ct);
        }
    }
}
