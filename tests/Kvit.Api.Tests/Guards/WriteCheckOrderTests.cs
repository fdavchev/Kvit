using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class WriteCheckOrderTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(GuardedWrites.EveryNameInEveryRefusingState), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByAStranger_Answers404GroupNotFoundBeforeTheStateIsChecked(string name, string state)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named(name);
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.ApplyAsync(_app, group, write, state);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);

            HttpResponseMessage response = await send(stranger.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(GuardedWrites.NamesWithACodeForAnotherMemberInEveryRefusingState), MemberType = typeof(GuardedWrites))]
        public async Task Write_ByAMemberWhoMayNotDoIt_Answers403TheExistingCodeBeforeTheStateIsChecked(string name, string state)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named(name);
            SendWrite send = await write.PrepareAsync(_app, group);
            await GuardStates.ApplyAsync(_app, group, write, state);
            ClosingFacts before = await ClosingFacts.ReadAsync(_app, group.GroupId);

            HttpResponseMessage response = await send(group.Third.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, write.CodeWhenAnotherMemberTries!);
            Assert.Equal(before, await ClosingFacts.ReadAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData(GuardedWrites.ClosingState)]
        [InlineData(GuardedWrites.FinishedState)]
        public async Task RecordPayment_ByAMemberWhoIsNeitherPayerNorReceiver_Answers403SettlementNotAllowedBeforeTheStateIsChecked(string state)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            GuardedWrite write = GuardedWrites.Named("RecordPayment");
            await GuardStates.ApplyAsync(_app, group, write, state);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Third.Client, group.GroupId, group.MemberRowId, group.OwnerRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Mkd);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(0, await SettlementRows.CountAsync(_app, group.GroupId));
        }
    }
}
