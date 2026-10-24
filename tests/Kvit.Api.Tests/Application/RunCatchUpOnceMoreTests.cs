using Kvit.Application.Persistence;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Application
{
    public class RunCatchUpOnceMoreTests
    {
        private readonly FakeUnitOfWork _unitOfWork = new();
        private int _workRuns;

        public static TheoryData<string> ClashesWorthOneMoreRun => ["deadlock", "same entry saved"];

        [Theory]
        [MemberData(nameof(ClashesWorthOneMoreRun))]
        public async Task OnceMore_FirstCommitClashesWithAnotherRequest_RunsTheWorkOnceMoreInANewTransactionAndReturnsItsResult(string clash)
        {
            _unitOfWork.CommitFailures.Enqueue(ClashNamed(clash));
            Result<int> secondResult = Result.Ok(3);

            Result<int> actual = await _unitOfWork.RunInTransactionOnceMoreIfRecurringIncomeEntryWasSavedOrDeadlockedAsync(() => CountWork(secondResult), TestContext.Current.CancellationToken);

            Assert.Same(secondResult, actual);
            Assert.Equal(2, _workRuns);
            Assert.Equal([FakeUnitOfWork.OpenedCall, FakeUnitOfWork.CommitFailedCall, FakeUnitOfWork.OpenedCall, FakeUnitOfWork.CommittedCall], _unitOfWork.Calls);
        }

        [Theory]
        [MemberData(nameof(ClashesWorthOneMoreRun))]
        public async Task OnceMore_SecondCommitClashesAgain_ThrowsTheSecondClashAndNeverRunsAThirdTime(string clash)
        {
            Exception first = ClashNamed(clash);
            Exception second = ClashNamed(clash);
            _unitOfWork.CommitFailures.Enqueue(first);
            _unitOfWork.CommitFailures.Enqueue(second);

            Exception actual = await Assert.ThrowsAnyAsync<Exception>(
                () => _unitOfWork.RunInTransactionOnceMoreIfRecurringIncomeEntryWasSavedOrDeadlockedAsync(() => CountWork(Result.Ok(3)), TestContext.Current.CancellationToken));

            Assert.Same(second, actual);
            Assert.Equal(2, _workRuns);
            Assert.Equal([FakeUnitOfWork.OpenedCall, FakeUnitOfWork.CommitFailedCall, FakeUnitOfWork.OpenedCall, FakeUnitOfWork.CommitFailedCall], _unitOfWork.Calls);
        }

        [Fact]
        public async Task OnceMore_DeadlockThenSameEntrySaved_ThrowsTheSecondClashAndNeverRunsAThirdTime()
        {
            RecurringIncomeEntryAlreadySavedException second = new(new InvalidOperationException("Unique violation."));
            _unitOfWork.CommitFailures.Enqueue(new DeadlockDetectedException(new InvalidOperationException("Deadlock.")));
            _unitOfWork.CommitFailures.Enqueue(second);

            RecurringIncomeEntryAlreadySavedException actual = await Assert.ThrowsAsync<RecurringIncomeEntryAlreadySavedException>(
                () => _unitOfWork.RunInTransactionOnceMoreIfRecurringIncomeEntryWasSavedOrDeadlockedAsync(() => CountWork(Result.Ok(3)), TestContext.Current.CancellationToken));

            Assert.Same(second, actual);
            Assert.Equal(2, _workRuns);
        }

        [Fact]
        public async Task OnceMore_AnyOtherCommitFailure_IsThrownAtOnceWithoutASecondRun()
        {
            InvalidOperationException outage = new("The database is down.");
            _unitOfWork.CommitFailures.Enqueue(outage);

            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _unitOfWork.RunInTransactionOnceMoreIfRecurringIncomeEntryWasSavedOrDeadlockedAsync(() => CountWork(Result.Ok(3)), TestContext.Current.CancellationToken));

            Assert.Same(outage, actual);
            Assert.Equal(1, _workRuns);
        }

        private static Exception ClashNamed(string clash)
        {
            return clash switch
            {
                "deadlock" => new DeadlockDetectedException(new InvalidOperationException("Deadlock.")),
                "same entry saved" => new RecurringIncomeEntryAlreadySavedException(new InvalidOperationException("Unique violation.")),
                _ => throw new ArgumentOutOfRangeException(nameof(clash), clash, "Unknown clash."),
            };
        }

        private async Task<Result<int>> CountWork(Result<int> result)
        {
            _workRuns++;
            await Task.Yield();

            return result;
        }
    }
}
