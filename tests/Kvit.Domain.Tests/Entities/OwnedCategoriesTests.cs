using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class OwnedCategoriesTests
    {
        [Fact]
        public void Find_OneOfTheOwnersCategories_ReturnsIt()
        {
            Category pets = CategoryTestData.NewCustom("Pets");
            OwnedCategories owned = new([CategoryTestData.NewCustom("Kids"), pets]);

            Result<Category> found = owned.Find(pets.Id);

            Assert.True(found.IsSuccess);
            Assert.Same(pets, found.Value);
        }

        [Fact]
        public void Find_AnArchivedCategoryOfTheOwner_ReturnsIt()
        {
            Category archived = CategoryTestData.NewArchived("Pets", 11, CategoryTestData.Moment);
            OwnedCategories owned = new([archived]);

            Result<Category> found = owned.Find(archived.Id);

            Assert.Same(archived, found.Value);
        }

        [Fact]
        public void Find_AnIdThatIsNotAmongTheOwnersCategories_FailsWith404CategoryNotFound()
        {
            OwnedCategories owned = new([CategoryTestData.NewCustom("Pets")]);

            Result<Category> found = owned.Find(Guid.CreateVersion7());

            ResultAssert.Failed(found, HttpStatusCode.NotFound, ResultCodes.CATEGORY_NOT_FOUND);
        }

        [Fact]
        public void NextSortOrder_NoOwnCategories_Is11SoTheFirstComesAfterTheTenBuiltIns()
        {
            OwnedCategories owned = new([]);

            Assert.Equal(11, owned.NextSortOrder());
        }

        [Fact]
        public void NextSortOrder_OwnCategories_IsOneAboveTheHighest()
        {
            OwnedCategories owned = new([CategoryTestData.NewCustom("Pets", 11), CategoryTestData.NewCustom("Kids", 14), CategoryTestData.NewCustom("Car", 12)]);

            Assert.Equal(15, owned.NextSortOrder());
        }

        [Fact]
        public void NextSortOrder_TheHighestIsArchived_StillCountsIt()
        {
            OwnedCategories owned = new([CategoryTestData.NewCustom("Pets", 11), CategoryTestData.NewArchived("Kids", 20, CategoryTestData.Moment)]);

            Assert.Equal(21, owned.NextSortOrder());
        }

        [Fact]
        public void CheckNameFree_ANameNoActiveCategoryHas_Succeeds()
        {
            OwnedCategories owned = new([CategoryTestData.NewCustom("Pets")]);

            Result result = owned.CheckNameFree("Kids", Guid.CreateVersion7());

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("Pets")]
        [InlineData("pets")]
        [InlineData("PETS")]
        public void CheckNameFree_TheNameOfAnotherActiveCategoryInAnyLetterCase_FailsWithNameTaken(string name)
        {
            OwnedCategories owned = new([CategoryTestData.NewCustom("Pets")]);

            Result result = owned.CheckNameFree(name, Guid.CreateVersion7());

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
        }

        [Fact]
        public void CheckNameFree_ACyrillicNameInAnotherLetterCase_FailsWithNameTaken()
        {
            OwnedCategories owned = new([CategoryTestData.NewCustom("Миленичиња")]);

            Result result = owned.CheckNameFree("МИЛЕНИЧИЊА", Guid.CreateVersion7());

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
        }

        [Fact]
        public void CheckNameFree_TheNameOfAnArchivedCategory_Succeeds()
        {
            OwnedCategories owned = new([CategoryTestData.NewArchived("Pets", 11, CategoryTestData.Moment)]);

            Result result = owned.CheckNameFree("Pets", Guid.CreateVersion7());

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void CheckNameFree_TheCategoryItselfInAnotherLetterCase_Succeeds()
        {
            Category pets = CategoryTestData.NewCustom("Pets");
            OwnedCategories owned = new([pets]);

            Result result = owned.CheckNameFree("PETS", pets.Id);

            Assert.True(result.IsSuccess);
        }
    }
}
