using Kvit.Domain.Entities;
using Kvit.Domain.PersonalEntries;

namespace Kvit.Application.Commands.PersonalEntries
{
    public sealed record AddPersonalEntryCommand(Guid ClientRequestId, PersonalEntryKind Kind, PersonalEntryInput Input);
}
