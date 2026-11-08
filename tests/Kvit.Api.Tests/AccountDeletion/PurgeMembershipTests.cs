using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeMembershipTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Purge_KeepName_TheMembersRowBecomesAPlainNameWithTheOldDisplayName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await UserRows.SetDisplayNameAsync(_app, group.Member.UserId, "Zorica Petrovska");

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.KeepName);

            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "claimed_at"));
            Assert.Equal("Zorica Petrovska", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "name"));
        }

        [Fact]
        public async Task Purge_FormerMember_TheMembersRowBecomesAPlainNameCalledFormerMember()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "claimed_at"));
            Assert.Equal(DeletionRequests.FormerMemberEnglish, await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "name"));
        }

        [Fact]
        public async Task Purge_FormerMemberWhoUsesMacedonian_TheRowIsCalledPoraneshenClen()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await AuthRequests.ChangeLanguageAsync(group.Member.Client, "mk");

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberMacedonian, await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "name"));
        }

        [Fact]
        public async Task Purge_TheRowKeepsItsIdItsJoinTimeAndWhoAddedIt()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string joinedBefore = await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "joined_at") ?? string.Empty;
            string addedByBefore = await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "added_by_user_id") ?? string.Empty;

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(joinedBefore, await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "joined_at"));
            Assert.Equal(addedByBefore, await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "added_by_user_id"));
            Assert.Equal(group.MemberRowId.ToString(), await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "id"));
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        [InlineData("SetAside")]
        public async Task Purge_EndedMembership_BecomesAPlainNameAndStaysEnded(string endKind)
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser gone = await _app.SignUpAsync("Zorica Petrovska");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            Guid goneRowId = await DeletionGroups.AddEndedAccountAsync(_app, group, gone, endKind, 5);
            string removedAtBefore = await GroupRows.MemberRowValueAsync(_app, goneRowId, "removed_at") ?? string.Empty;

            await DeletionSteps.RequestAndPurgeAsync(_app, gone.Client, gone.UserId, DeletionRequests.KeepName);

            Assert.Null(await GroupRows.MemberRowValueAsync(_app, goneRowId, "user_id"));
            Assert.Equal("Zorica Petrovska", await GroupRows.MemberRowValueAsync(_app, goneRowId, "name"));
            Assert.Equal(endKind, await GroupRows.MemberRowValueAsync(_app, goneRowId, "end_kind"));
            Assert.Equal(removedAtBefore, await GroupRows.MemberRowValueAsync(_app, goneRowId, "removed_at"));
        }

        [Fact]
        public async Task Purge_EndedMembershipOfAFormerMember_IsCalledFormerMemberToo()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser gone = await _app.SignUpAsync("Zorica Petrovska");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            Guid goneRowId = await DeletionGroups.AddEndedAccountAsync(_app, group, gone, "Left", 5);

            await DeletionSteps.RequestAndPurgeAsync(_app, gone.Client, gone.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await GroupRows.MemberRowValueAsync(_app, goneRowId, "name"));
        }

        [Fact]
        public async Task Purge_PersonInTwoGroups_BothRowsBecomePlainNames()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser leaver = await _app.SignUpAsync("Zorica");
            DeletionGroup first = await DeletionGroups.CreateAsync(_app, owner);
            DeletionGroup second = await DeletionGroups.CreateAsync(_app, owner);
            Guid firstRowId = await DeletionGroups.AddAccountAsync(_app, first, leaver, 5);
            Guid secondRowId = await DeletionGroups.AddAccountAsync(_app, second, leaver, 5);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await GroupRows.MemberRowValueAsync(_app, firstRowId, "name"));
            Assert.Equal(DeletionRequests.FormerMemberEnglish, await GroupRows.MemberRowValueAsync(_app, secondRowId, "name"));
            Assert.Equal(0, await DeletionRows.CountOfUserAsync(_app, "group_members", "user_id", leaver.UserId));
        }

        [Fact]
        public async Task Purge_ClaimedRow_BecomesAPlainNameAndLosesItsClaimTime()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            HttpResponseMessage claim = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.PlainRowIds[0]);
            Assert.True(claim.IsSuccessStatusCode);
            Assert.NotNull(await GroupRows.MemberRowValueAsync(_app, group.PlainRowIds[0], "claimed_at"));

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.PlainRowIds[0], "user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.PlainRowIds[0], "claimed_at"));
            Assert.Equal(DeletionRequests.FormerMemberEnglish, await GroupRows.MemberRowValueAsync(_app, group.PlainRowIds[0], "name"));
        }

        [Fact]
        public async Task Purge_TwoFormerMembersInOneGroup_AreCalledFormerMemberAndFormerMember2()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser first = await _app.SignUpAsync("First");
            SignedInUser second = await _app.SignUpAsync("Second");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            Guid firstRowId = await DeletionGroups.AddAccountAsync(_app, group, first, 5);
            Guid secondRowId = await DeletionGroups.AddAccountAsync(_app, group, second, 6);

            await DeletionSteps.RequestAndPurgeAsync(_app, first.Client, first.UserId, DeletionRequests.FormerMember);
            await DeletionSteps.RequestAndPurgeAsync(_app, second.Client, second.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Former member", await GroupRows.MemberRowValueAsync(_app, firstRowId, "name"));
            Assert.Equal("Former member 2", await GroupRows.MemberRowValueAsync(_app, secondRowId, "name"));
        }

        [Fact]
        public async Task Purge_ThreeFormerMembersInOneGroup_AreNumberedUpToThree()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            List<Guid> rowIds = [];
            for (int index = 0; index < 3; index++)
            {
                SignedInUser person = await _app.SignUpAsync($"Person {index}");
                rowIds.Add(await DeletionGroups.AddAccountAsync(_app, group, person, 5 + index));
                await DeletionSteps.RequestAndPurgeAsync(_app, person.Client, person.UserId, DeletionRequests.FormerMember);
            }

            List<string?> names = [];
            foreach (Guid rowId in rowIds)
            {
                names.Add(await GroupRows.MemberRowValueAsync(_app, rowId, "name"));
            }

            Assert.Equal(["Former member", "Former member 2", "Former member 3"], names);
        }

        [Fact]
        public async Task Purge_TwoMacedonianFormerMembers_AreCalledPoraneshenClenAndPoraneshenClen2()
        {
            SignedInUser owner = await _app.SignUpAsync("Ана");
            SignedInUser first = await _app.SignUpAsync("Прв");
            SignedInUser second = await _app.SignUpAsync("Втор");
            await AuthRequests.ChangeLanguageAsync(first.Client, "mk");
            await AuthRequests.ChangeLanguageAsync(second.Client, "mk");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            Guid firstRowId = await DeletionGroups.AddAccountAsync(_app, group, first, 5);
            Guid secondRowId = await DeletionGroups.AddAccountAsync(_app, group, second, 6);

            await DeletionSteps.RequestAndPurgeAsync(_app, first.Client, first.UserId, DeletionRequests.FormerMember);
            await DeletionSteps.RequestAndPurgeAsync(_app, second.Client, second.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Поранешен член", await GroupRows.MemberRowValueAsync(_app, firstRowId, "name"));
            Assert.Equal("Поранешен член 2", await GroupRows.MemberRowValueAsync(_app, secondRowId, "name"));
        }

        [Fact]
        public async Task Purge_FormerMemberWhenAPlainNameFormerMemberAlreadyExists_AppendsTwo()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser leaver = await _app.SignUpAsync("Zorica");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddPlainNameAsync(_app, group, "Former member", 4);
            Guid leaverRowId = await DeletionGroups.AddAccountAsync(_app, group, leaver, 5);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Former member 2", await GroupRows.MemberRowValueAsync(_app, leaverRowId, "name"));
        }

        [Fact]
        public async Task Purge_FormerMemberWhenTheTakenNameDiffersOnlyInCase_AppendsTwo()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser leaver = await _app.SignUpAsync("Zorica");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddPlainNameAsync(_app, group, "former MEMBER", 4);
            Guid leaverRowId = await DeletionGroups.AddAccountAsync(_app, group, leaver, 5);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Former member 2", await GroupRows.MemberRowValueAsync(_app, leaverRowId, "name"));
        }

        [Fact]
        public async Task Purge_FormerMemberWhenAnotherAccountIsCalledFormerMember_AppendsTwo()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser namesake = await _app.SignUpAsync("Former member");
            SignedInUser leaver = await _app.SignUpAsync("Zorica");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddAccountAsync(_app, group, namesake, 4);
            Guid leaverRowId = await DeletionGroups.AddAccountAsync(_app, group, leaver, 5);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Former member 2", await GroupRows.MemberRowValueAsync(_app, leaverRowId, "name"));
        }

        [Fact]
        public async Task Purge_MacedonianFormerMemberWhenThePlainNameIsTaken_AppendsTwo()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser leaver = await _app.SignUpAsync("Зорица");
            await AuthRequests.ChangeLanguageAsync(leaver.Client, "mk");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            await DeletionGroups.AddPlainNameAsync(_app, group, "Поранешен член", 4);
            Guid leaverRowId = await DeletionGroups.AddAccountAsync(_app, group, leaver, 5);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Поранешен член 2", await GroupRows.MemberRowValueAsync(_app, leaverRowId, "name"));
        }

        [Fact]
        public async Task Purge_KeepNameOfAPersonWhoLeftWhenTheirNameIsNowTakenByACurrentMember_AppendsTwo()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser gone = await _app.SignUpAsync("Marko");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            Guid goneRowId = await DeletionGroups.AddEndedAccountAsync(_app, group, gone, "Left", 5);
            Guid currentMarkoRowId = await DeletionGroups.AddPlainNameAsync(_app, group, "Marko", 10);

            await DeletionSteps.RequestAndPurgeAsync(_app, gone.Client, gone.UserId, DeletionRequests.KeepName);

            Assert.Equal("Marko 2", await GroupRows.MemberRowValueAsync(_app, goneRowId, "name"));
            Assert.Equal("Marko", await GroupRows.MemberRowValueAsync(_app, currentMarkoRowId, "name"));
        }

        [Fact]
        public async Task Purge_KeepNameOfAPersonWithACurrentClaimedRowAndASetAsideRow_TheCurrentRowKeepsTheNameAndTheOldOneIsNumbered()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            HttpResponseMessage claim = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.PlainRowIds[0]);
            Assert.True(claim.IsSuccessStatusCode);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.KeepName);

            Assert.Equal(GroupsApp.MemberName, await GroupRows.MemberRowValueAsync(_app, group.PlainRowIds[0], "name"));
            Assert.Equal($"{GroupsApp.MemberName} 2", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "name"));
        }

        [Fact]
        public async Task Purge_TheNamesOfTheCurrentRowsInAGroup_StayDifferentFromEachOtherIgnoringCase()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, owner);
            List<SignedInUser> people = [];
            for (int index = 0; index < 4; index++)
            {
                SignedInUser person = await _app.SignUpAsync($"Person {index}");
                await DeletionGroups.AddAccountAsync(_app, group, person, 5 + index);
                people.Add(person);
            }

            foreach (SignedInUser person in people)
            {
                await DeletionSteps.RequestAndPurgeAsync(_app, person.Client, person.UserId, DeletionRequests.FormerMember);
            }

            List<string> names = await DeletionRows.NamesOfCurrentRowsAsync(_app, group.GroupId);
            Assert.Equal(names.Count, names.Select(name => name.ToUpperInvariant()).Distinct().Count());
        }

        [Fact]
        public async Task Purge_FormerMembersInDifferentGroups_AreEachCalledFormerMemberWithoutANumber()
        {
            SignedInUser owner = await _app.SignUpAsync("Ana");
            SignedInUser first = await _app.SignUpAsync("First");
            SignedInUser second = await _app.SignUpAsync("Second");
            DeletionGroup firstGroup = await DeletionGroups.CreateAsync(_app, owner);
            DeletionGroup secondGroup = await DeletionGroups.CreateAsync(_app, owner);
            Guid firstRowId = await DeletionGroups.AddAccountAsync(_app, firstGroup, first, 5);
            Guid secondRowId = await DeletionGroups.AddAccountAsync(_app, secondGroup, second, 5);

            await DeletionSteps.RequestAndPurgeAsync(_app, first.Client, first.UserId, DeletionRequests.FormerMember);
            await DeletionSteps.RequestAndPurgeAsync(_app, second.Client, second.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Former member", await GroupRows.MemberRowValueAsync(_app, firstRowId, "name"));
            Assert.Equal("Former member", await GroupRows.MemberRowValueAsync(_app, secondRowId, "name"));
        }

        [Fact]
        public async Task Purge_OtherMembersRowsInTheGroup_AreUntouched()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            string ownerRowBefore = await RowTextAsync(group.OwnerRowId);
            string plainRowBefore = await RowTextAsync(group.PlainRowIds[0]);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(ownerRowBefore, await RowTextAsync(group.OwnerRowId));
            Assert.Equal(plainRowBefore, await RowTextAsync(group.PlainRowIds[0]));
        }

        [Fact]
        public async Task Purge_MemberListSeenByTheOwner_ShowsThePersonAsANameOnlyMember()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            JsonElement row = await GroupRequests.MemberRowAsync(group.Owner.Client, group.GroupId, group.MemberRowId);
            Assert.True(row.GetProperty("isNameOnly").GetBoolean());
            Assert.Equal(DeletionRequests.FormerMemberEnglish, row.GetProperty("displayName").GetString());
        }

        private async Task<string> RowTextAsync(Guid memberRowId)
        {
            List<string?> rows = await _app.QueryAsync(
                "SELECT t::text FROM group_members t WHERE t.id = @id",
                new NpgsqlParameter("id", memberRowId));

            return Assert.Single(rows)!;
        }
    }
}
