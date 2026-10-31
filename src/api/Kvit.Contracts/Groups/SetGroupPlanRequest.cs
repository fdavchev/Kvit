using System.Text.Json;

namespace Kvit.Contracts.Groups
{
    public sealed record SetGroupPlanRequest(JsonElement AmountMinor, JsonElement Currency)
    {
        public long? AmountMinorAsWholeNumber()
        {
            if (AmountMinor.ValueKind == JsonValueKind.Number && AmountMinor.TryGetInt64(out long amountMinor))
            {
                return amountMinor;
            }

            return null;
        }

        public string? CurrencyAsText()
        {
            if (Currency.ValueKind == JsonValueKind.String)
            {
                return Currency.GetString();
            }

            return null;
        }
    }
}
