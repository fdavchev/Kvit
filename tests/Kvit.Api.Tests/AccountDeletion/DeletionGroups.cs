using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;

namespace Kvit.Api.Tests.AccountDeletion
{
    public static class DeletionGroups
    {
        public static async Task<DeletionGroup> CreateAsync(ExpensesApp app, SignedInUser owner)
        {
            app.Clock.SetTo(ExpensesApp.ClockStart);
            Guid groupId = await app.CreateGroupAsync(owner);
            Guid ownerRowId = await GroupRows.CurrentMemberIdAsync(app, groupId, owner.UserId);

            return new DeletionGroup(groupId, owner.UserId, ownerRowId);
        }

        public static Task<Guid> AddAccountAsync(ExpensesApp app, DeletionGroup group, SignedInUser person, int joinedMinutesAfterStart)
        {
            return GroupRows.InsertMemberAsync(
                app,
                group.GroupId,
                person.UserId,
                person.Form.DisplayName,
                group.OwnerUserId,
                joinedAt: JoinedAt(joinedMinutesAfterStart));
        }

        public static Task<Guid> AddGuestAsync(ExpensesApp app, DeletionGroup group, GuestMember guest, string name, int joinedMinutesAfterStart)
        {
            return GroupRows.InsertMemberAsync(
                app,
                group.GroupId,
                guest.UserId,
                name,
                group.OwnerUserId,
                joinedAt: JoinedAt(joinedMinutesAfterStart));
        }

        public static Task<Guid> AddPlainNameAsync(ExpensesApp app, DeletionGroup group, string name, int joinedMinutesAfterStart)
        {
            return GroupRows.InsertMemberAsync(
                app,
                group.GroupId,
                null,
                name,
                group.OwnerUserId,
                joinedAt: JoinedAt(joinedMinutesAfterStart));
        }

        public static Task<Guid> AddEndedAccountAsync(ExpensesApp app, DeletionGroup group, SignedInUser person, string endKind, int joinedMinutesAfterStart)
        {
            return GroupRows.InsertMemberAsync(
                app,
                group.GroupId,
                person.UserId,
                person.Form.DisplayName,
                group.OwnerUserId,
                removedAt: JoinedAt(joinedMinutesAfterStart + 1),
                endKind: endKind,
                joinedAt: JoinedAt(joinedMinutesAfterStart));
        }

        public static DateTimeOffset JoinedAt(int minutesAfterStart)
        {
            return ExpensesApp.ClockStart + TimeSpan.FromMinutes(minutesAfterStart);
        }

        public static async Task<Guid> CreateNeutralExpenseAsync(ExpensesApp app)
        {
            SignedInUser neutralOwner = await app.SignUpAsync("Neutral");
            DeletionGroup group = await CreateAsync(app, neutralOwner);

            return await ExpenseRows.InsertExpenseAsync(app, group.GroupId, group.OwnerRowId, neutralOwner.UserId);
        }
    }
}
