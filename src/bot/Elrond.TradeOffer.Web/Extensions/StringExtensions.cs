namespace Elrond.TradeOffer.Web.Extensions;

public static class StringExtensions
{
    public static string TrimMid(this string str, int maxlengthWithoutReplacement, string replacement)
    {
        if (str.Length <= maxlengthWithoutReplacement)
        {
            return str;
        }

        var rightPart = maxlengthWithoutReplacement / 2;
        var leftPart = maxlengthWithoutReplacement - rightPart;
        return string.Concat(str[..leftPart], replacement, str.Substring(str.Length - rightPart - 1, rightPart));
    }
}