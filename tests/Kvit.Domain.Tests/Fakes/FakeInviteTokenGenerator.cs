using Kvit.Domain.Interfaces;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeInviteTokenGenerator(string _token) : IInviteTokenGenerator
    {
        public string NewToken()
        {
            return _token;
        }
    }
}
