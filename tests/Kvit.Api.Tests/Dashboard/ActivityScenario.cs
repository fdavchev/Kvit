using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.Dashboard
{
    public sealed record ActivityScenario(SignedInUser Ana, ExpenseGroup Greece, ExpenseGroup Flat);
}
