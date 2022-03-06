using System.Numerics;
using Elrond.TradeOffer.Web.Network;
using Elrond.TradeOffer.Web.Utils;
using Erdcsharp.Domain.Exceptions;

namespace Elrond.TradeOffer.Web.Models
{
    public record TokenAmount
    {
        public Token Token { get; }

        public BigInteger Value { get; }

        private TokenAmount(string value, Token token)
        {
            Token = token;
            Value = BigInteger.Parse(value);
            if (Value.Sign == -1)
            {
                throw new InvalidTokenAmountException(value);
            }
        }

        private TokenAmount(BigInteger value, Token token)
        {
            Token = token;
            Value = value;
        }

        public string ToCurrencyString()
        {
            return ToDenominated().TrimEnd('0').TrimEnd('.');
        }

        public string ToCurrencyStringWithIdentifier()
        {
            return $"{ToCurrencyString()} {Token.Identifier}";
        }

        public string ToCurrencyStringWithName()
        {
            return $"{ToCurrencyString()} {Token.Name}";
        }

        public string ToHtmlUrl(INetworkStrategy networkStrategy)
        {
            var tokenUrl = Token.ToHtmlLink(networkStrategy);
            return $"{ToCurrencyString()} {tokenUrl}";
        }

        private string ToDenominated()
        {
            var str1 = Value.ToString().PadLeft(Token.DecimalPrecision, '0');
            var num1 = str1.Length - Token.DecimalPrecision;
            var num2 = num1 < 0 ? 0 : num1;
            var str2 = str1.Substring(num2, Token.DecimalPrecision);
            return (num2 == 0 ? "0" : str1[..num2]) + "." + str2;
        }

        public override string ToString() => Value.ToString();
        
        public static TokenAmount From(string value, Token? token = null)
        {
            token ??= Token.Egld();
            return new TokenAmount(value, token);
        }

        public static TokenAmount From(decimal value, Token? token = null)
        {
            token ??= Token.Egld();

            var (numerator, denominator) = Fraction(value);

            var bigInteger = BigInteger.One;
            for (var i = 0; i < token.DecimalPrecision; i++)
            {
                bigInteger *= 10;
            }
            bigInteger = bigInteger * numerator / denominator;
            return new TokenAmount(bigInteger, token);
        }
        private static (BigInteger numerator, BigInteger denominator) Fraction(decimal d)
        {
            var bits = decimal.GetBits(d);
            var numerator = (1 - ((bits[3] >> 30) & 2)) *
                            unchecked(((BigInteger)(uint)bits[2] << 64) |
                                      ((BigInteger)(uint)bits[1] << 32) |
                                      (uint)bits[0]);
            var denominator = BigInteger.Pow(10, (bits[3] >> 16) & 0xff);
            return (numerator, denominator);
        }
    }
}
