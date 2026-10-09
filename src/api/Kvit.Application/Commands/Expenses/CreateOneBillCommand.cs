using Kvit.Domain.Expenses;

namespace Kvit.Application.Commands.Expenses
{
    public sealed record CreateOneBillCommand(Guid ClientRequestId, OneBillInput Input);
}
