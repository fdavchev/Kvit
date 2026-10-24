using System.Text.Json;

namespace Kvit.Contracts.BudgetLimits
{
    public sealed record SetBudgetLimitRequest(JsonElement LimitMinor)
    {
        public long? LimitMinorAsWholeNumber()
        {
            if (LimitMinor.ValueKind == JsonValueKind.Number && LimitMinor.TryGetInt64(out long limitMinor))
            {
                return limitMinor;
            }

            return null;
        }
    }
}
