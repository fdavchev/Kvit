using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Categories;
using Kvit.Contracts.Categories;
using Kvit.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvit.Api.Controllers
{
    [Authorize]
    [Route("api/categories")]
    public sealed class CategoriesController(IDispatcher _dispatcher) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<CategoryListResponse>> List(CancellationToken cancellationToken)
        {
            Result<CategoryListResponse> result = await _dispatcher.Query<GetCategoriesQuery, CategoryListResponse>(new GetCategoriesQuery(), cancellationToken);
            return Result(result);
        }
    }
}
