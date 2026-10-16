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

        public async Task<TResult> RunForInviteAsync<TResult>(string inviteToken, Func<Task<TResult>> write, CancellationToken cancellationToken)
            where TResult : Result
        {
            await SaveFinishOfExpiredClosingAsync(() => _finishExpiredClosing.ExecuteForInviteAsync(inviteToken, cancellationToken), cancellationToken);

            return await _unitOfWork.RunInTransactionAsync(write, cancellationToken);
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
