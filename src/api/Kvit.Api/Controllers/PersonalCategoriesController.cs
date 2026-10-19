using Kvit.Application.Commands.PersonalCategories;
using Kvit.Application.Dispatching;
using Kvit.Application.Queries.PersonalCategories;
using Kvit.Contracts.PersonalCategories;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/personal/categories")]
    public sealed class PersonalCategoriesController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<PersonalCategoryListResponse>> List([FromQuery] bool includeArchived, CancellationToken cancellationToken)
        {
            Result<PersonalCategoryListResponse> result = await _dispatcher.Query<GetPersonalCategoriesQuery, PersonalCategoryListResponse>(new GetPersonalCategoriesQuery(includeArchived), cancellationToken);
            return Result(result);
        }

        [HttpPost]
        public async Task<ActionResult<PersonalCategoryRow>> Create(CreatePersonalCategoryRequest request, CancellationToken cancellationToken)
        {
            CreatePersonalCategoryCommand command = new(request.Name, request.Emoji, request.Color);
            Result<PersonalCategoryRow> result = await _dispatcher.Send<CreatePersonalCategoryCommand, PersonalCategoryRow>(command, cancellationToken);
            return Result(result);
        }

        [HttpPut("{categoryId:guid}")]
        public async Task<ActionResult> Update(Guid categoryId, UpdatePersonalCategoryRequest request, CancellationToken cancellationToken)
        {
            UpdatePersonalCategoryCommand command = new(categoryId, request.Name, request.Emoji, request.Color);
            Result result = await _dispatcher.Send(command, cancellationToken);
            return Result(result);
        }

        [HttpPost("{categoryId:guid}/archive")]
        public async Task<ActionResult> Archive(Guid categoryId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new ArchivePersonalCategoryCommand(categoryId), cancellationToken);
            return Result(result);
        }

        [HttpPost("{categoryId:guid}/unarchive")]
        public async Task<ActionResult> Unarchive(Guid categoryId, CancellationToken cancellationToken)
        {
            Result result = await _dispatcher.Send(new UnarchivePersonalCategoryCommand(categoryId), cancellationToken);
            return Result(result);
        }
    }
}
