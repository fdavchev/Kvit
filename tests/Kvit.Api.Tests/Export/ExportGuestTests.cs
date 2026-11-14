using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportGuestTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FreeName = "Marko";

        [Fact]
        public async Task Export_Guest_ListsOnlyTheGroupTheyJoined()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            ExportBody body = await ExportRequests.GetBodyAsync(guest.Client);

            Assert.Equal([group.GroupId], ExportBody.IdsOf(body.Groups));
        }

        [Fact]
        public async Task Export_Guest_ShowsTheirOwnMembershipAndMarksTheirRowAsYou()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            ExportBody body = await ExportRequests.GetBodyAsync(guest.Client);

            JsonElement exportedGroup = body.GroupWithId(group.GroupId);
            Assert.Equal(guest.RowId, exportedGroup.GetProperty("membership").GetProperty("memberId").GetGuid());
            JsonElement yourRow = Assert.Single(ExportBody.ListOf(exportedGroup, "members"), member => member.GetProperty("isYou").GetBoolean());
            Assert.Equal(guest.RowId, yourRow.GetProperty("memberId").GetGuid());
            Assert.False(yourRow.GetProperty("isPlainName").GetBoolean());
        }

        [Fact]
        public async Task Export_Guest_ShowsOtherMembersOfTheGroupByDisplayNameOnly()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            ExportBody body = await ExportRequests.GetBodyAsync(guest.Client);

            JsonElement exportedGroup = body.GroupWithId(group.GroupId);
            JsonElement ownerRow = ExportBody.WithId(ExportBody.ListOf(exportedGroup, "members"), group.OwnerRowId, "memberId");
            Assert.Equal(GroupsApp.OwnerName, ownerRow.GetProperty("displayName").GetString());
            body.AssertDoesNotMention(group.Owner.Form.Email);
            body.AssertDoesNotMention(group.Member.Form.Email);
        }

        [Fact]
        public async Task Export_Guest_HasNoPersonalBudget()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            ExportBody body = await ExportRequests.GetBodyAsync(guest.Client);

            foreach (string listName in ExportRequests.PersonalBudgetFieldNames)
            {
                Assert.Empty(ExportBody.ListOf(body.PersonalBudget, listName));
            }
        }
    }
}
