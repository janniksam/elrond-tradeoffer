namespace Elrond.TradeOffer.Web.Models;

public record Token
{
    private const string EgldIdentifier = "EGLD";

    public Token(string name, string identifier, ulong nonce, int decimalPrecision) 
    {
        if (decimalPrecision is < 0 or > 18)
        {
            throw new ArgumentException("Should be between 0 and 18", nameof(decimalPrecision));
        }

        Name = name;
        Identifier = identifier;
        Nonce = nonce;
        DecimalPrecision = decimalPrecision;
    }

    public string Name { get; }

    public string Identifier { get; }

    public ulong Nonce { get; }

    public int DecimalPrecision { get; }

    public bool IsEgld() => Identifier == EgldIdentifier;

    public bool IsNft() => DecimalPrecision == 0 && Nonce != 0;

    public static Token Egld() => new(EgldIdentifier, EgldIdentifier, 0, 18);

    public static Token Esdt(string name, string identifier, int decimalPrecision) => new(name, identifier, 0, decimalPrecision);

    public static Token Nft(string name, string identifier, int decimalPrecision, ulong nonce) => new(name, identifier, nonce, decimalPrecision);

    public override string ToString() => Name;
}