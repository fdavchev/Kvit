using System.Net;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public static class OwnerToolsCalls
    {
        public const string People = "People";
        public const string Suspend = "Suspend";
        public const string Unsuspend = "Unsuspend";
        public const string EraseName = "EraseName";
        public const string Group = "Group";
        public const string Close = "Close";
        public const string Reopen = "Reopen";
        public const string Log = "Log";

        public static IReadOnlyList<OwnerToolsCall> All { get; } =
        [
            new OwnerToolsCall(People, OwnerToolsTarget.None, (_, _) => $"{OwnerToolsRequests.PeoplePath}?query=ab", (caller, _, _, _) => OwnerToolsRequests.PeopleAsync(caller, "ab"), HttpStatusCode.OK),
            new OwnerToolsCall(Suspend, OwnerToolsTarget.Person, (personId, _) => OwnerToolsRequests.PersonActionPathOf(personId, "suspend"), (caller, personId, _, reason) => OwnerToolsRequests.SuspendAsync(caller, personId, reason), HttpStatusCode.NoContent),
            new OwnerToolsCall(Unsuspend, OwnerToolsTarget.Person, (personId, _) => OwnerToolsRequests.PersonActionPathOf(personId, "unsuspend"), (caller, personId, _, reason) => OwnerToolsRequests.UnsuspendAsync(caller, personId, reason), HttpStatusCode.NoContent),
            new OwnerToolsCall(EraseName, OwnerToolsTarget.Person, (personId, _) => OwnerToolsRequests.PersonActionPathOf(personId, "erase-name"), (caller, personId, _, reason) => OwnerToolsRequests.EraseNameAsync(caller, personId, reason), HttpStatusCode.NoContent),
            new OwnerToolsCall(Group, OwnerToolsTarget.None, (_, groupId) => OwnerToolsRequests.GroupPathOf(groupId), (caller, _, groupId, _) => OwnerToolsRequests.GroupAsync(caller, groupId), HttpStatusCode.OK),
            new OwnerToolsCall(Close, OwnerToolsTarget.Group, (_, groupId) => OwnerToolsRequests.GroupActionPathOf(groupId, "close"), (caller, _, groupId, reason) => OwnerToolsRequests.CloseAsync(caller, groupId, reason), HttpStatusCode.NoContent),
            new OwnerToolsCall(Reopen, OwnerToolsTarget.Group, (_, groupId) => OwnerToolsRequests.GroupActionPathOf(groupId, "reopen"), (caller, _, groupId, reason) => OwnerToolsRequests.ReopenAsync(caller, groupId, reason), HttpStatusCode.NoContent),
            new OwnerToolsCall(Log, OwnerToolsTarget.None, (_, _) => OwnerToolsRequests.LogPath, (caller, _, _, _) => OwnerToolsRequests.LogAsync(caller), HttpStatusCode.OK),
        ];

        public static TheoryData<string> Names => NamesOf(All);

        public static TheoryData<string> NamesWithAReason => NamesOf(All.Where(call => call.Target != OwnerToolsTarget.None));

        public static TheoryData<string> PersonNames => NamesOf(All.Where(call => call.Target == OwnerToolsTarget.Person));

        public static TheoryData<string> GroupNames => NamesOf(All.Where(call => call.Target == OwnerToolsTarget.Group));

        public static TheoryData<string, string> NamesWithEveryInvalidReason()
        {
            TheoryData<string, string> data = [];
            foreach (OwnerToolsCall call in All.Where(call => call.Target != OwnerToolsTarget.None))
            {
                foreach (string reasonJson in InvalidReasonJsons)
                {
                    data.Add(call.Name, reasonJson);
                }
            }

            return data;
        }

        public static OwnerToolsCall Named(string name)
        {
            return All.SingleOrDefault(call => call.Name == name)
                ?? throw new ArgumentException($"No owner tool call is named '{name}'. The names are: {string.Join(", ", All.Select(call => call.Name))}.", nameof(name));
        }

        private static IEnumerable<string> InvalidReasonJsons { get; } =
        [
            "{\"reason\":\"\"}",
            "{\"reason\":\"   \"}",
            "{\"reason\":\"\\t\\r\\n\"}",
            "{\"reason\":null}",
            "{}",
            $"{{\"reason\":\"{new string('x', OwnerToolsRequests.ReasonMaxLength + 1)}\"}}",
            $"{{\"reason\":\"  {new string('x', OwnerToolsRequests.ReasonMaxLength + 1)}  \"}}",
        ];

        private static TheoryData<string> NamesOf(IEnumerable<OwnerToolsCall> calls)
        {
            TheoryData<string> data = [];
            foreach (OwnerToolsCall call in calls)
            {
                data.Add(call.Name);
            }

            return data;
        }
    }
}
