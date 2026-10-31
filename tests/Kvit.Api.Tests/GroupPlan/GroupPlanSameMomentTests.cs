using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class GroupPlanSameMomentTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Repetitions = 20;
        private const string InMkd = "200000:MKD";
        private const string InEur = "60000:EUR";

        [Fact]
        public async Task Put_TwoDifferentPlansAtTheSameMoment_BothAnswer204AndOneWholePlanStays()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                HttpResponseMessage[] responses = await Task.WhenAll(
                    GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd),
                    GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur));

                Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
                Assert.Contains(await GroupPlanRows.PlanOfAsync(_app, group.GroupId), new[] { InMkd, InEur });
            }
        }

        [Fact]
        public async Task Put_ManyEqualPlansAtTheSameMoment_AllAnswer204AndThePlanIsTheGivenOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                HttpResponseMessage[] responses = await Task.WhenAll(
                    GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd),
                    GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd),
                    GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd));

                Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
                Assert.Equal(InMkd, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
                await GroupPlanRows.SetAsync(_app, group.GroupId, null, null);
            }
        }

        [Fact]
        public async Task PutAndDelete_RepeatedAtTheSameMoment_NeverAnswerAnErrorAndLeaveNoPlanOrOneWholePlan()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                HttpResponseMessage[] responses = await Task.WhenAll(
                    GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd),
                    GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId),
                    GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur),
                    GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId));

                Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
                Assert.Contains(await GroupPlanRows.PlanOfAsync(_app, group.GroupId), new[] { GroupPlanRows.NoPlan, InMkd, InEur });
            }
        }

        [Fact]
        public async Task Delete_ManyRemovalsAtTheSameMoment_AllAnswer204AndLeaveNoPlan()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                await GroupPlanRows.SetAsync(_app, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

                HttpResponseMessage[] responses = await Task.WhenAll(
                    GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId),
                    GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId),
                    GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId));

                Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
                Assert.Equal(GroupPlanRows.NoPlan, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
            }
        }
    }
}
