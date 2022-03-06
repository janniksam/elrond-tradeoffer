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
        string urlFormat;
        if (token.IsEgld())
        {
            return $"{token.Identifier}";
        }
        if (token.IsNft())
        {
            urlFormat = networkStrategy.GetNftUrlFormat();
        }
        else
        {
            urlFormat = networkStrategy.GetTokenUrlFormat();
        }

        var tokenUrl = string.Format(urlFormat, token.Identifier);
        return Create(token.Identifier, tokenUrl);
    }
}