using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class CategoryCustomTests
    {
        [Fact]
        public void CreateCustom_ValidFields_ReturnsACategoryOfTheOwnerWithTheGivenValues()
        {
            Category category = Category.CreateCustom(CategoryTestData.OwnerId, CategoryTestData.Fields("Pets", "\U0001F43E", "teal"), 12);

            Assert.NotEqual(Guid.Empty, category.Id);
            Assert.Equal(CategoryTestData.OwnerId, category.OwnerUserId);
            Assert.Equal("Pets", category.Name);
            Assert.Equal("\U0001F43E", category.Emoji);
            Assert.Equal("teal", category.Color);
            Assert.Equal(12, category.SortOrder);
        }

        [Fact]
        public void CreateCustom_ValidFields_HasNoKeyAndIsNotArchived()
        {
            Category category = CategoryTestData.NewCustom();

            Assert.Null(category.Key);
            Assert.Null(category.ArchivedAt);
        }

        [Fact]
        public void CreateCustom_TwoCategories_GetDifferentIds()
        {
            Category first = CategoryTestData.NewCustom();
            Category second = CategoryTestData.NewCustom();

            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public void CreateCustom_ACustomCategory_CannotTagGroupExpenses()
        {
            Category category = CategoryTestData.NewCustom();

            Assert.False(category.CanTagGroupExpenses());
        }

        [Fact]
        public void CustomName_OfACustomCategory_IsItsName()
        {
            Category category = CategoryTestData.NewCustom("Pets");

            Assert.Equal("Pets", category.CustomName());
        }

        [Fact]
        public void Update_NewFields_ReplacesTheNameEmojiAndColor()
        {
            Category category = CategoryTestData.NewCustom();

            category.Update(CategoryTestData.Fields("Kids", "\U0001F9F8", "pink"));

            Assert.Equal("Kids", category.Name);
            Assert.Equal("\U0001F9F8", category.Emoji);
            Assert.Equal("pink", category.Color);
        }

        [Fact]
        public void Update_NewFields_KeepsTheIdOwnerSortOrderAndArchiveState()
        {
            Category category = CategoryTestData.NewArchived("Pets", 14, CategoryTestData.Moment);
            Guid id = category.Id;

            category.Update(CategoryTestData.Fields("Kids"));

            Assert.Equal(id, category.Id);
            Assert.Equal(CategoryTestData.OwnerId, category.OwnerUserId);
            Assert.Equal(14, category.SortOrder);
            Assert.Equal(CategoryTestData.Moment, category.ArchivedAt);
        }

        [Fact]
        public void Archive_AnActiveCategory_StoresTheMoment()
        {
            Category category = CategoryTestData.NewCustom();

            category.Archive(CategoryTestData.Moment);

            Assert.Equal(CategoryTestData.Moment, category.ArchivedAt);
        }

        [Fact]
        public void Archive_AnArchivedCategory_KeepsTheFirstMoment()
        {
            Category category = CategoryTestData.NewArchived("Pets", 11, CategoryTestData.Moment);

            category.Archive(CategoryTestData.Moment.AddDays(1));

            Assert.Equal(CategoryTestData.Moment, category.ArchivedAt);
        }

        [Fact]
        public void Archive_AnArchivedCustomCategory_StillCannotTagGroupExpenses()
        {
            Category category = CategoryTestData.NewArchived("Pets", 11, CategoryTestData.Moment);

            Assert.False(category.CanTagGroupExpenses());
        }

        [Fact]
        public void Unarchive_AnArchivedCategory_ClearsTheMoment()
        {
            Category category = CategoryTestData.NewArchived("Pets", 11, CategoryTestData.Moment);

            category.Unarchive();

            Assert.Null(category.ArchivedAt);
        }

        [Fact]
        public void Unarchive_AnActiveCategory_LeavesItActive()
        {
            Category category = CategoryTestData.NewCustom();

            category.Unarchive();

            Assert.Null(category.ArchivedAt);
        }
    }
}
