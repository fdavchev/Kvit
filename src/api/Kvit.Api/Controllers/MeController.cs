using System.Globalization;
using Kvit.Api.Authorization;
using Kvit.Application.Commands.Me;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Me;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/me")]
    public sealed class MeController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        [AllowedWithTemporaryPassword]
        [AllowedWhileSuspended]
        public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
        {
            Result<MeResponse> result = await _dispatcher.Query<GetMeQuery, MeResponse>(new GetMeQuery(), cancellationToken);
            return Result(result);
        }

        [HttpGet("export")]
        [EnableRateLimiting("export")]
        public async Task<ActionResult<MyDataExportResponse>> Export(CancellationToken cancellationToken)
        {
            Result<MyDataExportResponse> result = await _dispatcher.Query<GetMyDataExportQuery, MyDataExportResponse>(new GetMyDataExportQuery(), cancellationToken);
            if (result.IsSuccess)
            {
                DateOnly exportDay = DateOnly.FromDateTime(result.Value.ExportedAt.UtcDateTime);
                Response.Headers.ContentDisposition = $"attachment; filename=\"kvit-my-data-{exportDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.json\"";
                Response.Headers.CacheControl = "no-store";
            }

            return Result(result);
        }

        [HttpPut("language")]
        public async Task<ActionResult> ChangeLanguage(ChangeLanguageRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new ChangeLanguageCommand(request.Language), cancellationToken);
            return Result(result);
        }

        [HttpPut("budget-currency")]
        public async Task<ActionResult> SetBudgetTotalCurrency(SetBudgetTotalCurrencyRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new SetBudgetTotalCurrencyCommand(request.BudgetTotalCurrency), cancellationToken);
            return Result(result);
        }

        [HttpPost("active")]
        public async Task<ActionResult> RecordActive(CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RecordActiveCommand(), cancellationToken);
            return Result(result);
        }

        [HttpPost("terms/accept")]
        public async Task<ActionResult> AcceptTerms(CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new AcceptTermsCommand(), cancellationToken);
            return Result(result);
        }

        [HttpPost("delete")]
        public async Task<ActionResult> Delete(DeleteAccountRequest request, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new DeleteAccountCommand(request.NameChoice), cancellationToken);
            return Result(result);
        }
    }
}
