using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class GetBalancesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_MixedExpensesInMkdAndEur_AnswersTheExactMkdBalancesInJoiningOrder()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal(group.RowIdsInJoiningOrder, BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal(BalanceScenario.MkdBalancesInJoiningOrder, BalanceJson.BalanceMinors(mkd));
        }

        [Fact]
        public async Task Get_MixedExpensesInMkdAndEur_AnswersTheExactEurBalancesInJoiningOrder()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement eur = BalanceJson.CurrencyOf(body, "EUR");
            Assert.Equal(group.RowIdsInJoiningOrder, BalanceJson.BalanceMemberIds(eur));
            Assert.Equal(BalanceScenario.EurBalancesInJoiningOrder, BalanceJson.BalanceMinors(eur));
        }

        [Fact]
        public async Task Get_MixedExpensesInMkdAndEur_AnswersMkdBeforeEurAndNothingElse()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD", "EUR"], BalanceJson.CurrencyCodesOf(body));
        }

        [Fact]
        public async Task Get_MixedExpenses_IsNotEveryoneKvit()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.False(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_MixedExpenses_EveryMemberIsInEveryCurrencyAndEveryoneIsCurrent()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.All(
                body.GetProperty("currencies").EnumerateArray(),
                currencyEntry =>
                {
                    Assert.Equal(group.RowIdsInJoiningOrder, BalanceJson.BalanceMemberIds(currencyEntry));
                    Assert.Equal([true, true, true, true], BalanceJson.BalanceIsCurrents(currencyEntry));
                });
        }

        [Fact]
        public async Task Get_Answer_HasExactlyTheDocumentedFieldsAtEveryLevel()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(BalanceJson.ResponseFieldNames, GroupRequests.PropertyNamesOf(body));
            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal(BalanceJson.CurrencyFieldNames, GroupRequests.PropertyNamesOf(mkd));
            Assert.All(mkd.GetProperty("balances").EnumerateArray(), row => Assert.Equal(BalanceJson.BalanceRowFieldNames, GroupRequests.PropertyNamesOf(row)));
            Assert.All(mkd.GetProperty("payments").EnumerateArray(), payment => Assert.Equal(BalanceJson.PaymentFieldNames, GroupRequests.PropertyNamesOf(payment)));
            Assert.NotEmpty(mkd.GetProperty("payments").EnumerateArray());
        }

        [Fact]
        public async Task Get_OnlyAnEurExpense_AnswersOnlyEur()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["EUR"], BalanceJson.CurrencyCodesOf(body));
            Assert.Equal([500L, -500L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "EUR")));
        }

        [Fact]
        public async Task Get_EurExpenseAddedBeforeAMkdOne_StillAnswersMkdFirst()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD", "EUR"], BalanceJson.CurrencyCodesOf(body));
            Assert.Equal([-50_000L, 50_000L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "MKD")));
            Assert.Equal([500L, -500L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, "EUR")));
        }

        [Fact]
        public async Task Get_GroupWithoutExpenses_AnswersNoCurrenciesAndEveryoneIsKvit()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(BalanceJson.ResponseFieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.Equal(0, body.GetProperty("currencies").GetArrayLength());
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_ExpenseThePayerOwesEntirelyThemselves_AnswersZeroBalancesNoPaymentsAndEveryoneIsKvit()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(50_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["MKD"], BalanceJson.CurrencyCodesOf(body));
            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal([0L, 0L], BalanceJson.BalanceMinors(mkd));
            Assert.Empty(BalanceJson.PaymentsOf(mkd));
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }

        [Fact]
        public async Task Get_MemberWhoIsNotInAnyExpense_IsInTheAnswerWithZero()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal(group.RowIdsInJoiningOrder, BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal([50_000L, -50_000L, 0L], BalanceJson.BalanceMinors(mkd));
        }

        [Fact]
        public async Task Get_PlainNameMembers_AreInTheAnswerWithTheirPlainNameAndTheirBalance()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko", "Cveta");
            Guid marko = group.PlainRowIds[0];
            Guid cveta = group.PlainRowIds[1];
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(90_000, ExpenseInputs.Mkd, marko, group.OwnerRowId, marko, cveta));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            JsonElement mkd = BalanceJson.CurrencyOf(body, "MKD");
            Assert.Equal(group.RowIdsInJoiningOrder, BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal(["Ana", "Bojan", "Marko", "Cveta"], BalanceJson.BalanceNames(mkd));
            Assert.Equal([-30_000L, 0L, 60_000L, -30_000L], BalanceJson.BalanceMinors(mkd));
            Assert.Equal([true, true, true, true], BalanceJson.BalanceIsCurrents(mkd));
        }

        [Fact]
        public async Task Get_AccountMembersAfterARename_AreNamedByTheirCurrentDisplayName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            await UserRows.SetDisplayNameAsync(_app, group.Member.UserId, "Bojan Petrov");
            await UserRows.SetDisplayNameAsync(_app, group.Owner.UserId, "Ana Ivanova");

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(["Ana Ivanova", "Bojan Petrov"], BalanceJson.BalanceNames(BalanceJson.CurrencyOf(body, "MKD")));
        }

        [Fact]
        public async Task Get_ByAnotherMember_AnswersTheSameAsForTheOwner()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);

            string forOwner = await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            string forMember = await BalanceRequests.ReadTextAsync(group.Member.Client, group.GroupId);

            Assert.Equal(forOwner, forMember);
        }

        [Fact]
        public async Task Get_BalancesOfAnotherGroupOfTheSameCaller_AreNotMixedIn()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);
            Guid ownerRowInOther = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, group.Owner.UserId);
            await _app.AddExpenseAsync(group.Owner, otherGroupId, ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, ownerRowInOther, ownerRowInOther));

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(0, body.GetProperty("currencies").GetArrayLength());
            Assert.True(body.GetProperty("isEveryoneKvit").GetBoolean());
        }
    }
}
