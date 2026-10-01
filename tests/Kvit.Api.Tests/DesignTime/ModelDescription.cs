using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Api.Tests.DesignTime
{
    public static class ModelDescription
    {
        public static List<string> Of(AppDbContext context)
        {
            return
            [
                .. context.Model.GetEntityTypes()
                    .Where(entityType => entityType.GetTableName() is not null)
                    .Select(entityType => Line(entityType.GetTableName(), entityType.GetProperties().Select(property => property.GetColumnName())))
                    .OrderBy(line => line, StringComparer.Ordinal),
            ];
        }

        private static string Line(string? table, IEnumerable<string?> columns)
        {
            return $"{table}: {string.Join(", ", columns.Order(StringComparer.Ordinal))}";
        }
    }
}
