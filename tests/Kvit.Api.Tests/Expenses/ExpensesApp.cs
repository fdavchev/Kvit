using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Postgres;
using Kvit.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ExpensesApp : GroupsApp
    {
        public static readonly DateTimeOffset ClockStart = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        public static readonly TimeSpan JoiningGap = TimeSpan.FromMinutes(1);

        public ExpensesApp(PostgresFixture postgres)
            : base(postgres)
        {
            Clock = new AdjustableClock(ClockStart);
            RateSource = new ScriptedExchangeRateSource();
            _factory.AdditionalTestServices = services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
                services.RemoveAll<IExchangeRateSource>();
                services.AddSingleton<IExchangeRateSource>(RateSource);
            };
        }

        public AdjustableClock Clock { get; }

        public ScriptedExchangeRateSource RateSource { get; }

        public async Task<ExpenseGroup> CreateExpenseGroupAsync(params string[] plainNames)
        {
            SignedInUser owner = await SignUpAsync(OwnerName);
            SignedInUser member = await SignUpAsync(MemberName);
            Guid groupId = await CreateGroupAsync(owner);
            Guid ownerRowId = await GroupRows.CurrentMemberIdAsync(this, groupId, owner.UserId);
            DateTimeOffset ownerJoinedAt = Clock.GetUtcNow();
            Guid memberRowId = await GroupRows.InsertMemberAsync(this, groupId, member.UserId, member.Form.DisplayName, owner.UserId, joinedAt: ownerJoinedAt + JoiningGap);
            List<Guid> plainRowIds = [];
            for (int index = 0; index < plainNames.Length; index++)
            {
                DateTimeOffset joinedAt = ownerJoinedAt + (JoiningGap * (index + 2));
                plainRowIds.Add(await GroupRows.InsertMemberAsync(this, groupId, null, plainNames[index], owner.UserId, joinedAt: joinedAt));
            }

            return new ExpenseGroup(groupId, owner, ownerRowId, member, memberRowId, plainRowIds);
        }

        public async Task<Guid> AddExpenseAsync(SignedInUser adder, Guid groupId, ExpenseInput input, Guid? clientRequestId = null)
        {
            HttpResponseMessage response = await ExpenseRequests.AddAsync(adder.Client, groupId, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        public async Task<JsonElement> ReadExpenseAsync(SignedInUser viewer, Guid groupId, Guid expenseId)
        {
            HttpResponseMessage response = await ExpenseRequests.GetAsync(viewer.Client, groupId, expenseId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }

        public async Task<(Guid GroupId, Guid ExpenseId)> CreateOneBillAsync(SignedInUser caller, OneBillInput input, Guid? clientRequestId = null)
        {
            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            return (body.GetProperty("groupId").GetGuid(), body.GetProperty("expenseId").GetGuid());
        }

        public async Task AssertNothingWrittenForAsync(SignedInUser caller)
        {
            Assert.Equal(0, await GroupRows.CountGroupsOwnedByAsync(this, caller.UserId));
            Assert.Equal(0, await ExpenseRows.CountExpensesCreatedByAsync(this, caller.UserId));
            Assert.Equal(0, await ExpenseRows.CountEventsByActorAsync(this, caller.UserId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(this, caller.UserId, "GroupCreated"));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(this, caller.UserId, "ExpenseAdded"));
        }

        public async Task<Guid> MemberRowIdByNameAsync(Guid groupId, string name)
        {
            List<string?> ids = await QueryAsync(
                "SELECT id::text FROM group_members WHERE group_id = @group_id AND name = @name",
                new NpgsqlParameter("group_id", groupId),
                new NpgsqlParameter("name", name));

            return Guid.Parse(Assert.Single(ids)!);
        }

        public async Task<Guid> BuiltInCategoryIdAsync(string key)
        {
            List<string?> ids = await QueryAsync(
                "SELECT id::text FROM categories WHERE key = @key AND owner_user_id IS NULL",
                new NpgsqlParameter("key", key));

            return Guid.Parse(Assert.Single(ids)!);
        }

        public async Task PrepareRatesAsync(DateOnly rateDate, decimal mkdPerEur, TimeSpan fetchedAgo)
        {
            await ExecuteAsync(
                "DELETE FROM exchange_rates; "
                + "INSERT INTO exchange_rates (rate_date, mkd_per_eur, fetched_at) VALUES (@rate_date, @mkd_per_eur, @fetched_at)",
                new NpgsqlParameter("rate_date", rateDate),
                new NpgsqlParameter("mkd_per_eur", mkdPerEur),
                new NpgsqlParameter("fetched_at", Clock.GetUtcNow() - fetchedAgo));
            RateSource.Reset();
        }
    }
}
