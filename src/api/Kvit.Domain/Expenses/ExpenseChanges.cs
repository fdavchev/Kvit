using System.Globalization;
using Kvit.Domain.Entities;

namespace Kvit.Domain.Expenses
{
    public static class ExpenseChanges
    {
        public static IReadOnlyList<FieldChange> Between(ExpenseState before, ExpenseState after)
        {
            List<FieldChange> changes = [];
            AddIfDifferent(changes, "amount", MinorUnitsText(before.AmountMinor), MinorUnitsText(after.AmountMinor));
            AddIfDifferent(changes, "currency", before.Currency.ToString(), after.Currency.ToString());
            AddIfDifferent(changes, "title", before.Title ?? string.Empty, after.Title ?? string.Empty);
            AddIfDifferent(changes, "note", before.Note ?? string.Empty, after.Note ?? string.Empty);
            AddIfDifferent(changes, "date", DateText(before.ExpenseDate), DateText(after.ExpenseDate));
            AddIfDifferent(changes, "category", before.CategoryKey, after.CategoryKey);
            if (before.PayerMemberId != after.PayerMemberId)
            {
                changes.Add(new FieldChange("paidBy", before.PayerName, after.PayerName));
            }

            if (IsSplitChanged(before, after))
            {
                changes.Add(new FieldChange("split", SplitText(before), SplitText(after)));
            }

            return changes;
        }

        private static void AddIfDifferent(List<FieldChange> changes, string field, string oldText, string newText)
        {
            if (!string.Equals(oldText, newText, StringComparison.Ordinal))
            {
                changes.Add(new FieldChange(field, oldText, newText));
            }
        }

        private static bool IsSplitChanged(ExpenseState before, ExpenseState after)
        {
            return before.SplitType != after.SplitType
                || !before.SplitInJoiningOrder.Select(TypedValueOf).SequenceEqual(after.SplitInJoiningOrder.Select(TypedValueOf));
        }

        private static (Guid MemberId, long InputValue) TypedValueOf(SplitLine line)
        {
            return (line.MemberId, line.InputValue);
        }

        private static string SplitText(ExpenseState state)
        {
            IEnumerable<string> people = state.SplitInJoiningOrder.Select(line => $"{line.Name} {MinorUnitsText(line.ShareMinor)}");

            return $"{state.SplitType}: {string.Join(", ", people)}";
        }

        private static string MinorUnitsText(long minorUnits)
        {
            return minorUnits.ToString(CultureInfo.InvariantCulture);
        }

        private static string DateText(DateOnly date)
        {
            return date.ToString(ExpenseFields.DateFormat, CultureInfo.InvariantCulture);
        }
    }
}
