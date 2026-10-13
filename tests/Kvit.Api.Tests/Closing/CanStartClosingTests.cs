using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class CanStartClosingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task CanStartClosing_OwnerOfAnOpenGroupWhereEveryoneIsKvit_IsTrue()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Assert.True(await CanStartAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_OwnerOfAGroupWithOnlyPlainNames_IsTrue()
        {
            LoneOwnerGroup group = await ClosingSteps.CreateGroupWithPlainNamesOnlyAsync(_app, "Marko", "Cveta");

            Assert.True(await CanStartAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_AMemberWhoIsNotTheOwner_IsFalseEvenWhenEveryoneIsKvit()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Assert.False(await CanStartAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_WhenSomeoneOwesMoney_IsFalseUntilThePaymentIsConfirmed()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            bool whileOwing = await CanStartAsync(group.Owner, group.GroupId);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            bool afterPaying = await CanStartAsync(group.Owner, group.GroupId);

            Assert.False(whileOwing);
            Assert.True(afterPaying);
        }

        [Fact]
        public async Task CanStartClosing_WhenAPaymentIsPendingAndEveryBalanceIsZero_IsFalse()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            Assert.False(await CanStartAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_WhenThePendingPaymentWasRejected_IsTrue()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.RejectedStatus);

            Assert.True(await CanStartAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_OnlyEurIsUnsettled_IsFalse()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, ExpenseInputs.Eur);

            Assert.False(await CanStartAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_OneBill_IsFalseEvenWhenEveryoneIsKvit()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await ClosingSteps.CreateEmptyOneBillAsync(_app, owner);

            Assert.False(await CanStartAsync(owner, groupId));
        }

        [Fact]
        public async Task CanStartClosing_WhileTheGroupIsClosing_IsFalse()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            Assert.False(await CanStartAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_WhenTheGroupIsFinished_IsFalse()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            Assert.False(await CanStartAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_WhenAClosingExpiredWithoutAnswers_IsFalseBecauseTheGroupCountsAsFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(ClosingSteps.ClosingWindow);

            Assert.False(await CanStartAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_AfterAnObjection_IsTrueAgain()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ObjectAsync(group.Member, group.GroupId);

            Assert.True(await CanStartAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task CanStartClosing_AfterAnUnlock_IsTrueAgain()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            Assert.True(await CanStartAsync(group.Owner, group.GroupId));
        }

        private static async Task<bool> CanStartAsync(SignedInUser viewer, Guid groupId)
        {
            JsonElement body = await ClosingSteps.ReadGroupAsync(viewer, groupId);

            return body.GetProperty("canStartClosing").GetBoolean();
        }
    }
}
