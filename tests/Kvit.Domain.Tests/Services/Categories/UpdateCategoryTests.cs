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
    public class UpdateCategoryTests
    {
        private readonly FakeCategoryRepository _categories = new();
        private readonly UpdateCategory _updateCategory;
        private readonly Category _pets = CategoryTestData.NewCustom("Pets", 11);

        public UpdateCategoryTests()
        {
            _categories.Seed(_pets);
            _updateCategory = new UpdateCategory(_categories);
        }

        [Fact]
        public async Task Execute_OwnersCategoryWithValidInput_ChangesTheNameEmojiAndColor()
        {
            Result result = await ExecuteAsync(CategoryTestData.OwnerId, _pets.Id, " Kids ", "\U0001F9F8", "pink");

            Assert.True(result.IsSuccess);
            Assert.Equal("Kids", _pets.Name);
            Assert.Equal("\U0001F9F8", _pets.Emoji);
            Assert.Equal("pink", _pets.Color);
            Assert.Equal(11, _pets.SortOrder);
        }

        [Fact]
        public async Task Execute_SameNameInAnotherLetterCase_Succeeds()
        {
            Result result = await ExecuteAsync(CategoryTestData.OwnerId, _pets.Id, "PETS");

            Assert.True(result.IsSuccess);
            Assert.Equal("PETS", _pets.Name);
        }

        [Fact]
        public async Task Execute_NameOfAnotherActiveCategoryOfTheOwner_FailsWithNameTakenAndChangesNothing()
        {
            _categories.Seed(CategoryTestData.NewCustom("Kids", 12));

            Result result = await ExecuteAsync(CategoryTestData.OwnerId, _pets.Id, "kids", "\U0001F9F8", "pink");

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
            AssertPetsUnchanged();
        }

        [Fact]
        public async Task Execute_NameOfAnArchivedCategoryOfTheOwner_Succeeds()
        {
            _categories.Seed(CategoryTestData.NewArchived("Kids", 12, CategoryTestData.Moment));

            Result result = await ExecuteAsync(CategoryTestData.OwnerId, _pets.Id, "Kids");

            Assert.True(result.IsSuccess);
            Assert.Equal("Kids", _pets.Name);
        }

        [Fact]
        public async Task Execute_AnArchivedCategoryGetsTheNameOfAnActiveOne_SucceedsBecauseArchivedNamesDoNotCount()
        {
            Category archivedKids = CategoryTestData.NewArchived("Old kids", 12, CategoryTestData.Moment);
            _categories.Seed(CategoryTestData.NewCustom("Kids", 13), archivedKids);

            Result result = await ExecuteAsync(CategoryTestData.OwnerId, archivedKids.Id, "Kids");

            Assert.True(result.IsSuccess);
            Assert.Equal("Kids", archivedKids.Name);
            Assert.Equal(CategoryTestData.Moment, archivedKids.ArchivedAt);
        }

        [Theory]
        [InlineData("", CategoryTestData.ValidEmoji, CategoryTestData.ValidColor, ResultCodes.CATEGORY_NAME_INVALID)]
        [InlineData(CategoryTestData.ValidName, " ", CategoryTestData.ValidColor, ResultCodes.CATEGORY_EMOJI_INVALID)]
        [InlineData(CategoryTestData.ValidName, CategoryTestData.ValidEmoji, "Teal", ResultCodes.CATEGORY_COLOR_INVALID)]
        public async Task Execute_InvalidField_FailsWithItsCodeAndChangesNothing(string name, string emoji, string color, string expectedErrorCode)
        {
            Result result = await ExecuteAsync(CategoryTestData.OwnerId, _pets.Id, name, emoji, color);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, expectedErrorCode);
            AssertPetsUnchanged();
        }

        [Fact]
        public async Task Execute_AnotherOwnersCategory_FailsWith404CategoryNotFoundAndChangesNothing()
        {
            Result result = await ExecuteAsync(CategoryTestData.OtherOwnerId, _pets.Id, "Kids");

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
            AssertPetsUnchanged();
        }

        [Fact]
        public async Task Execute_UnknownCategory_FailsWith404CategoryNotFound()
        {
            Result result = await ExecuteAsync(CategoryTestData.OwnerId, Guid.CreateVersion7(), "Kids");

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
        }

        [Fact]
        public async Task Execute_UnknownCategoryWithInvalidInput_ReportsNotFoundFirst()
        {
            Result result = await ExecuteAsync(CategoryTestData.OwnerId, Guid.CreateVersion7(), "", "", "black");

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
        }

        private void AssertPetsUnchanged()
        {
            Assert.Equal("Pets", _pets.Name);
            Assert.Equal(CategoryTestData.ValidEmoji, _pets.Emoji);
            Assert.Equal(CategoryTestData.ValidColor, _pets.Color);
        }

        private Task<Result> ExecuteAsync(Guid ownerUserId, Guid categoryId, string name, string emoji = CategoryTestData.ValidEmoji, string color = CategoryTestData.ValidColor)
        {
            return _updateCategory.Execute(ownerUserId, categoryId, name, emoji, color, TestContext.Current.CancellationToken);
        }
    }
}
