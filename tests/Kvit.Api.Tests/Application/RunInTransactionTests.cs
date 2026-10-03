using System.Net;
using Kvit.Application.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Application
{
    public class RunInTransactionTests
    {
        private const string WorkCall = "work";
        private const string FailureCode = "SOME_FAILURE";

        private readonly FakeUnitOfWork _unitOfWork = new();

        [Fact]
        public async Task RunInTransactionAsync_SuccessfulResult_CommitsAndNeverRollsBack()
        {
            Result expected = Result.Ok();

            Result actual = await _unitOfWork.RunInTransactionAsync(() => RecordWork(expected), TestContext.Current.CancellationToken);

            Assert.Same(expected, actual);
            Assert.Equal([FakeUnitOfWork.OpenedCall, WorkCall, FakeUnitOfWork.CommittedCall], _unitOfWork.Calls);
        }

        [Fact]
        public async Task RunInTransactionAsync_FailedResult_RollsBackNeverCommitsAndReturnsTheSameResult()
        {
            Result expected = Result.Failure("Something is wrong.", FailureCode);

            Result actual = await _unitOfWork.RunInTransactionAsync(() => RecordWork(expected), TestContext.Current.CancellationToken);

            Assert.Same(expected, actual);
            Assert.False(actual.IsSuccess);
            Assert.Equal(FailureCode, actual.ErrorCode);
            Assert.Equal([FakeUnitOfWork.OpenedCall, WorkCall, FakeUnitOfWork.RolledBackCall], _unitOfWork.Calls);
        }

        [Fact]
        public async Task RunInTransactionAsync_ResultWithValue_CommitsAndReturnsTheSameResultWithItsValue()
        {
            Result<Guid> expected = Result.Ok(Guid.NewGuid());

            Result<Guid> actual = await _unitOfWork.RunInTransactionAsync(() => RecordWork(expected), TestContext.Current.CancellationToken);

            Assert.Same(expected, actual);
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal([FakeUnitOfWork.OpenedCall, WorkCall, FakeUnitOfWork.CommittedCall], _unitOfWork.Calls);
        }

        [Fact]
        public async Task RunInTransactionAsync_FailedResultWithValueType_RollsBackNeverCommitsAndReturnsTheSameResult()
        {
            Result<Guid> expected = Result.NotFound<Guid>("No such thing.", FailureCode);

            Result<Guid> actual = await _unitOfWork.RunInTransactionAsync(() => RecordWork(expected), TestContext.Current.CancellationToken);

            Assert.Same(expected, actual);
            Assert.Equal(HttpStatusCode.NotFound, actual.StatusCode);
            Assert.Equal([FakeUnitOfWork.OpenedCall, WorkCall, FakeUnitOfWork.RolledBackCall], _unitOfWork.Calls);
        }

        [Fact]
        public async Task RunInTransactionAsync_OpensTheTransactionBeforeTheWorkRuns()
        {
            await _unitOfWork.RunInTransactionAsync(() => RecordWork(Result.Ok()), TestContext.Current.CancellationToken);

            Assert.True(_unitOfWork.Calls.IndexOf(FakeUnitOfWork.OpenedCall) < _unitOfWork.Calls.IndexOf(WorkCall));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RunInTransactionAsync_RunsTheWorkExactlyOnce(bool succeeds)
        {
            Result outcome = succeeds ? Result.Ok() : Result.Failure("Something is wrong.", FailureCode);

            await _unitOfWork.RunInTransactionAsync(() => RecordWork(outcome), TestContext.Current.CancellationToken);

            Assert.Single(_unitOfWork.Calls, WorkCall);
        }

        [Fact]
        public async Task RunInTransactionAsync_WorkThatThrows_PropagatesTheSameExceptionAndNeitherCommitsNorRollsBack()
        {
            InvalidOperationException expected = new("The work failed.");

            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _unitOfWork.RunInTransactionAsync<Result>(() => ThrowFromWork(expected), TestContext.Current.CancellationToken));

            Assert.Same(expected, actual);
            Assert.Equal([FakeUnitOfWork.OpenedCall, WorkCall], _unitOfWork.Calls);
        }

        [Fact]
        public async Task RunInTransactionAsync_SuccessfulResult_PassesTheCancellationTokenToOpenAndCommit()
        {
            await _unitOfWork.RunInTransactionAsync(() => RecordWork(Result.Ok()), TestContext.Current.CancellationToken);

            Assert.Equal([TestContext.Current.CancellationToken, TestContext.Current.CancellationToken], _unitOfWork.TokensReceived);
        }

        [Fact]
        public async Task RunInTransactionAsync_FailedResult_PassesTheCancellationTokenToOpenAndRollback()
        {
            await _unitOfWork.RunInTransactionAsync(() => RecordWork(Result.Failure("Something is wrong.", FailureCode)), TestContext.Current.CancellationToken);

            Assert.Equal([TestContext.Current.CancellationToken, TestContext.Current.CancellationToken], _unitOfWork.TokensReceived);
        }

        private async Task<TResult> RecordWork<TResult>(TResult result)
            where TResult : Result
        {
            _unitOfWork.Calls.Add(WorkCall);
            await Task.Yield();

            return result;
        }

        private async Task<Result> ThrowFromWork(Exception exception)
        {
            _unitOfWork.Calls.Add(WorkCall);
            await Task.Yield();

            throw exception;
        }
    }
}
