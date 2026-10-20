using Kvit.Domain.Entities;
using Kvit.Domain.PersonalEntries;

namespace Kvit.Application.Commands.PersonalEntries
{
    public sealed record EditPersonalEntryCommand(Guid EntryId, PersonalEntryKind Kind, PersonalEntryInput Input);
}
