using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Extensions;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Network;
using Erdcsharp.Configuration;
using Erdcsharp.Provider;
using Erdcsharp.Provider.Dtos;
using Token = Elrond.TradeOffer.Web.Models.Token;

namespace Elrond.TradeOffer.Web.Services;

public class ElrondApiService : IElrondApiService
{
    private readonly INetworkStrategies _networkStrategies;

    public ElrondApiService(INetworkStrategies networkStrategies)
    {
        _networkStrategies = networkStrategies;
    }

    public async Task<IEnumerable<ElrondToken>> GetBalancesAsync(string address, ElrondNetwork network)
    {
        var networkStrategy = _networkStrategies.GetStrategy(network);

        var client = new HttpClient();
        var provider = new ElrondProvider(client, new ElrondNetworkConfiguration(networkStrategy.Network));
        var account = await provider.GetAccount(address);
        var esdtTokens = await provider.GetEsdtTokens(address);
        var tokens = new List<ElrondToken>
        {
            new(Token.Egld(), account.Balance)
        };

        foreach (var (_, value) in esdtTokens.Esdts)
        {
            if (value.Nonce != 0)
            {
                tokens.Add(new ElrondToken(Token.EsdtNft(value.TokenIdentifier, value.TokenIdentifier, value.Nonce), value.Balance));
            }
            else
            {
                tokens.Add(new ElrondToken(Token.Esdt(value.TokenIdentifier, value.TokenIdentifier, 18), value.Balance));
            }
        }

        return tokens;
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