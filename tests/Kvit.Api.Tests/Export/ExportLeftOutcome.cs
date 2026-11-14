using System.Text.Json;

namespace Kvit.Api.Tests.Export
{
    public sealed record ExportLeftOutcome(ExportBeforeEnd Before, ExportAfterEnd After, ExportBody Body)
    {
        public JsonElement Group => Body.GroupWithId(Before.GroupId);

        public List<JsonElement> Expenses => ExportBody.ListOf(Group, "expenses");

        public List<JsonElement> Payments => ExportBody.ListOf(Group, "payments");

        public List<JsonElement> Members => ExportBody.ListOf(Group, "members");

        public List<JsonElement> Activity => ExportBody.ListOf(Group, "activity");

        public JsonElement Membership => Group.GetProperty("membership");
    }
}
