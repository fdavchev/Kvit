using Kvit.Application.Dispatching;
using Kvit.Application.Queries.PersonalEntries;
using Kvit.Contracts.PersonalEntries;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/personal/summary")]
    public sealed class PersonalSummaryController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<PersonalSummaryResponse>> Get([FromQuery] string? month, CancellationToken cancellationToken)
        {
            Result<PersonalSummaryResponse> result = await _dispatcher.Query<GetPersonalSummaryQuery, PersonalSummaryResponse>(new GetPersonalSummaryQuery(month), cancellationToken);
            return Result(result);
        }
    }
}
