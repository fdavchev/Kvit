using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.Export
{
    public sealed record ExportBeforeEnd(
        SignedInUser Owner,
        SignedInUser Caller,
        Guid GroupId,
        Guid OwnerRowId,
        Guid CallerRowId,
        Guid EarlyRowId,
        Guid BeforeExpenseId,
        Guid BeforePaymentId);
}
