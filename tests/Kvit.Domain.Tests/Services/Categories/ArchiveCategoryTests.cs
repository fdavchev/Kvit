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
    public class ArchiveCategoryTests
    {
        private readonly FakeCategoryRepository _categories = new();
        private readonly ArchiveCategory _archiveCategory;
        private readonly Category _pets = CategoryTestData.NewCustom("Pets");

        public ArchiveCategoryTests()
        {
            _categories.Seed(_pets);
            _archiveCategory = new ArchiveCategory(_categories, new FixedTimeProvider(CategoryTestData.Moment));
        }

        [Fact]
        public async Task Execute_OwnersActiveCategory_ArchivesItAtTheCurrentMoment()
        {
            Result result = await ExecuteAsync(CategoryTestData.OwnerId, _pets.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(CategoryTestData.Moment, _pets.ArchivedAt);
        }

        [Fact]
        public async Task Execute_OwnersArchivedCategory_SucceedsAndKeepsTheFirstArchiveMoment()
        {
            DateTimeOffset firstArchivedAt = CategoryTestData.Moment.AddDays(-3);
            Category archived = CategoryTestData.NewArchived("Car", 12, firstArchivedAt);
            _categories.Seed(archived);

            Result result = await ExecuteAsync(CategoryTestData.OwnerId, archived.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(firstArchivedAt, archived.ArchivedAt);
        }

        [Fact]
        public async Task Execute_AnotherOwnersCategory_FailsWith404CategoryNotFoundAndKeepsItActive()
        {
            Result result = await ExecuteAsync(CategoryTestData.OtherOwnerId, _pets.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            Assert.Null(_pets.ArchivedAt);
        }

        [Fact]
        public async Task Execute_UnknownCategory_FailsWith404CategoryNotFound()
        {
            Result result = await ExecuteAsync(CategoryTestData.OwnerId, Guid.CreateVersion7());

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
        }

        private Task<Result> ExecuteAsync(Guid ownerUserId, Guid categoryId)
        {
            return _archiveCategory.Execute(ownerUserId, categoryId, TestContext.Current.CancellationToken);
        }
    }
}
