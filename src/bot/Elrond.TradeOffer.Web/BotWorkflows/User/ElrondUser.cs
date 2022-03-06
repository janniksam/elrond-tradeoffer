using Elrond.TradeOffer.Web.Database;
using Elrond.TradeOffer.Web.Extensions;

namespace Elrond.TradeOffer.Web.BotWorkflows.User;

public record ElrondUser(long UserId, string? Address, ElrondNetwork Network, bool IsAdmin)
{
    public string? Address { get; set; } = Address;

    public ElrondNetwork Network { get; set; } = Network;

    public bool IsAdmin { get; } = IsAdmin;

    public static ElrondUser From(DbUser user) => new(user.Id, user.Address, user.Network, user.IsAdmin);
 
    public string ShortedAddress => Address?.TrimMid(30, "...") ?? "Not set";
}