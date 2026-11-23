using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Persistence
{
    public sealed class GroupWriteTransaction(IUnitOfWork _unitOfWork, FinishExpiredClosingBeforeWrite _finishExpiredClosing)
    {
        public async Task<TResult> RunAsync<TResult>(Guid groupId, Guid userId, Func<Task<TResult>> write, CancellationToken cancellationToken)
            where TResult : Result
        {
            await SaveFinishOfExpiredClosingAsync(() => _finishExpiredClosing.ExecuteForMemberAsync(groupId, userId, cancellationToken), cancellationToken);

            return await _unitOfWork.RunInTransactionAsync(write, cancellationToken);
        }

        public async Task<TResult> RunOnceMoreIfClientRequestIdWasTakenAsync<TResult>(Guid groupId, Guid userId, Func<Task<TResult>> write, CancellationToken cancellationToken)
            where TResult : Result
        {
            await SaveFinishOfExpiredClosingAsync(() => _finishExpiredClosing.ExecuteForMemberAsync(groupId, userId, cancellationToken), cancellationToken);

            return await _unitOfWork.RunInTransactionOnceMoreIfClientRequestIdWasTakenAsync(write, cancellationToken);
        }

        public async Task<TResult> RunOnceMoreIfMemberChangedMeanwhileAsync<TResult>(Guid groupId, Guid userId, Func<Task<TResult>> write, CancellationToken cancellationToken)
            where TResult : Result
        {
            await SaveFinishOfExpiredClosingAsync(() => _finishExpiredClosing.ExecuteForMemberAsync(groupId, userId, cancellationToken), cancellationToken);

            return await _unitOfWork.RunInTransactionOnceMoreIfMemberChangedMeanwhileAsync(write, cancellationToken);
        }

        public async Task<TResult> RunOnceMoreIfSettlementChangedMeanwhileAsync<TResult>(Guid groupId, Guid userId, Func<Task<TResult>> write, CancellationToken cancellationToken)
            where TResult : Result
        {
            await SaveFinishOfExpiredClosingAsync(() => _finishExpiredClosing.ExecuteForMemberAsync(groupId, userId, cancellationToken), cancellationToken);

            return await _unitOfWork.RunInTransactionOnceMoreIfSettlementChangedMeanwhileAsync(write, cancellationToken);
        }

        public async Task<TResult> RunOrAnswerIfExpenseChangedMeanwhileAsync<TResult>(Guid groupId, Guid userId, Func<Task<TResult>> write, Func<Task<TResult>> answerAfterChange, CancellationToken cancellationToken)
            where TResult : Result
        {
            await SaveFinishOfExpiredClosingAsync(() => _finishExpiredClosing.ExecuteForMemberAsync(groupId, userId, cancellationToken), cancellationToken);

            return await _unitOfWork.RunInTransactionOrAnswerIfExpenseChangedMeanwhileAsync(write, answerAfterChange, cancellationToken);
        }

        public async Task<Result> PrepareThenRunOrAnswerIfExpenseChangedMeanwhileAsync<TPrepared>(Guid groupId, Guid userId, Func<Task<Result<TPrepared>>> prepareBeforeTransaction, Func<TPrepared, Task<Result>> write, Func<Task<Result>> answerAfterChange, CancellationToken cancellationToken)
        {
            await SaveFinishOfExpiredClosingAsync(() => _finishExpiredClosing.ExecuteForMemberAsync(groupId, userId, cancellationToken), cancellationToken);
            Result<TPrepared> prepared = await prepareBeforeTransaction();
            if (!prepared.IsSuccess)
            {
                return prepared;
            }

            return await _unitOfWork.RunInTransactionOrAnswerIfExpenseChangedMeanwhileAsync(() => write(prepared.Value), answerAfterChange, cancellationToken);
        }

        public async Task<TResult> RunForInviteOnceMoreIfMemberChangedMeanwhileAsync<TResult>(string inviteToken, Func<Task<TResult>> write, CancellationToken cancellationToken)
            where TResult : Result
        {
            await SaveFinishOfExpiredClosingAsync(() => _finishExpiredClosing.ExecuteForInviteAsync(inviteToken, cancellationToken), cancellationToken);

            return await _unitOfWork.RunInTransactionOnceMoreIfMemberChangedMeanwhileAsync(write, cancellationToken);
        }

        private async Task SaveFinishOfExpiredClosingAsync(Func<Task> finishExpiredClosing, CancellationToken cancellationToken)
        {
            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            await finishExpiredClosing();
            await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
