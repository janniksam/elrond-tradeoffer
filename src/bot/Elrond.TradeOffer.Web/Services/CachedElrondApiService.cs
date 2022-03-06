using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Network;
using Microsoft.Extensions.Caching.Memory;

namespace Elrond.TradeOffer.Web.Services;

public class CachedElrondApiService : ElrondApiService
{
    private readonly IMemoryCache _memoryCache;

    // ReSharper disable NotAccessedPositionalProperty.Local
    private record BalanceCacheKey(string Address, ElrondNetwork Network);
    // ReSharper restore NotAccessedPositionalProperty.Local

    public CachedElrondApiService(
        IMemoryCache memoryCache,
        INetworkStrategies networkStrategies, ILogger<ElrondApiService> logger) : base(networkStrategies, logger)
    {
        _memoryCache = memoryCache;
    }

    public override async Task<IReadOnlyCollection<ElrondToken>> GetBalancesAsync(string address, ElrondNetwork network)
    {
        var cacheKey = new BalanceCacheKey(address, network);

        if (!_memoryCache.TryGetValue(cacheKey, out IReadOnlyCollection<ElrondToken> balances))
        {
            balances = await base.GetBalancesAsync(address, network);
            
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(2));
            _memoryCache.Set(cacheKey, balances, cacheEntryOptions);
        }

        return balances;
    }
}