using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Models;
using Erdcsharp.Provider.Dtos;

namespace Elrond.TradeOffer.Web.Services;

public interface IElrondApiService
{
    Task<IReadOnlyCollection<ElrondToken>> GetBalancesAsync(string address, ElrondNetwork network);
    Task<AccountDto> GetAccountAsync(ElrondNetwork network, string address);
    Task<ConfigDataDto> GetNetworkConfigAsync(ElrondNetwork network);
    Task<bool> IsOfferInSmartContractAsync(ElrondNetwork network, Guid offerId);
    Task<IReadOnlyCollection<(Guid offerId, OfferFinishStatus status)>> IsOfferFinishedInSmartContractAsync(ElrondNetwork network, Guid[] offerIds);
    Task<IReadOnlyCollection<(Guid offerId, bool initiated)>> IsOfferInitiatedInSmartContractAsync(ElrondNetwork network, Guid[] offerIds);
    Task<ElrondToken?> GetTokenAsync(ElrondNetwork elrondUserNetwork, string tokenIdentifier);
}