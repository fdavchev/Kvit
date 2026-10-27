using Kvit.Domain.Entities;

namespace Kvit.Infrastructure.Persistence
{
    public sealed record CountedShare
    {
        public required ExpenseShare Share { get; init; }

        public required Expense Expense { get; init; }

        public required Group Group { get; init; }
    }
}
