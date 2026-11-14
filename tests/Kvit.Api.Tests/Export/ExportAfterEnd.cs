namespace Kvit.Api.Tests.Export
{
    public sealed record ExportAfterEnd(
        Guid BoundaryRowId,
        Guid LateRowId,
        Guid AfterExpenseId,
        Guid AfterPaymentId);
}
