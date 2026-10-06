using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class GroupRoster(IReadOnlyList<NamedMember> _rows)
    {
        public Result<NamedMember> FindCurrent(Guid memberId)
        {
            return Find(memberId, row => row.Member.RemovedAt is null);
        }

        public Result<NamedMember> FindCurrentPlainName(Guid memberId)
        {
            return Find(memberId, row => row.Member.RemovedAt is null && row.Member.UserId is null);
        }

        public Result<NamedMember> FindRemoved(Guid memberId)
        {
            return Find(memberId, row => row.Member.EndKind == MemberEndKind.Removed);
        }

        public NamedMember RowOf(Guid memberId)
        {
            return _rows.SingleOrDefault(row => row.Member.Id == memberId)
                ?? throw new InvalidOperationException($"Member {memberId} is not a row of this group's roster, but something in the group points at it.");
        }

        public Result<IReadOnlyList<SplitInput>> CurrentInJoiningOrder(IReadOnlyList<SplitInput> inputs)
        {
            foreach (SplitInput input in inputs)
            {
                Result<NamedMember> row = FindCurrent(input.MemberId);
                if (!row.IsSuccess)
                {
                    return row.ToFailure<IReadOnlyList<SplitInput>>();
                }
            }

            return Result.Ok<IReadOnlyList<SplitInput>>(InJoiningOrder(inputs, input => input.MemberId));
        }

        public IReadOnlyList<T> InJoiningOrder<T>(IEnumerable<T> items, Func<T, Guid> memberIdOf)
        {
            return [.. items.OrderBy(item => RowOf(memberIdOf(item)).Member.JoinedAt).ThenBy(memberIdOf)];
        }

        public NamedMember? CurrentRowOf(Guid userId)
        {
            return _rows.SingleOrDefault(row => row.Member.UserId == userId && row.Member.RemovedAt is null);
        }

        public NamedMember? LatestEndedRowOf(Guid userId, MemberEndKind endKind)
        {
            return _rows
                .Where(row => row.Member.UserId == userId && row.Member.EndKind == endKind)
                .MaxBy(row => row.Member.RemovedAt);
        }

        public bool HasAnyRowOf(Guid userId)
        {
            return _rows.Any(row => row.Member.UserId == userId);
        }

        public Result CheckNameFree(string name)
        {
            if (_rows.Any(row => row.Member.RemovedAt is null && string.Equals(row.DisplayName, name, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure($"There is already a {name} in this group.", ResultCodes.MEMBER_NAME_TAKEN);
            }

            return Result.Ok();
        }

        private Result<NamedMember> Find(Guid memberId, Func<NamedMember, bool> isWanted)
        {
            NamedMember? row = _rows.SingleOrDefault(candidate => candidate.Member.Id == memberId);
            if (row is null || !isWanted(row))
            {
                return GroupMember.NotFound<NamedMember>(memberId);
            }

            return Result.Ok(row);
        }
    }
}
