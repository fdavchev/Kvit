using Kvit.Domain.PersonalEntries;

namespace Kvit.Application.Commands.PersonalEntries
{
    public sealed record AddPersonalEntryCommand(Guid ClientRequestId, PersonalEntryInput Input);
}
