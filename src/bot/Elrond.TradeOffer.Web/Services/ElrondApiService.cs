using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Extensions;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Network;
using Erdcsharp.Configuration;
using Erdcsharp.Provider;
using Erdcsharp.Provider.Dtos;
using Newtonsoft.Json;
using Token = Elrond.TradeOffer.Web.Models.Token;

namespace Elrond.TradeOffer.Web.Services;

public class ElrondApiService : IElrondApiService
{
    private readonly INetworkStrategies _networkStrategies;
    private readonly ILogger<ElrondApiService> _logger;

    public ElrondApiService(
        INetworkStrategies networkStrategies,
        ILogger<ElrondApiService> logger)
    {
        _networkStrategies = networkStrategies;
        _logger = logger;
    }

    public virtual async Task<IReadOnlyCollection<ElrondToken>> GetBalancesAsync(string address, ElrondNetwork network)
    {
        var networkStrategy = _networkStrategies.GetStrategy(network);

        var client = new HttpClient();
        var provider = new ElrondProvider(client, new ElrondNetworkConfiguration(networkStrategy.Network));
        var account = await provider.GetAccount(address);
        var esdtTokens = await GetEsdtTokens(address, networkStrategy);
        var nftsTokens = await GetNftTokens(address, networkStrategy);

        var tokens = new List<ElrondToken>
        {
            new(Token.Egld(), account.Balance)
        };

        foreach (var token in esdtTokens)
        {
            tokens.Add(ToElrondToken(token));
        }
        
        foreach (var token in nftsTokens)
        {
            tokens.Add(ToElrondToken(token));
        }

        return tokens;
    }

    private static ElrondToken ToElrondToken(NftToken token)
    {
        return new ElrondToken(Token.Nft(token.name, token.ticker, token.decimals, (ulong)token.nonce), token.balance ?? "1");
    }

    private static ElrondToken ToElrondToken(EsdtToken token)
    {
        return new ElrondToken(Token.Esdt(token.name, token.identifier, token.decimals), token.balance ?? "0");
    }

    private async Task<IReadOnlyCollection<EsdtToken>> GetEsdtTokens(string address, INetworkStrategy networkStrategy)
    {
        try
        {
            var client = new HttpClient();
            var apiGateway = networkStrategy.GetApiGateway();
            var tokenUrl = $"{apiGateway}/accounts/{address}/tokens";
            var tokensRaw = await client.GetStringAsync(tokenUrl);
            var tokens = JsonConvert.DeserializeObject<List<EsdtToken>>(tokensRaw);
            return tokens ?? (IReadOnlyCollection<EsdtToken>)Array.Empty<EsdtToken>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while retrieving tokens");
            return Array.Empty<EsdtToken>();
        }
    }

    private async Task<IReadOnlyCollection<NftToken>> GetNftTokens(string address, INetworkStrategy networkStrategy)
    {
        try
        {
            var client = new HttpClient();
            var apiGateway = networkStrategy.GetApiGateway();
            var tokenUrl = $"{apiGateway}/accounts/{address}/nfts";
            var tokensRaw = await client.GetStringAsync(tokenUrl);
            var tokens = JsonConvert.DeserializeObject<List<NftToken>>(tokensRaw);
            return tokens ?? (IReadOnlyCollection<NftToken>)Array.Empty<NftToken>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while retrieving tokens");
            return Array.Empty<NftToken>();
        }
    }


    public async Task<AccountDto> GetAccountAsync(ElrondNetwork network, string address)
    {
        var client = new HttpClient();
        var networkStrategy = _networkStrategies.GetStrategy(network);
        var provider = new ElrondProvider(client, new ElrondNetworkConfiguration(networkStrategy.Network));
        return await provider.GetAccount(address).ConfigureAwait(false);
    }

    public async Task<ConfigDataDto> GetNetworkConfigAsync(ElrondNetwork network)
    {
        var client = new HttpClient();
        var networkStrategy = _networkStrategies.GetStrategy(network);
        var provider = new ElrondProvider(client, new ElrondNetworkConfiguration(networkStrategy.Network));
        return await provider.GetNetworkConfig().ConfigureAwait(false);
    }

    public async Task<bool> IsOfferInSmartContractAsync(ElrondNetwork network, Guid offerId)
    {
        var networkStrategy = _networkStrategies.GetStrategy(network);
        
        var client = new HttpClient();
        var provider = new ElrondProvider(client, new ElrondNetworkConfiguration(networkStrategy.Network));
        
        var queryVmRequestDto = new QueryVmRequestDto
        {
            ScAddress = networkStrategy.GetSmartContractAddress(),
            FuncName = "get_trade_offer",
            Args = new[] { offerId.ToHex() },
        };

        var queryResult = await provider.QueryVm(queryVmRequestDto);
        return queryResult.Data.ReturnCode == "ok" && 
               queryResult.Data.ReturnData.Length > 0 && 
               !string.IsNullOrWhiteSpace(queryResult.Data.ReturnData[0]);
    }

