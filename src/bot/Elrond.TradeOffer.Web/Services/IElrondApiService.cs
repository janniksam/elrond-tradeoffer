using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Models;
using Erdcsharp.Provider.Dtos;

namespace Elrond.TradeOffer.Web.Services;

public interface IElrondApiService
{
    Task<IEnumerable<ElrondToken>> GetBalancesAsync(string address, ElrondNetwork network);
    Task<AccountDto> GetAccountAsync(ElrondNetwork network, string address);
    Task<ConfigDataDto> GetNetworkConfigAsync(ElrondNetwork network);
    Task<bool> IsOfferInSmartContractAsync(ElrondNetwork network, Guid offerId);
    Task<bool> IsOfferFinishedInSmartContractAsync(ElrondNetwork network, Guid offerId);
}