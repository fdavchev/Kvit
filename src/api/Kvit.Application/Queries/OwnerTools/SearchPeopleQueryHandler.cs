using Kvit.Application.Dispatching;
using Kvit.Contracts.OwnerTools;
using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.OwnerTools
{
    public sealed class SearchPeopleQueryHandler(AppDbContext _context) : IQueryHandler<SearchPeopleQuery, IReadOnlyList<OwnerToolsPersonRow>>
    {
        private const int QueryMinLength = 2;
        private const int PeopleMaxCount = 10;
        private const string LikeEscapeCharacter = "\\";

        public async Task<Result<IReadOnlyList<OwnerToolsPersonRow>>> Handle(SearchPeopleQuery query, CancellationToken cancellationToken)
        {
            string text = query.Query?.Trim() ?? string.Empty;
            if (text.Length < QueryMinLength)
            {
                return Result.Ok<IReadOnlyList<OwnerToolsPersonRow>>([]);
            }

            IQueryable<AppUser> matches = Guid.TryParse(text, out Guid personId)
                ? _context.Users.Where(user => user.Id == personId)
                : PeopleWhoseNameOrEmailContains(text);
            List<OwnerToolsPersonRow> people = await OwnerToolsPersonRowsOf(matches).ToListAsync(cancellationToken);

            return Result.Ok<IReadOnlyList<OwnerToolsPersonRow>>(people);
        }

        private IQueryable<AppUser> PeopleWhoseNameOrEmailContains(string text)
        {
            string pattern = $"%{EscapedForLike(text)}%";

            return _context.Users.Where(user =>
                EF.Functions.ILike(user.DisplayName, pattern, LikeEscapeCharacter)
                || (user.Email != null && EF.Functions.ILike(user.Email, pattern, LikeEscapeCharacter)));
        }

        private static string EscapedForLike(string text)
        {
            return text
                .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
                .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
                .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
        }

        private IQueryable<OwnerToolsPersonRow> OwnerToolsPersonRowsOf(IQueryable<AppUser> users)
        {
            IQueryable<GroupMember> currentMembers = _context.CurrentGroupMembers();

            return users
                .AsNoTracking()
                .OrderByDescending(user => user.CreatedAt)
                .ThenByDescending(user => user.Id)
                .Take(PeopleMaxCount)
                .Select(user => new OwnerToolsPersonRow(
                    user.Id,
                    user.DisplayName,
                    user.Email,
                    user.IsGuest,
                    user.SuspendedAt != null,
                    _context.UserRoles.Any(userRole => userRole.UserId == user.Id
                        && _context.Roles.Any(role => role.Id == userRole.RoleId && role.Name == Roles.Admin)),
                    user.DeletionRequestedAt != null && user.PurgedAt == null,
                    user.PurgedAt != null,
                    user.CreatedAt,
                    _context.Groups.Count(group => group.DeletedAt == null
                        && currentMembers.Any(member => member.GroupId == group.Id && member.UserId == user.Id))));
        }
    }
}
