using System.Net;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public static class CleanupSteps
    {
        public const string PlainMemberName = "Marko";

        public static readonly TimeSpan SoftDeletedRowLimit = TimeSpan.FromDays(35);
        public static readonly TimeSpan DeletedGroupLimit = TimeSpan.FromDays(60);
        public static readonly TimeSpan OneHour = TimeSpan.FromHours(1);
        public static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        public static async Task<Guid> AddExpenseAsync(CleanupApp app, ExpenseGroup group)
        {
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            return await app.AddExpenseAsync(group.Owner, group.GroupId, input);
        }

        public static async Task<Guid> AddExpenseDeletedAgoAsync(CleanupApp app, ExpenseGroup group, TimeSpan deletedAgo)
        {
            Guid expenseId = await AddExpenseAsync(app, group);
            HttpResponseMessage deleted = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            await CleanupRows.SetExpenseDeletedAtAsync(app, expenseId, group.Owner.UserId, app.Now - deletedAgo);

            return expenseId;
        }

        public static Task<Guid> RecordSettlementAsync(ExpenseGroup group)
        {
            return SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId);
        }

        public static async Task<Guid> RecordSettlementDeletedAgoAsync(CleanupApp app, ExpenseGroup group, TimeSpan deletedAgo)
        {
            Guid settlementId = await RecordSettlementAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await CleanupRows.SetSettlementDeletedAtAsync(app, settlementId, group.Owner.UserId, app.Now - deletedAgo);

            return settlementId;
        }

        public static Task<Guid> AddPersonalEntryAsync(SignedInUser user)
        {
            return PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
        }

        public static async Task<Guid> AddPersonalEntryDeletedAgoAsync(CleanupApp app, SignedInUser user, TimeSpan deletedAgo)
        {
            Guid entryId = await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            await PersonalEntrySteps.SetDeletedAtAsync(app, entryId, app.Now - deletedAgo);

            return entryId;
        }

        public static async Task<Guid> AddIncomeDeletedAgoAsync(CleanupApp app, SignedInUser user, TimeSpan deletedAgo)
        {
            Guid entryId = await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of());
            await PersonalEntrySteps.SetDeletedAtAsync(app, entryId, app.Now - deletedAgo);

            return entryId;
        }

        public static Task SetGroupDeletedAgoAsync(CleanupApp app, ExpenseGroup group, TimeSpan deletedAgo)
        {
            return CleanupRows.SetGroupDeletedAtAsync(app, group.GroupId, group.Owner.UserId, app.Now - deletedAgo);
        }

        public static async Task<ExpenseGroup> CreateGroupDeletedAgoAsync(CleanupApp app, TimeSpan deletedAgo)
        {
            ExpenseGroup group = await app.CreateExpenseGroupAsync();
            await SetGroupDeletedAgoAsync(app, group, deletedAgo);

            return group;
        }

        public static async Task<FullGroup> CreateFullGroupAsync(CleanupApp app)
        {
            ExpenseGroup group = await app.CreateExpenseGroupAsync(PlainMemberName);
            Guid ownerExpenseId = await AddExpenseAsync(app, group);
            ExpenseInput memberInput = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId);
            Guid memberExpenseId = await app.AddExpenseAsync(group.Member, group.GroupId, memberInput);
            Guid settlementId = await RecordSettlementAsync(group);
            await ClosingResponseRows.InsertAsync(app, group.GroupId, group.MemberRowId);
            Guid memberEntryId = await AddPersonalEntryAsync(group.Member);
            await DuplicateAnswerRows.InsertAsync(app, group.Member.UserId, memberEntryId, ownerExpenseId);

            return new FullGroup(group, ownerExpenseId, memberExpenseId, settlementId, memberEntryId);
        }

        public static async Task AddOneDueRowOfEveryKindAsync(CleanupApp app)
        {
            await CreateAccountRequestingDeletionAsync(app);
            app.Clock.Advance(DeletionRequests.GraceTime);
            ExpenseGroup group = await app.CreateExpenseGroupAsync();
            await AddExpenseDeletedAgoAsync(app, group, SoftDeletedRowLimit);
            await RecordSettlementDeletedAgoAsync(app, group, SoftDeletedRowLimit);
            await AddPersonalEntryDeletedAgoAsync(app, group.Owner, SoftDeletedRowLimit);
            await CreateGroupDeletedAgoAsync(app, DeletedGroupLimit);
            await CleanupRows.InsertUsageEventAsync(app, group.Owner.UserId, "GroupCreated", app.Now.AddMonths(-12) - OneSecond);
        }

        public static async Task<SignedInUser> CreateAccountRequestingDeletionAsync(CleanupApp app, string nameChoice = DeletionRequests.KeepName)
        {
            SignedInUser user = await app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, nameChoice);

            return user;
        }
    }
}
