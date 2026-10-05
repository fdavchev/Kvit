using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupUpdateTests
    {
        private const string NewEmoji = "\U0001F37D";

        [Fact]
        public void Update_NameOnly_ReportsTheNameChangeAndNoSettingsChange()
        {
            Group group = GroupTestData.NewGroup();

            Result<GroupChanges> result = group.Update("Rome", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            Assert.True(result.IsSuccess);
            Assert.Equal(new FieldChange("name", GroupTestData.ValidName, "Rome"), result.Value.Name);
            Assert.Empty(result.Value.Settings);
            Assert.Equal("Rome", group.Name);
        }

        [Fact]
        public void Update_EmojiOnly_ReportsOneSettingsChangeNamedEmoji()
        {
            Group group = GroupTestData.NewGroup();

            Result<GroupChanges> result = group.Update(GroupTestData.ValidName, NewEmoji, GroupTestData.ValidCurrency);

            Assert.Null(result.Value.Name);
            Assert.Equal([new FieldChange("emoji", GroupTestData.ValidEmoji, NewEmoji)], result.Value.Settings);
            Assert.Equal(NewEmoji, group.Emoji);
        }

        [Fact]
        public void Update_CurrencyOnly_ReportsOneSettingsChangeNamedDefaultCurrency()
        {
            Group group = GroupTestData.NewGroup();

            Result<GroupChanges> result = group.Update(GroupTestData.ValidName, GroupTestData.ValidEmoji, "EUR");

            Assert.Null(result.Value.Name);
            Assert.Equal([new FieldChange("defaultCurrency", "MKD", "EUR")], result.Value.Settings);
            Assert.Equal(Currency.EUR, group.DefaultCurrency);
        }

        [Fact]
        public void Update_NameEmojiAndCurrency_ReportsTheNameChangeAndBothSettingsChanges()
        {
            Group group = GroupTestData.NewGroup();

            Result<GroupChanges> result = group.Update("Rome", NewEmoji, "EUR");

            Assert.Equal(new FieldChange("name", GroupTestData.ValidName, "Rome"), result.Value.Name);
            Assert.Equal(
                [new FieldChange("emoji", GroupTestData.ValidEmoji, NewEmoji), new FieldChange("defaultCurrency", "MKD", "EUR")],
                result.Value.Settings);
        }

        [Fact]
        public void Update_SameValues_ReportsNothingAndKeepsTheGroup()
        {
            Group group = GroupTestData.NewGroup();

            Result<GroupChanges> result = group.Update(GroupTestData.ValidName, GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value.Name);
            Assert.Empty(result.Value.Settings);
            Assert.Equal(GroupTestData.ValidName, group.Name);
        }

        [Fact]
        public void Update_NameThatDiffersOnlyBySurroundingSpaces_ReportsNothing()
        {
            Group group = GroupTestData.NewGroup();

            Result<GroupChanges> result = group.Update($"  {GroupTestData.ValidName}  ", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            Assert.Null(result.Value.Name);
        }

        [Fact]
        public void Update_NameWithSurroundingSpaces_StoresAndReportsTheTrimmedName()
        {
            Group group = GroupTestData.NewGroup();

            Result<GroupChanges> result = group.Update("  Rome  ", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency);

            Assert.Equal("Rome", group.Name);
            Assert.Equal(new FieldChange("name", GroupTestData.ValidName, "Rome"), result.Value.Name);
        }

        [Theory]
        [InlineData("", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency, ResultCodes.GROUP_NAME_INVALID)]
        [InlineData("Rome", "   ", "EUR", ResultCodes.GROUP_EMOJI_INVALID)]
        [InlineData("Rome", GroupTestData.ValidEmoji, "eur", ResultCodes.GROUP_CURRENCY_INVALID)]
        [InlineData("", "", "USD", ResultCodes.GROUP_NAME_INVALID)]
        public void Update_InvalidInput_FailsWithTheErrorCodeAndChangesNothing(string name, string emoji, string currency, string expectedErrorCode)
        {
            Group group = GroupTestData.NewGroup();

            Result<GroupChanges> result = group.Update(name, emoji, currency);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(GroupTestData.ValidName, group.Name);
            Assert.Equal(GroupTestData.ValidEmoji, group.Emoji);
            Assert.Equal(Currency.MKD, group.DefaultCurrency);
        }
    }
}
