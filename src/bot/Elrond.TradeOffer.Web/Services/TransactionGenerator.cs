using System.Text;
using Elrond.TradeOffer.Web.BotWorkflows.Bids;
using Elrond.TradeOffer.Web.BotWorkflows.Offers;
using Elrond.TradeOffer.Web.Extensions;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Network;
using Elrond.TradeOffer.Web.Repositories;
using Erdcsharp.Provider.Dtos;

namespace Elrond.TradeOffer.Web.Services
{
    public class TransactionGenerator : ITransactionGenerator
    {
        private readonly IUserRepository _userManager;
        private readonly IElrondApiService _elrondApiService;
        private readonly INetworkStrategies _networkStrategies;

        public TransactionGenerator(IUserRepository userManager, IElrondApiService elrondApiService, INetworkStrategies networkStrategies)
        {
            _userManager = userManager;
            _elrondApiService = elrondApiService;
            _networkStrategies = networkStrategies;
        }

        public async Task<string> GenerateInitiateTradeUrlAsync(Offer offer, Bid acceptedBid)
        {
            var elrondUser = await _userManager.GetAsync(offer.CreatorUserId, CancellationToken.None);
            if (elrondUser.Address == null)
            {
                throw new ArgumentException("Address of the user needs to be filled.", nameof(offer));
            }

            var networkStrategy = _networkStrategies.GetStrategy(elrondUser.Network);
            var networkConfig = await _elrondApiService.GetNetworkConfigAsync(elrondUser.Network);
            
            var receiver = networkStrategy.GetSmartContractAddress();
            var minGasLimit = networkConfig.Config.erd_min_gas_limit;
            var minGasPrice = networkConfig.Config.erd_min_gas_price;

            TransactionRequest request;
            if (offer.Amount.Token.IsEgld())
            {
                var data = CreateDataInitiateTradeForEgld(acceptedBid);
                request = new TransactionRequest(
                    receiver,
                    offer.Amount,
                    minGasLimit,
                    minGasPrice,
                    data);
            }
            else
            {
                var data = CreateDataInitiateTradeForEsdt(receiver, offer, acceptedBid.Amount);
                request = new TransactionRequest(
                    elrondUser.Address,
                    TokenAmount.From(0, Token.Egld()),
                    minGasLimit,
                    minGasPrice,
                    data);
            }
            
            request.GasLimit = CalculateGasPrice(request, networkConfig);
            return networkStrategy.GetTransactionUrl(request);
        }

        public async Task<string> GenerateFinalizeTradeUrlAsync(Offer offer, Bid acceptedBid)
        {
            var elrondUser = await _userManager.GetAsync(offer.CreatorUserId, CancellationToken.None);
            if (elrondUser.Address == null)
            {
                throw new ArgumentException("Address of the user needs to be filled.", nameof(offer));
            }

            var networkStrategy = _networkStrategies.GetStrategy(elrondUser.Network);
            var networkConfig = await _elrondApiService.GetNetworkConfigAsync(elrondUser.Network);

            var receiver = networkStrategy.GetSmartContractAddress();
            var minGasLimit = networkConfig.Config.erd_min_gas_limit;
            var minGasPrice = networkConfig.Config.erd_min_gas_price;

            TransactionRequest request;
            if (acceptedBid.Amount.Token.IsEgld())
            {
                var data = CreateDataFinalizeTradeForEgld(offer);
                request = new TransactionRequest(
                    receiver,
                    acceptedBid.Amount,
                    minGasLimit,
                    minGasPrice,
                    data);
            }
            else
            {
                var data = CreateDataFinalizeTradeForEsdt(receiver, acceptedBid, offer.Amount);
                request = new TransactionRequest(
                    elrondUser.Address,
                    TokenAmount.From(0, Token.Egld()),
                    minGasLimit,
                    minGasPrice,
                    data);
            }

            request.GasLimit = CalculateGasPrice(request, networkConfig);
            return networkStrategy.GetTransactionUrl(request);
        }

