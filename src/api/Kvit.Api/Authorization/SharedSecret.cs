using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Kvit.Api.Authorization
{
    public static class SharedSecret
    {
        public static bool IsCarriedBy(StringValues sentSecret, byte[] secretBytes)
        {
            if (sentSecret.Count != 1)
            {
                return false;
            }

            byte[] sentBytes = Encoding.UTF8.GetBytes(sentSecret[0] ?? string.Empty);
            return CryptographicOperations.FixedTimeEquals(sentBytes, secretBytes);
        }
    }
}
