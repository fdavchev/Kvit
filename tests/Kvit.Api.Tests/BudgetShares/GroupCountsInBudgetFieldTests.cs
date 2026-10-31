using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.BudgetShares
{
    public class GroupCountsInBudgetFieldTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FieldName = "countsInBudget";

        [Fact]
        public async Task Get_NewlyCreatedGroup_AnswersCountsInBudgetTrueAsABoolean()
        {
            SignedInUser owner = await _app.SignUpAsync();

            HttpResponseMessage created = await GroupRequests.CreateAsync(owner.Client);

            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(created);
            Assert.Equal(JsonValueKind.True, body.GetProperty(FieldName).ValueKind);
        }

        [Fact]
        public async Task Get_OwnerAndMemberBeforeAnyChange_BothAnswerTrue()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            Assert.True(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Owner.Client, setup.GroupId));
            Assert.True(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, setup.GroupId));
        }

        [Fact]
        public async Task Get_AfterTheCallerSwitchedOff_AnswersFalse()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            bool countsInBudget = await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, setup.GroupId);

            Assert.False(countsInBudget);
        }

        [Fact]
        public async Task Get_AfterTheCallerSwitchedOffAndOnAgain_AnswersTrue()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, true);

            bool countsInBudget = await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, setup.GroupId);

            Assert.True(countsInBudget);
        }

        [Fact]
        public async Task Get_OwnerSwitchedOff_AnswersFalseToTheOwnerAndTrueToTheMember()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await BudgetShareSteps.SetAsync(setup.Owner, setup.GroupId, false);

            Assert.False(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Owner.Client, setup.GroupId));
            Assert.True(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, setup.GroupId));
        }

        [Fact]
        public async Task Get_MemberSwitchedOff_AnswersFalseToTheMemberAndTrueToTheOwner()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            Assert.False(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, setup.GroupId));
            Assert.True(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Owner.Client, setup.GroupId));
        }

        [Fact]
        public async Task Get_SwitchedOffInOneGroup_StaysTrueInAnotherGroupOfTheSamePerson()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(setup.Owner);
            await GroupRows.InsertMemberAsync(_app, otherGroupId, setup.Member.UserId, "Bojan", setup.Owner.UserId);
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            Assert.False(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, setup.GroupId));
            Assert.True(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, otherGroupId));
        }

        [Fact]
        public async Task Get_SwitchedOffOnlyInTheOtherGroup_StaysTrueInThisGroup()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(setup.Owner);
            await GroupRows.InsertMemberAsync(_app, otherGroupId, setup.Member.UserId, "Bojan", setup.Owner.UserId);
            await BudgetShareSteps.SetAsync(setup.Member, otherGroupId, false);

            Assert.True(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, setup.GroupId));
            Assert.False(await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, otherGroupId));
        }

        [Fact]
        public async Task Get_GuestMember_AnswersTheFieldWithTrue()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, setup.GroupId, "Gost");

            bool countsInBudget = await BudgetShareSteps.CountsInBudgetOfAsync(guest.Client, setup.GroupId);

            Assert.True(countsInBudget);
        }

        [Fact]
        public async Task Get_GuestMember_AnswersExactlyTheEighteenGroupFields()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, setup.GroupId, "Gost");

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(guest.Client, setup.GroupId));

            Assert.Equal(GroupRequests.GroupFieldNames, GroupRequests.PropertyNamesOf(body));
        }

        [Fact]
        public async Task Get_PersonWhoClaimedAPlainName_AnswersTheSwitchOfTheClaimedRowAndNotOfTheRowSetAside()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);
            HttpResponseMessage claimed = await GroupRequests.ClaimAsync(setup.Member.Client, setup.GroupId, plainId);
            Assert.Equal(HttpStatusCode.NoContent, claimed.StatusCode);

            bool countsInBudget = await BudgetShareSteps.CountsInBudgetOfAsync(setup.Member.Client, setup.GroupId);

            Assert.Equal(await BudgetShareSteps.MemberCountsAsync(_app, plainId), countsInBudget);
            Assert.True(countsInBudget);
        }

        [Fact]
        public async Task ListMembers_AfterSomeoneSwitchedOff_NoRowAndNoPartOfTheAnswerHasTheField()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);
            await BudgetShareSteps.SetAsync(setup.Owner, setup.GroupId, false);

            JsonElement body = await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId);

            Assert.DoesNotContain("ountsInBudget", body.GetRawText(), StringComparison.OrdinalIgnoreCase);
            Assert.All(
                body.GetProperty("members").EnumerateArray(),
                row => Assert.Equal(GroupRequests.MemberFieldNames, GroupRequests.PropertyNamesOf(row)));
        }

        [Fact]
        public async Task ListMembers_SeenByTheMemberWhoSwitchedOff_HasNoFieldOnTheirOwnRowEither()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            JsonElement row = await GroupRequests.MemberRowAsync(setup.Member.Client, setup.GroupId, setup.MemberRowId);

            Assert.False(row.TryGetProperty(FieldName, out _));
        }

        [Fact]
        public async Task GroupList_AfterSwitchingOff_RowsDoNotHaveTheField()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await BudgetShareSteps.SetAsync(setup.Member, setup.GroupId, false);

            HttpResponseMessage response = await GroupRequests.ListAsync(setup.Member.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.DoesNotContain("ountsInBudget", body.GetRawText(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
