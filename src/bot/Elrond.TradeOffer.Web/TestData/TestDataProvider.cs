using Elrond.TradeOffer.Web.BotWorkflows.BidsTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.OffersTemporary;
using Elrond.TradeOffer.Web.BotWorkflows.User;
using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Repositories;
using Elrond.TradeOffer.Web.Services;

namespace Elrond.TradeOffer.Web.TestData
{
    public class TestDataProvider : ITestDataProvider
    {
        private const long MyUserId = 1692646156;
        private const long MyChatId = 1692646156;
        private const long OtherUser1Id = 1;
        private const long OtherUser2Id = 2;
        private const long OtherUser1ChatId = 1;

        private static readonly Token LkmexToken = Token.Esdt("LKMEX", "LKMEX", 18);
        private static readonly Token UsdcToken = Token.Esdt("USDC-c76f1f", "USDC-c76f1f", 18);
        private static readonly Token RideToken = Token.Esdt("RIDE-7d18e9", "RIDE-7d18e9", 18);
        private static readonly Token EsdtTestToken = new("ESDTest", "ESDT-103a9a", 0, 18);

        private readonly IOfferRepository _offerRepository;
        private readonly IUserRepository _userManager;
        private readonly IFeatureStatesManager _statesManager;

        public TestDataProvider(
            Func<IOfferRepository> offerManager,
            IUserRepository userManager,
            IFeatureStatesManager statesManager)
        {
            _offerRepository = offerManager();
            _userManager = userManager;
            _statesManager = statesManager;
        }

        public async Task ApplyAsync()
        {
            var myUser = await ApplyUserAsync(MyUserId, "erd1npvd6gwtyvem63vngmjvruk7frlld20fmzkzy0a4f80t3cye75pqw5fwsp");
            var anotherUser = await ApplyUserAsync(OtherUser1Id, "anotherErd");

            await _statesManager.SetDevNetStateAsync(true, MyUserId, CancellationToken.None);
            await _statesManager.SetTestNetStateAsync(true, MyUserId, CancellationToken.None);

            await ApplyOffer(
                myUser, 
                TokenAmount.From(1000000m, LkmexToken), 
                "My offer, no bids", 
                MyChatId);
           
            await ApplyOffer(
                myUser,
                TokenAmount.From(Million(2), LkmexToken), 
                "My offer, 2 bids",
                MyChatId,
                (MyChatId, new TemporaryBid(OtherUser1Id)
                {
                    Token = Token.Egld(),
                    Amount = TokenAmount.From(0.3m, Token.Egld()),
                    BidState = BidState.Created
                }),
                (MyChatId, new TemporaryBid(OtherUser2Id)
                {
                    Token = Token.Egld(),
                    Amount = TokenAmount.From(0.4m, Token.Egld()),
                    BidState = BidState.Created
                }));

            await ApplyOffer(
                anotherUser,
                TokenAmount.From(Million(10), LkmexToken), 
                "Other offer, my bid",
                OtherUser1ChatId,
                (OtherUser1ChatId, new TemporaryBid(OtherUser1Id)
                {
                    Token = UsdcToken,
                    Amount = TokenAmount.From(300m, UsdcToken),
                    BidState = BidState.Created
                }));

            await ApplyOffer(
                anotherUser,
                TokenAmount.From(300, RideToken),
                "Between two other users",
                OtherUser1ChatId,
                (OtherUser1ChatId, new TemporaryBid(OtherUser1Id)
                {
                    Token = UsdcToken,
                    Amount = TokenAmount.From(1000m, UsdcToken),
                    BidState = BidState.Created
                })
            );

            await ApplyOffer(
                anotherUser,
                TokenAmount.From(300, RideToken),
                "Other offer, my chat id.",
                MyChatId
            );

            await ApplyOffer(
                anotherUser,
                TokenAmount.From(0.005m, Token.Egld()),
                "My declined bid",
                OtherUser1ChatId,
                (MyChatId, new TemporaryBid(MyUserId)
                {
                    Token = Token.Egld(),
                    Amount = TokenAmount.From(0.001m, Token.Egld()),
                    BidState = BidState.Declined
                }));

            await ApplyOffer(
                myUser, 
                TokenAmount.From(0.005m, Token.Egld()), 
                "My accepted egld offer, my egld bid",
                MyChatId,
                (MyChatId, new TemporaryBid(MyUserId) 
                { 
                    Token = Token.Egld(),
                    Amount = TokenAmount.From(0.003m, Token.Egld()),
                    BidState = BidState.Accepted
                }));

            await ApplyOffer(
                myUser,
                TokenAmount.From(0.005m, EsdtTestToken),
                "My accepted esdt offer, my egld bid",
                MyChatId,
                (MyChatId, new TemporaryBid(MyUserId)
                {
                    Token = Token.Egld(),
                    Amount = TokenAmount.From(0.002m, Token.Egld()),
                    BidState = BidState.Accepted
                }));

            await ApplyOffer(
                myUser,
                TokenAmount.From(0.003m, Token.Egld()),
                "My accepted egld offer, my esdt bid",
                MyChatId,
                (MyChatId, new TemporaryBid(MyUserId)
                {
                    Token = EsdtTestToken,
                    Amount = TokenAmount.From(0.01m, EsdtTestToken),
                    BidState = BidState.Accepted
                }));
        }

        private async Task ApplyOffer(ElrondUser user, TokenAmount amount, string description, long chatId,
            params (long ChatId, TemporaryBid Bid)[] bids)
        {
            var offerId = await _offerRepository.PlaceAsync(user, new TemporaryOffer
            {
                Token = amount.Token,
                Amount = amount,
                Description = description
            }, chatId, CancellationToken.None);

            foreach (var (bidChatId, bid) in bids)
            {
                bid.OfferId = offerId;
                await _offerRepository.PlaceBidAsync(bid, bidChatId, CancellationToken.None);
            }
        }

        private static decimal Million(decimal input) => input * 1000000;

        private async Task<ElrondUser> ApplyUserAsync(
            long userId, string address, ElrondNetwork network = ElrondNetwork.Devnet, CancellationToken ct = default)
        {
            var user = await _userManager.GetAsync(userId, CancellationToken.None);
            user.Address = address;
            user.Network = network;
            await _userManager.AddOrUpdateAsync(user, ct);
            return user;
        }
    }
}
