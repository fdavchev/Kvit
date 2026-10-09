using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Expenses;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Expenses
{
    public sealed class CreateOneBill(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IExpenseRepository _expenses,
        ICategoryRepository _categories,
        IActivityEventRepository _activityEvents,
        IUsageEventRepository _usageEvents,
        IInviteTokenGenerator _inviteTokenGenerator,
        TimeProvider _timeProvider)
    {
        public async Task<Result<OneBillCreated>> Execute(Guid userId, string accountName, Guid clientRequestId, OneBillInput input, ExchangeRateSnapshot rate, CancellationToken cancellationToken)
        {
            Expense? earlier = await _expenses.FindByClientRequestIdAsync(clientRequestId, cancellationToken);
            if (earlier is not null)
            {
                return await EarlierBillAsync(earlier, userId, clientRequestId, cancellationToken);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<ExpenseFields> fields = ExpenseFields.Create(
                input.Title,
                input.Note,
                input.AmountMinor,
                input.Currency,
                input.ExpenseDate,
                input.CategoryId,
                input.SplitType,
                DateOnly.FromDateTime(now.UtcDateTime),
                GroupSettings.NameMaxLength);
            if (!fields.IsSuccess)
            {
                return fields.ToFailure<OneBillCreated>();
            }

            Result<string> categoryKey = await _categories.GroupExpenseKeyAsync(fields.Value.CategoryId, cancellationToken);
            if (!categoryKey.IsSuccess)
            {
                return categoryKey.ToFailure<OneBillCreated>();
            }

            string groupName = fields.Value.Title ?? ExpenseFields.TrimToNull(input.GroupName) ?? Group.OneBillNameFor(fields.Value.ExpenseDate);
            Result<Group> created = Group.CreateOneBill(groupName, input.Emoji ?? Group.OneBillDefaultEmoji, fields.Value.Amount.Currency.ToString(), userId, _inviteTokenGenerator.NewToken(), now);
            if (!created.IsSuccess)
            {
                return created.ToFailure<OneBillCreated>();
            }

            Group group = created.Value;
            Result<IReadOnlyList<NamedMember>> people = PeopleOf(group.Id, userId, accountName, input.Names, now);
            if (!people.IsSuccess)
            {
                return people.ToFailure<OneBillCreated>();
            }

            Result<Guid> payer = MemberAt(people.Value, input.PaidByPersonIndex);
            if (!payer.IsSuccess)
            {
                return payer.ToFailure<OneBillCreated>();
            }

            List<SplitInput> inputs = [];
            foreach (PersonShareInput share in input.Shares)
            {
                Result<Guid> member = MemberAt(people.Value, share.PersonIndex);
                if (!member.IsSuccess)
                {
                    return member.ToFailure<OneBillCreated>();
                }

                inputs.Add(new SplitInput(member.Value, share.InputValue));
            }

            Result<IReadOnlyList<SplitShare>> shares = Expense.SplitAmong(new GroupRoster(people.Value), fields.Value, payer.Value, inputs);
            if (!shares.IsSuccess)
            {
                return shares.ToFailure<OneBillCreated>();
            }

            Expense expense = Expense.Create(group.Id, fields.Value, payer.Value, shares.Value, rate, userId, clientRequestId, now);
            _groups.Add(group);
            _activityEvents.Add(ActivityEvent.GroupCreated(group.Id, userId, group.Name, now));
            foreach (NamedMember person in people.Value)
            {
                _members.Add(person.Member);
                if (person.Member.UserId is null)
                {
                    _activityEvents.Add(ActivityEvent.MemberAdded(group.Id, userId, person.Member.Id, person.Member.Name, now));
                }
            }

            _expenses.Add(expense);
            _activityEvents.Add(ActivityEvent.ExpenseAdded(group.Id, userId, expense.Id, expense.Title, expense.AmountMinor, expense.Currency, now));
            _usageEvents.Add(UsageEvent.GroupCreated(userId, now));
            _usageEvents.Add(UsageEvent.ExpenseAdded(userId, now));

            return Result.Ok(new OneBillCreated(group.Id, expense.Id));
        }

        private static Result<IReadOnlyList<NamedMember>> PeopleOf(Guid groupId, Guid userId, string accountName, IReadOnlyList<string> names, DateTimeOffset now)
        {
            List<NamedMember> people = [new NamedMember { Member = GroupMember.ForAccount(groupId, userId, accountName, userId, now), DisplayName = accountName }];
            for (int index = 0; index < names.Count; index++)
            {
                Result<GroupMember> member = GroupMember.ForName(groupId, names[index], userId, now.AddMilliseconds(index + 1));
                if (!member.IsSuccess)
                {
                    return member.ToFailure<IReadOnlyList<NamedMember>>();
                }

                Result nameFree = new GroupRoster(people).CheckNameFree(member.Value.Name);
                if (!nameFree.IsSuccess)
                {
                    return nameFree.ToFailure<IReadOnlyList<NamedMember>>();
                }

                people.Add(new NamedMember { Member = member.Value, DisplayName = member.Value.Name });
            }

            return Result.Ok<IReadOnlyList<NamedMember>>(people);
        }

        private static Result<Guid> MemberAt(IReadOnlyList<NamedMember> people, int personIndex)
        {
            if (personIndex < 0 || personIndex >= people.Count)
            {
                return Result.NotFound<Guid>($"Person {personIndex} is not on this bill; it has people 0 to {people.Count - 1}.", ResultCodes.MEMBER_NOT_FOUND);
            }

            return Result.Ok(people[personIndex].Member.Id);
        }

        private async Task<Result<OneBillCreated>> EarlierBillAsync(Expense earlier, Guid userId, Guid clientRequestId, CancellationToken cancellationToken)
        {
            Result<Group> group = await _groups.FindIncludingDeletedForMemberAsync(earlier.GroupId, userId, cancellationToken);
            if (earlier.CreatedByUserId != userId || !group.IsSuccess || group.Value.Kind != GroupKind.OneBill)
            {
                return Expense.ClientRequestIdUsed<OneBillCreated>(clientRequestId);
            }

            return Result.Ok(new OneBillCreated(earlier.GroupId, earlier.Id));
        }
    }
}
