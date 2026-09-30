using Kvit.Application.Commands.Auth;
using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Me;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kvit.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/auth")]
    public sealed class AuthController(IDispatcher _dispatcher) : BaseController
    {
        [HttpPost("register")]
        [EnableRateLimiting("sign-up")]
        public async Task<ActionResult<MeResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            RegisterCommand command = new(request.DisplayName, request.Email, request.Password, request.TimeZone, request.Language);
            Result<MeResponse> result = await _dispatcher.Send<RegisterCommand, MeResponse>(command, cancellationToken);
            return Result(result);
        }

        [HttpPost("login")]
        [EnableRateLimiting("log-in")]
        public async Task<ActionResult<MeResponse>> LogIn(LogInRequest request, CancellationToken cancellationToken)
        {
            LogInCommand command = new(request.Email, request.Password, request.TimeZone);
            Result<MeResponse> result = await _dispatcher.Send<LogInCommand, MeResponse>(command, cancellationToken);
            return Result(result);
        }

        [HttpPost("logout")]
        public async Task<ActionResult> LogOut(CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new LogOutCommand(), cancellationToken);
            return Result(result);
        }
    }
}
