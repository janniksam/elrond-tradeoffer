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

    public async Task<IEnumerable<ElrondToken>> GetBalancesAsync(string address, ElrondNetwork network)
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
            tokens.Add(new ElrondToken(Token.Esdt(token.name, token.identifier, token.decimals), token.balance));
        }
        
        foreach (var token in nftsTokens)
        {
            tokens.Add(new ElrondToken(Token.Nft(token.name, token.identifier, token.decimals, (ulong)token.nonce), token.balance ?? "1"));
        }

        return tokens;
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

    public async Task<bool> IsOfferFinishedInSmartContractAsync(ElrondNetwork network, Guid offerId)
    {
        var networkStrategy = _networkStrategies.GetStrategy(network);

        var client = new HttpClient();
        var provider = new ElrondProvider(client, new ElrondNetworkConfiguration(networkStrategy.Network));

        var queryVmRequestDto = new QueryVmRequestDto
        {
            ScAddress = networkStrategy.GetSmartContractAddress(),
            FuncName = "get_finished_offer",
            Args = new[] { offerId.ToHex() },
        };

        var queryResult = await provider.QueryVm(queryVmRequestDto);
        return queryResult.Data.ReturnCode == "ok" &&
               queryResult.Data.ReturnData.Length > 0 &&
               !string.IsNullOrWhiteSpace(queryResult.Data.ReturnData[0]);
    }
}