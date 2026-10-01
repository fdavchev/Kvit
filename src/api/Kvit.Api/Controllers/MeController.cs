using Kvit.Api.Authorization;
using Kvit.Application.Commands.Me;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Me;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/me")]
    public sealed class MeController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        [AllowedWithTemporaryPassword]
        public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
        {
            Result<MeResponse> result = await _dispatcher.Query<GetMeQuery, MeResponse>(new GetMeQuery(), cancellationToken);
            return Result(result);
        }

        [HttpPut("language")]
        public async Task<ActionResult> ChangeLanguage(ChangeLanguageRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new ChangeLanguageCommand(request.Language), cancellationToken);
            return Result(result);
        }
    }
}
