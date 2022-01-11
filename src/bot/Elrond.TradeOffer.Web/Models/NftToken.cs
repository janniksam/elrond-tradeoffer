// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618
namespace Elrond.TradeOffer.Web.Models;

public class NftToken
{
    public string identifier { get; set; }
    public string collection { get; set; }
    public string attributes { get; set; }
    public int nonce { get; set; }
    public string type { get; set; }
    public string name { get; set; }
    public string creator { get; set; }
    public bool isWhitelistedStorage { get; set; }
    public int decimals { get; set; }
    public string? balance { get; set; }
    public string ticker { get; set; }
}