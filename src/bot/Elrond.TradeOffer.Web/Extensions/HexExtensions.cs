using System.Numerics;
using System.Text;
using Erdcsharp.Cryptography;
using Erdcsharp.Domain.Helper;
using Org.BouncyCastle.Asn1;

namespace Elrond.TradeOffer.Web.Extensions
{
    public static class HexExtensions
    {
        public static string ToHex(this ulong num)
        {
            return num.ToString("x").ToEvenLength();
        }

        public static string ToHex(this BigInteger num)
        {
            return num.ToString("x").ToEvenLength();
        }

        private static string ToEvenLength(this string hexString)
        {
            return hexString.Length % 2 == 0 ? hexString : $"0{hexString}";
        }

        public static string ToHex(this string text)
        {
            var sBuffer = new StringBuilder();
            foreach (var character in text)
            {
                sBuffer.Append(Convert.ToInt32(character).ToString("x"));
            }
            return sBuffer.ToString();
        }

        public static string ToHex(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (var @byte in bytes)
            {
                hex.AppendFormat("{0:x2}", @byte);
            }
            return hex.ToString();
        }

        public static string EmptyIfZero(this string hex)
        {
            return hex == "00" ? string.Empty : hex;
        }

        public static string FromBech32ToHex(this string bech32)
        {
            Bech32Engine.Decode(bech32, out _, out var data);
            var hex = Converter.ToHexString(data);
            return hex;
        }
    }
}
