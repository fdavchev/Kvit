using Kvit.Application.Persistence;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Application
{
    public class RunOrAnswerIfExpenseChangedTests
    {
        private const string ChangedCode = "EXPENSE_CHANGED_MEANWHILE";

        private readonly FakeUnitOfWork _unitOfWork = new();
        private int _workRuns;
        private int _answerRuns;

        [Fact]
        public async Task FirstCommitDeadlocks_RunsTheWorkOnceMoreAndReturnsItsResultInsteadOfClaimingTheExpenseChanged()
        {
            DeadlockDetectedException deadlock = new(new InvalidOperationException("Deadlock."));
            _unitOfWork.CommitFailures.Enqueue(deadlock);
            Result secondResult = Result.Ok();

            Result actual = await RunAsync(secondResult);

            Assert.Same(secondResult, actual);
            Assert.Equal(2, _workRuns);
            Assert.Equal(0, _answerRuns);
            Assert.Equal([FakeUnitOfWork.OpenedCall, FakeUnitOfWork.CommitFailedCall, FakeUnitOfWork.OpenedCall, FakeUnitOfWork.CommittedCall], _unitOfWork.Calls);
            Assert.Same(deadlock, Assert.Single(_unitOfWork.LoggedClashes));
        }

        [Fact]
        public async Task SecondCommitDeadlocksAgain_ThrowsTheSecondDeadlockForTheGlobalHandlerAndNeverAnswers()
        {
            DeadlockDetectedException first = new(new InvalidOperationException("Deadlock."));
            DeadlockDetectedException second = new(new InvalidOperationException("Deadlock again."));
            _unitOfWork.CommitFailures.Enqueue(first);
            _unitOfWork.CommitFailures.Enqueue(second);

            DeadlockDetectedException actual = await Assert.ThrowsAsync<DeadlockDetectedException>(() => RunAsync(Result.Ok()));

            Assert.Same(second, actual);
            Assert.Equal(2, _workRuns);
            Assert.Equal(0, _answerRuns);
            Assert.Same(first, Assert.Single(_unitOfWork.LoggedClashes));
        }

        [Fact]
        public async Task ExpenseChangedMeanwhile_LogsTheClashAndAnswersFromTheExpenseAsItIsNow()
        {
            ExpenseChangedMeanwhileException changed = new(new InvalidOperationException("Row version changed."));
            _unitOfWork.CommitFailures.Enqueue(changed);

            Result actual = await RunAsync(Result.Ok());

            Assert.Equal(ChangedCode, actual.ErrorCode);
            Assert.Equal(1, _workRuns);
            Assert.Equal(1, _answerRuns);
            Assert.Same(changed, Assert.Single(_unitOfWork.LoggedClashes));
        }

        [Fact]
        public async Task DeadlockThenExpenseChangedMeanwhile_AnswersFromTheExpenseAsItIsNowAndLogsBothClashes()
        {
            DeadlockDetectedException deadlock = new(new InvalidOperationException("Deadlock."));
            ExpenseChangedMeanwhileException changed = new(new InvalidOperationException("Row version changed."));
            _unitOfWork.CommitFailures.Enqueue(deadlock);
            _unitOfWork.CommitFailures.Enqueue(changed);

            Result actual = await RunAsync(Result.Ok());

            Assert.Equal(ChangedCode, actual.ErrorCode);
            Assert.Equal(2, _workRuns);
            Assert.Equal(1, _answerRuns);
            Assert.Equal<Exception>([deadlock, changed], _unitOfWork.LoggedClashes);
        }

        [Fact]
        public async Task SuccessfulCommit_LogsNoClash()
        {
            await RunAsync(Result.Ok());

            Assert.Empty(_unitOfWork.LoggedClashes);
        }

        [Fact]
        public async Task OnceMoreAfterADuplicateRequestId_LogsTheCaughtClashBeforeTheSecondRun()
        {
            ClientRequestIdAlreadySavedException duplicate = new(new InvalidOperationException("Unique violation."));
            _unitOfWork.CommitFailures.Enqueue(duplicate);

            await _unitOfWork.RunInTransactionOnceMoreIfClientRequestIdWasTakenAsync(() => CountWork(Result.Ok()), TestContext.Current.CancellationToken);

            Assert.Same(duplicate, Assert.Single(_unitOfWork.LoggedClashes));
            Assert.Equal(2, _workRuns);
        }

        private Task<Result> RunAsync(Result workResult)
        {
            return _unitOfWork.RunInTransactionOrAnswerIfExpenseChangedMeanwhileAsync(
                () => CountWork(workResult),
                CountAnswer,
                TestContext.Current.CancellationToken);
        }

        private async Task<Result> CountWork(Result result)
        {
            _workRuns++;
            await Task.Yield();

            return result;
        }

        private async Task<Result> CountAnswer()
        {
            _answerRuns++;
            await Task.Yield();

            return Result.Conflict("The expense changed meanwhile.", ChangedCode);
        }
    }
}
