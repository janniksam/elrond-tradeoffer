using Elrond.TradeOffer.Web.Extensions;

namespace Elrond.TradeOffer.Web.Models;

public record Token
{
    private const string EgldTicker = "EGLD";

    public Token(
        string name,
        string ticker, 
        ulong nonce,
        int decimalPrecision) 
    {
        if (decimalPrecision is < 0 or > 18)
        {
            throw new ArgumentException("Should be between 0 and 18", nameof(decimalPrecision));
        }

        Name = name;
        Ticker = ticker;
        Nonce = nonce;
        DecimalPrecision = decimalPrecision;
        Identifier = Nonce == 0 ? Ticker : $"{Ticker}-{Nonce.ToHex()}";
    }

    public string Name { get; }

    public string Ticker { get; }
    
    public ulong Nonce { get; }

    public int DecimalPrecision { get; }

    public string Identifier { get; }

    public bool IsEgld() => Ticker == EgldTicker;

    public bool IsNft() => DecimalPrecision == 0 && Nonce != 0;

    public static Token Egld() => new(EgldTicker, EgldTicker, 0, 18);

    public static Token Esdt(string name, string ticker, int decimalPrecision) => new(name, ticker, 0, decimalPrecision);

    public static Token Nft(string name, string ticker, int decimalPrecision, ulong nonce) => new(name, ticker, nonce, decimalPrecision);

    public override string ToString()
    {
        if (IsEgld())
        {
            return Name;
        }

        return $"{Name} ({Ticker})";
    }
}