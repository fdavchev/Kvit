using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class CategoryPersonalEntryTests
    {
        [Fact]
        public void CanTagPersonalEntriesOf_TheOwnerOfAnActiveCustomCategory_IsTrue()
        {
            Category category = CategoryTestData.NewCustom();

            Assert.True(category.CanTagPersonalEntriesOf(CategoryTestData.OwnerId));
        }

        [Fact]
        public void CanTagPersonalEntriesOf_AnotherUser_IsFalse()
        {
            Category category = CategoryTestData.NewCustom();

            Assert.False(category.CanTagPersonalEntriesOf(CategoryTestData.OtherOwnerId));
        }

        [Fact]
        public void CanTagPersonalEntriesOf_TheOwnerOfAnArchivedCategory_IsFalse()
        {
            Category category = CategoryTestData.NewArchived("Pets", 11, CategoryTestData.Moment);

            Assert.False(category.CanTagPersonalEntriesOf(CategoryTestData.OwnerId));
        }
    }
}
