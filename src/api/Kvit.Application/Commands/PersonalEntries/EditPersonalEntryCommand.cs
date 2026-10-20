using Kvit.Domain.PersonalEntries;

namespace Kvit.Application.Commands.PersonalEntries
{
    public sealed record EditPersonalEntryCommand(Guid EntryId, PersonalEntryInput Input);
}
