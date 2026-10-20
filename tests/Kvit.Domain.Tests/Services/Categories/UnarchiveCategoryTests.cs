using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Categories;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.Categories
{
    public class UnarchiveCategoryTests
    {
        private readonly FakeCategoryRepository _categories = new();
        private readonly UnarchiveCategory _unarchiveCategory;
        private readonly Category _archivedPets = CategoryTestData.NewArchived("Pets", 11, CategoryTestData.Moment);

        public UnarchiveCategoryTests()
        {
            _categories.Seed(_archivedPets);
            _unarchiveCategory = new UnarchiveCategory(_categories);
        }

        [Fact]
        public async Task Execute_OwnersArchivedCategory_BringsItBack()
        {
            Result result = await ExecuteAsync(CategoryTestData.OwnerId, _archivedPets.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(_archivedPets.ArchivedAt);
        }

        [Fact]
        public async Task Execute_OwnersActiveCategory_SucceedsAndLeavesItActive()
        {
            Category active = CategoryTestData.NewCustom("Car", 12);
            _categories.Seed(active);

            Result result = await ExecuteAsync(CategoryTestData.OwnerId, active.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(active.ArchivedAt);
        }

        [Theory]
        [InlineData("Pets")]
        [InlineData("PETS")]
        public async Task Execute_AnActiveCategoryHasTheSameName_FailsWithNameTakenAndKeepsItArchived(string activeName)
        {
            _categories.Seed(CategoryTestData.NewCustom(activeName, 12));

            Result result = await ExecuteAsync(CategoryTestData.OwnerId, _archivedPets.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
            Assert.Equal(CategoryTestData.Moment, _archivedPets.ArchivedAt);
        }

        [Fact]
        public async Task Execute_AnotherArchivedCategoryHasTheSameName_BringsItBack()
        {
            _categories.Seed(CategoryTestData.NewArchived("Pets", 12, CategoryTestData.Moment));

            Result result = await ExecuteAsync(CategoryTestData.OwnerId, _archivedPets.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(_archivedPets.ArchivedAt);
        }

        [Fact]
        public async Task Execute_AnotherOwnersCategory_FailsWith404CategoryNotFoundAndKeepsItArchived()
        {
            Result result = await ExecuteAsync(CategoryTestData.OtherOwnerId, _archivedPets.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Equal(CategoryTestData.Moment, _archivedPets.ArchivedAt);
        }

        [Fact]
        public async Task Execute_UnknownCategory_FailsWith404CategoryNotFound()
        {
            Result result = await ExecuteAsync(CategoryTestData.OwnerId, Guid.CreateVersion7());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
        }

        private Task<Result> ExecuteAsync(Guid ownerUserId, Guid categoryId)
        {
            return _unarchiveCategory.Execute(ownerUserId, categoryId, TestContext.Current.CancellationToken);
        }
    }
}
