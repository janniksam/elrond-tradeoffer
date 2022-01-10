using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Extensions;

namespace Elrond.TradeOffer.Web.BotWorkflows.User;

public record ElrondUser
{
    public ElrondUser(long userId)
    {
        UserId = userId;
    }

    private ElrondUser(long userId, string? address, ElrondNetwork network)
    {
        UserId = userId;
        Address = address;
        Network = network;
    }

    public long UserId { get; }

    public string? Address { get; set; }

    public ElrondNetwork Network { get; set; }

    public static ElrondUser From(DbUser user) => new(user.Id, user.Address, user.Network);
 
    public string ShortedAddress => Address?.TrimMid(30, "...") ?? "Not set";
}