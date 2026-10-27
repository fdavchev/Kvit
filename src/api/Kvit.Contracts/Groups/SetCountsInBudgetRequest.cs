using System.ComponentModel.DataAnnotations;

namespace Kvit.Contracts.Groups
{
    public sealed record SetCountsInBudgetRequest([Required] bool? CountsInBudget);
}
