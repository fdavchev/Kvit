using Kvit.Api.Authorization;
using Kvit.Api.RateLimiting;
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
    [Route("api/auth")]
    public sealed class AuthController(IDispatcher _dispatcher) : BaseController
    {
        [HttpPost("register")]
        [AllowAnonymous]
        [ServiceFilter<SignUpLimitFilter>]
        public async Task<ActionResult<MeResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            RegisterCommand command = new(request.DisplayName, request.Email, request.Password, request.TimeZone, request.Language);
            Result<MeResponse> result = await _dispatcher.Send<RegisterCommand, MeResponse>(command, cancellationToken);
            return Result(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("log-in")]
        public async Task<ActionResult<MeResponse>> LogIn(LogInRequest request, CancellationToken cancellationToken)
        {
            LogInCommand command = new(request.Email, request.Password, request.TimeZone);
            Result<MeResponse> result = await _dispatcher.Send<LogInCommand, MeResponse>(command, cancellationToken);
            return Result(result);
        }

        [HttpPost("google")]
        [AllowAnonymous]
        [EnableRateLimiting("log-in")]
        public async Task<ActionResult<MeResponse>> GoogleLogIn(GoogleLogInRequest request, CancellationToken cancellationToken)
        {
            GoogleLogInCommand command = new(request.IdToken, request.TimeZone);
            Result<MeResponse> result = await _dispatcher.Send<GoogleLogInCommand, MeResponse>(command, cancellationToken);
            return Result(result);
        }

        [HttpPost("google/sign-up")]
        [AllowAnonymous]
        [EnableRateLimiting("log-in")]
        public async Task<ActionResult<MeResponse>> GoogleSignUp(GoogleSignUpRequest request, CancellationToken cancellationToken)
        {
            GoogleSignUpCommand command = new(request.IdToken, request.DisplayName, request.TimeZone, request.Language);
            Result<MeResponse> result = await _dispatcher.Send<GoogleSignUpCommand, MeResponse>(command, cancellationToken);
            return Result(result);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<ActionResult> LogOut(CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new LogOutCommand(), cancellationToken);
            return Result(result);
        }

        [HttpPost("change-password")]
        [Authorize]
        [AllowedWithTemporaryPassword]
        [EnableRateLimiting("log-in")]
        public async Task<ActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            ChangePasswordCommand command = new(request.CurrentPassword, request.NewPassword);
            Result result = await _dispatcher.Send(command, cancellationToken);
            return Result(result);
        }

        [HttpPost("set-password")]
        [Authorize]
        [EnableRateLimiting("log-in")]
        public async Task<ActionResult> SetPassword(SetPasswordRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new SetPasswordCommand(request.NewPassword), cancellationToken);
            return Result(result);
        }
    }
}
