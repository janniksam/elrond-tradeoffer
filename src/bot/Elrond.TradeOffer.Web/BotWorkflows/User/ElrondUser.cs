using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Extensions;

namespace Elrond.TradeOffer.Web.BotWorkflows.User;

public record ElrondUser
{
    public ElrondUser(long userId)
    {
        UserId = userId;
    }

    private ElrondUser(long userId, string? address, ElrondNetwork network, bool isAdmin)
    {
        UserId = userId;
        Address = address;
        Network = network;
        IsAdmin = isAdmin;
    }

    public long UserId { get; }

    public string? Address { get; set; }

    public ElrondNetwork Network { get; set; }

    public bool IsAdmin { get; }

    public static ElrondUser From(DbUser user) => new(user.Id, user.Address, user.Network, user.IsAdmin);
 
    public string ShortedAddress => Address?.TrimMid(30, "...") ?? "Not set";
}