    public async Task<IReadOnlyCollection<(Guid offerId, OfferFinishStatus status)>> IsOfferFinishedInSmartContractAsync(ElrondNetwork network, Guid[] offerIds)
    {
        var networkStrategy = _networkStrategies.GetStrategy(network);

        var client = new HttpClient();
        var provider = new ElrondProvider(client, new ElrondNetworkConfiguration(networkStrategy.Network));

        var queryVmRequestDto = new QueryVmRequestDto
        {
            ScAddress = networkStrategy.GetSmartContractAddress(),
            FuncName = "get_finished_offer_list",
            Args = offerIds.Select(p => p.ToHex()).ToArray()
        };

        var queryResult = await provider.QueryVm(queryVmRequestDto);
        if (queryResult.Data.ReturnCode != "ok" ||
            queryResult.Data.ReturnData.Length != offerIds.Length)
        {
            _logger.LogError("Could not get finished offers from sc");
            return Array.Empty<(Guid, OfferFinishStatus)>();
        }

        var result = new List<(Guid, OfferFinishStatus)>();
        for (var i = 0; i < offerIds.Length; i++)
        {
            var resultCodeBytes = Convert.FromBase64String(queryResult.Data.ReturnData[i]);
            var resultCode = resultCodeBytes.FirstOrDefault();
            result.Add((offerIds[i], (OfferFinishStatus)resultCode));
        }

        return result;
    }

    public async Task<IReadOnlyCollection<(Guid offerId, bool initiated)>> IsOfferInitiatedInSmartContractAsync(ElrondNetwork network, Guid[] offerIds)
    {
        var networkStrategy = _networkStrategies.GetStrategy(network);

        var client = new HttpClient();
        var provider = new ElrondProvider(client, new ElrondNetworkConfiguration(networkStrategy.Network));

        var queryVmRequestDto = new QueryVmRequestDto
        {
            ScAddress = networkStrategy.GetSmartContractAddress(),
            FuncName = "are_offers_pending",
            Args = offerIds.Select(p => p.ToHex()).ToArray()
        };

        var queryResult = await provider.QueryVm(queryVmRequestDto);
        if (queryResult.Data.ReturnCode != "ok" ||
            queryResult.Data.ReturnData.Length != offerIds.Length)
        {
            _logger.LogError("Could not get finished offers from sc");
            return Array.Empty<(Guid, bool)>();
        }

        var result = new List<(Guid, bool)>();
        for (var i = 0; i < offerIds.Length; i++)
        {
            var resultCodeBytes = Convert.FromBase64String(queryResult.Data.ReturnData[i]);
            var resultCode = resultCodeBytes.Length != 0 && BitConverter.ToBoolean(resultCodeBytes);
            result.Add((offerIds[i], resultCode));
        }

        return result;
    }

    public async Task<ElrondToken?> GetTokenAsync(ElrondNetwork network, string tokenIdentifier)
    {
        var esdtToken = await GetEsdtTokenAsync(network, tokenIdentifier);
        if (esdtToken != null)
        {
            return ToElrondToken(esdtToken);
        }

        var nft = await GetNftTokenAsync(network, tokenIdentifier);
        if (nft != null)
        {
            return ToElrondToken(nft);
        }

        return null;
    }

    private async Task<NftToken?> GetNftTokenAsync(ElrondNetwork network, string tokenIdentifier)
    {
        try
        {
            var client = new HttpClient();
            var networkStrategy = _networkStrategies.GetStrategy(network);
            var apiGateway = networkStrategy.GetApiGateway();
            var tokenUrl = $"{apiGateway}/nfts/{tokenIdentifier}";
            var tokenRaw = await client.GetStringAsync(tokenUrl);
            var token = JsonConvert.DeserializeObject<NftToken>(tokenRaw);
            return token;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<EsdtToken?> GetEsdtTokenAsync(ElrondNetwork network, string tokenIdentifier)
    {
        try
        {
            var client = new HttpClient();
            var networkStrategy = _networkStrategies.GetStrategy(network);
            var apiGateway = networkStrategy.GetApiGateway();
            var tokenUrl = $"{apiGateway}/tokens/{tokenIdentifier}";
            var tokenRaw = await client.GetStringAsync(tokenUrl);
            var token = JsonConvert.DeserializeObject<EsdtToken>(tokenRaw);
            return token;
        }
        catch (Exception)
        {
            return null;
        }
    }
}