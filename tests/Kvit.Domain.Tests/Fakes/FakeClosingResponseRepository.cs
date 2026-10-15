using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeClosingResponseRepository : IClosingResponseRepository
    {
        private readonly List<ClosingResponse> _responses = [];

        public IReadOnlyList<ClosingResponse> Stored => _responses;

        public void Seed(ClosingResponse response)
        {
            _responses.Add(response);
        }

        public void Add(ClosingResponse response)
        {
            _responses.Add(response);
        }

        public Task<IReadOnlyList<ClosingResponse>> ResponsesOfAsync(Guid groupId, CancellationToken cancellationToken)
        {
            IReadOnlyList<ClosingResponse> responses = [.. _responses.Where(response => response.GroupId == groupId)];

            return Task.FromResult(responses);
        }

        public Task RemoveAllOfAsync(Guid groupId, CancellationToken cancellationToken)
        {
            _responses.RemoveAll(response => response.GroupId == groupId);

            return Task.CompletedTask;
        }
    }
}
