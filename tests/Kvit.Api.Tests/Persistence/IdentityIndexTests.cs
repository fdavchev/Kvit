using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class IdentityIndexTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("ix_users_normalized_email", "false")]
        [InlineData("ix_users_normalized_user_name", "true")]
        [InlineData("ix_roles_normalized_name", "true")]
        public async Task Index_HasTheExpectedUniqueness(string indexName, string expectedIsUnique)
        {
            List<string?> isUnique = await _database.QueryAsync(
                "SELECT i.indisunique::text FROM pg_class c JOIN pg_index i ON i.indexrelid = c.oid WHERE c.relname = @name",
                new NpgsqlParameter("name", indexName));

            Assert.Equal(expectedIsUnique, Assert.Single(isUnique));
        }

        [Theory]
        [InlineData("ix_users_normalized_email", "ON public.users USING btree (normalized_email)")]
        [InlineData("ix_users_normalized_user_name", "ON public.users USING btree (normalized_user_name)")]
        [InlineData("ix_roles_normalized_name", "ON public.roles USING btree (normalized_name)")]
        public async Task Index_CoversTheExpectedTableAndColumn(string indexName, string expectedDefinitionPart)
        {
            List<string?> definitions = await _database.QueryAsync(
                "SELECT indexdef FROM pg_indexes WHERE schemaname = 'public' AND indexname = @name",
                new NpgsqlParameter("name", indexName));

            Assert.Contains(expectedDefinitionPart, Assert.Single(definitions));
        }

        [Fact]
        public async Task TwoUsers_WithTheSameNormalizedUserName_ViolateTheUniqueIndex()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser first = TestUsers.Create();
            AppUser second = TestUsers.Create();
            second.NormalizedUserName = first.NormalizedUserName;
            context.Users.AddRange(first, second);

            PostgresException error = await PostgresErrors.SaveChangesFailingAsync(context);

            Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        }

        [Fact]
        public async Task TwoUsers_WithTheSameNormalizedEmail_AreAllowed()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser first = TestUsers.Create();
            AppUser second = TestUsers.Create();
            second.NormalizedEmail = first.NormalizedEmail;
            context.Users.AddRange(first, second);

            int saved = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, saved);
        }

        [Fact]
        public async Task TwoRoles_WithTheSameNormalizedName_ViolateTheUniqueIndex()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            IdentityRole<Guid> first = new() { Id = Guid.CreateVersion7(), Name = "Admin", NormalizedName = "ADMIN" };
            IdentityRole<Guid> second = new() { Id = Guid.CreateVersion7(), Name = "Admin", NormalizedName = "ADMIN" };
            context.Roles.AddRange(first, second);

            PostgresException error = await PostgresErrors.SaveChangesFailingAsync(context);

            Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        }
    }
}
