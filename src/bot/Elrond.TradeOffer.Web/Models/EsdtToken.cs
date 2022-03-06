// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618
namespace Elrond.TradeOffer.Web.Models;

public class EsdtToken
{
    public string identifier { get; set; }
    public string name { get; set; }
    public string ticker { get; set; }
    public string owner { get; set; }
    public string minted { get; set; }
    public string burnt { get; set; }
    public int decimals { get; set; }
    public bool isPaused { get; set; }
    public bool canUpgrade { get; set; }
    public bool canMint { get; set; }
    public bool canBurn { get; set; }
    public bool canChangeOwner { get; set; }
    public bool canPause { get; set; }
    public bool canFreeze { get; set; }
    public bool canWipe { get; set; }
    public string? balance { get; set; }
}