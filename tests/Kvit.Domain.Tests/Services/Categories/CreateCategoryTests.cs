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
    public class CreateCategoryTests
    {
        private readonly FakeCategoryRepository _categories = new();
        private readonly CreateCategory _createCategory;

        public CreateCategoryTests()
        {
            _createCategory = new CreateCategory(_categories);
        }

        [Fact]
        public async Task Execute_ValidInput_AddsOneCategoryOfTheOwnerAndReturnsItsId()
        {
            Result<Guid> result = await ExecuteAsync(CategoryTestData.OwnerId, "  Pets ", "\U0001F43E", "teal");

            Assert.True(result.IsSuccess);
            Category added = Assert.Single(_categories.Added);
            Assert.Equal(added.Id, result.Value);
            Assert.Equal(CategoryTestData.OwnerId, added.OwnerUserId);
            Assert.Equal("Pets", added.Name);
            Assert.Equal("\U0001F43E", added.Emoji);
            Assert.Equal("teal", added.Color);
            Assert.Null(added.Key);
            Assert.Null(added.ArchivedAt);
        }

        [Fact]
        public async Task Execute_FirstCategoryOfTheOwner_GetsSortOrder11()
        {
            await ExecuteAsync(CategoryTestData.OwnerId, "Pets");

            Assert.Equal(11, Assert.Single(_categories.Added).SortOrder);
        }

        [Fact]
        public async Task Execute_OwnerHasCategories_GetsTheSortOrderAfterTheHighestOfTheOwner()
        {
            _categories.Seed(CategoryTestData.NewCustom("Pets", 11), CategoryTestData.NewArchived("Car", 13, CategoryTestData.Moment));

            await ExecuteAsync(CategoryTestData.OwnerId, "Kids");

            Assert.Equal(14, Assert.Single(_categories.Added).SortOrder);
        }

        [Fact]
        public async Task Execute_AnotherOwnerHasHigherSortOrders_IgnoresThem()
        {
            _categories.Seed(Category.CreateCustom(CategoryTestData.OtherOwnerId, CategoryTestData.Fields("Pets"), 40));

            await ExecuteAsync(CategoryTestData.OwnerId, "Kids");

            Assert.Equal(11, Assert.Single(_categories.Added).SortOrder);
        }

        [Theory]
        [InlineData("Pets")]
        [InlineData("pets")]
        [InlineData(" PETS ")]
        public async Task Execute_NameOfAnActiveCategoryOfTheOwnerInAnyLetterCase_FailsWithNameTakenAndAddsNothing(string name)
        {
            _categories.Seed(CategoryTestData.NewCustom("Pets"));

            Result<Guid> result = await ExecuteAsync(CategoryTestData.OwnerId, name);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
            Assert.Empty(_categories.Added);
        }

        [Fact]
        public async Task Execute_NameOfAnArchivedCategoryOfTheOwner_Succeeds()
        {
            _categories.Seed(CategoryTestData.NewArchived("Pets", 11, CategoryTestData.Moment));

            Result<Guid> result = await ExecuteAsync(CategoryTestData.OwnerId, "Pets");

            Assert.True(result.IsSuccess);
            Assert.Single(_categories.Added);
        }

        [Fact]
        public async Task Execute_NameOfAnotherOwnersCategory_Succeeds()
        {
            _categories.Seed(Category.CreateCustom(CategoryTestData.OtherOwnerId, CategoryTestData.Fields("Pets"), 11));

            Result<Guid> result = await ExecuteAsync(CategoryTestData.OwnerId, "Pets");

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("", CategoryTestData.ValidEmoji, CategoryTestData.ValidColor, ResultCodes.CATEGORY_NAME_INVALID)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", CategoryTestData.ValidEmoji, CategoryTestData.ValidColor, ResultCodes.CATEGORY_NAME_INVALID)]
        [InlineData(CategoryTestData.ValidName, "", CategoryTestData.ValidColor, ResultCodes.CATEGORY_EMOJI_INVALID)]
        [InlineData(CategoryTestData.ValidName, CategoryTestData.ValidEmoji, "black", ResultCodes.CATEGORY_COLOR_INVALID)]
        public async Task Execute_InvalidField_FailsWithItsCodeAndAddsNothing(string name, string emoji, string color, string expectedErrorCode)
        {
            Result<Guid> result = await ExecuteAsync(CategoryTestData.OwnerId, name, emoji, color);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Empty(_categories.Added);
        }

        [Fact]
        public async Task Execute_InvalidNameThatIsAlsoTaken_ReportsTheInvalidName()
        {
            string tooLong = new('a', 31);
            _categories.Seed(CategoryTestData.NewCustom(new string('a', 30)));

            Result<Guid> result = await ExecuteAsync(CategoryTestData.OwnerId, tooLong);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_INVALID);
        }

        [Fact]
        public async Task Execute_TakenNameWithABadColor_ReportsTheColorBeforeTheTakenName()
        {
            _categories.Seed(CategoryTestData.NewCustom("Pets"));

            Result<Guid> result = await ExecuteAsync(CategoryTestData.OwnerId, "Pets", CategoryTestData.ValidEmoji, "black");

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_COLOR_INVALID);
        }

        private Task<Result<Guid>> ExecuteAsync(Guid ownerUserId, string name, string emoji = CategoryTestData.ValidEmoji, string color = CategoryTestData.ValidColor)
        {
            return _createCategory.Execute(ownerUserId, name, emoji, color, TestContext.Current.CancellationToken);
        }
    }
}
