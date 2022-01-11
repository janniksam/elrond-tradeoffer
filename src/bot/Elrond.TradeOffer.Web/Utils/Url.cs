using Elrond.TradeOffer.Web.Models;
using Elrond.TradeOffer.Web.Network;

namespace Elrond.TradeOffer.Web.Utils;

public static class HtmlUrl
{
    public static string Create(string display, string url)
    {
        return $"<a href=\"{url}\">{display}</a>";
    }

    public static string ToHtmlLink(this Token token, INetworkStrategy networkStrategy)
    {
        var tokenUrlFormat = networkStrategy.GetTokenUrlFormat();
        var tokenUrl = string.Format(tokenUrlFormat, token.Identifier);
        return Create(token.Identifier, tokenUrl);
    }
}