        public async Task<string> GenerateReclaimUrlAsync(Offer offer)
        {
            var elrondUser = await _userManager.GetAsync(offer.CreatorUserId, CancellationToken.None);
            if (elrondUser.Address == null)
            {
                throw new ArgumentException("Address of the user needs to be filled.", nameof(offer));
            }

            var networkStrategy = _networkStrategies.GetStrategy(elrondUser.Network);
            var networkConfig = await _elrondApiService.GetNetworkConfigAsync(elrondUser.Network);
            var receiver = networkStrategy.GetSmartContractAddress();
            var minGasLimit = networkConfig.Config.erd_min_gas_limit;
            var minGasPrice = networkConfig.Config.erd_min_gas_price;
            var data = CreateDataForReclaim(offer);

            var request = new TransactionRequest(receiver, TokenAmount.From(0, Token.Egld()), minGasLimit, minGasPrice, data);
            request.GasLimit = CalculateGasPrice(request, networkConfig);
            return networkStrategy.GetTransactionUrl(request);
        }

        private static string CreateDataForReclaim(Offer offer)
        {
            var offerId =offer.Id.ToHex();
            return $"cancel_offer@{offerId}";
        }

        private static string CreateDataInitiateTradeForEgld(Bid bid)
        {
            var offerId = bid.OfferId.ToHex();
            var tokenWant = bid.Amount.Token.Identifier.ToHex();
            var amountWant = bid.Amount.Value.ToHex();
            var nonceWant = bid.Amount.Token.Nonce.ToHex().EmptyIfZero();
            
            return $"offer@{offerId}@{tokenWant}@{amountWant}@{nonceWant}";
        }

        private static string CreateDataFinalizeTradeForEgld(Offer offer)
        {
            var offerId = offer.Id.ToHex();
            var tokenWant = offer.Amount.Token.Identifier.ToHex();
            var amountWant = offer.Amount.Value.ToHex();
            var nonceWant = offer.Amount.Token.Nonce.ToHex().EmptyIfZero();

            return $"accept_offer@{offerId}@{tokenWant}@{amountWant}@{nonceWant}";
        }

        private static string CreateDataInitiateTradeForEsdt(string scAddress, Offer offer, TokenAmount want)
        {
            var offerId = offer.Id.ToHex();
            var receiver = scAddress.FromBech32ToHex();
            var tokenOffered = offer.Amount.Token.Identifier.ToHex();
            var amountOffered = offer.Amount.Value.ToHex();
            var nonceOffered = offer.Amount.Token.Nonce.ToHex().EmptyIfZero();
            var functionName = "offer".ToHex();
            var tokenWant = want.Token.Identifier.ToHex();
            var amountWant = want.Value.ToHex();
            var nonceWant = want.Token.Nonce.ToHex().EmptyIfZero();

            return $"MultiESDTNFTTransfer@{receiver}@01@{tokenOffered}@{nonceOffered}@{amountOffered}@{functionName}@{offerId}@{tokenWant}@{amountWant}@{nonceWant}";
        }

        private static string CreateDataFinalizeTradeForEsdt(string scAddress, Bid acceptedBid, TokenAmount want)
        {
            var offerId = acceptedBid.OfferId.ToHex();
            var receiver = scAddress.FromBech32ToHex();
            var tokenBid = acceptedBid.Amount.Token.Identifier.ToHex();
            var amountBid = acceptedBid.Amount.Value.ToHex();
            var nonceBid = acceptedBid.Amount.Token.Nonce.ToHex().EmptyIfZero();
            var functionName = "accept_offer".ToHex();
            var tokenWant = want.Token.Identifier.ToHex();
            var amountWant = want.Value.ToHex();
            var nonceWant = want.Token.Nonce.ToHex().EmptyIfZero();

            return $"MultiESDTNFTTransfer@{receiver}@01@{tokenBid}@{nonceBid}@{amountBid}@{functionName}@{offerId}@{tokenWant}@{amountWant}@{nonceWant}";
        }


        private static int CalculateGasPrice(
            TransactionRequest request,
            ConfigDataDto configDataDto)
        {
            var value = configDataDto.Config.erd_min_gas_limit + 6000000;
            if (string.IsNullOrEmpty(request.Data))
                return value;

            var bytes = Encoding.ASCII.GetBytes(request.Data);
            value += configDataDto.Config.erd_gas_per_data_byte * bytes.Length;

            return value;
        }
    }
}
