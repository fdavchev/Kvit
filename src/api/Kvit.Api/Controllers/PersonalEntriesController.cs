using Kvit.Application.Commands.PersonalEntries;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.PersonalEntries;
using Kvit.Contracts.PersonalEntries;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/personal/entries")]
    public sealed class PersonalEntriesController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<PersonalEntryMonthResponse>> List([FromQuery] string? month, CancellationToken cancellationToken)
        {
            Result<PersonalEntryMonthResponse> result = await _dispatcher.Query<GetPersonalEntriesQuery, PersonalEntryMonthResponse>(new GetPersonalEntriesQuery(month), cancellationToken);
            return Result(result);
        }

        [HttpPost]
        public async Task<ActionResult<PersonalEntryRow>> Add(AddPersonalEntryRequest request, CancellationToken cancellationToken)
        {
            PersonalEntryInput input = new(request.CategoryId, request.AmountMinor, request.Currency, request.EntryDate, request.Note);
            Result<PersonalEntryRow> result = await _dispatcher.Send<AddPersonalEntryCommand, PersonalEntryRow>(new AddPersonalEntryCommand(request.ClientRequestId, input), cancellationToken);
            return Result(result);
        }

        [HttpPut("{entryId:guid}")]
        public async Task<ActionResult<PersonalEntryRow>> Edit(Guid entryId, EditPersonalEntryRequest request, CancellationToken cancellationToken)
        {
            PersonalEntryInput input = new(request.CategoryId, request.AmountMinor, request.Currency, request.EntryDate, request.Note);
            Result<PersonalEntryRow> result = await _dispatcher.Send<EditPersonalEntryCommand, PersonalEntryRow>(new EditPersonalEntryCommand(entryId, input), cancellationToken);
            return Result(result);
        }

        [HttpDelete("{entryId:guid}")]
        public async Task<ActionResult> Delete(Guid entryId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new DeletePersonalEntryCommand(entryId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{entryId:guid}/restore")]
        public async Task<ActionResult> Restore(Guid entryId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new RestorePersonalEntryCommand(entryId), cancellationToken);
            return Result(result);
        }
    }
}
