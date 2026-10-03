using System.Buffers.Text;
using System.Security.Cryptography;
using Kvit.Domain.Interfaces;

namespace Kvit.Infrastructure.Groups
{
    public sealed class InviteTokenGenerator : IInviteTokenGenerator
    {
        private const int TokenBytes = 32;

        public string NewToken()
        {
            return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
        }
    }
}
