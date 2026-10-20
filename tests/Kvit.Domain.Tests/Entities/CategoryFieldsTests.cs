using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class CategoryFieldsTests
    {
        private const string LongestStandardEmoji = "\U0001F469\U0001F3FC‍❤️‍\U0001F48B‍\U0001F468\U0001F3FD";

        [Fact]
        public void Create_ValidInput_KeepsTheNameEmojiAndColor()
        {
            Result<CategoryFields> result = CategoryFields.Create("Pets", "\U0001F43E", "teal");

            Assert.True(result.IsSuccess);
            Assert.Equal("Pets", result.Value.Name);
            Assert.Equal("\U0001F43E", result.Value.Emoji);
            Assert.Equal("teal", result.Value.Color);
        }

        [Fact]
        public void Create_NameWithSurroundingSpaces_KeepsTheTrimmedName()
        {
            Result<CategoryFields> result = CategoryFields.Create("  Pets \t", CategoryTestData.ValidEmoji, CategoryTestData.ValidColor);

            Assert.Equal("Pets", result.Value.Name);
        }

        [Fact]
        public void Create_NameOf30Characters_Succeeds()
        {
            string name = new('a', 30);

            Result<CategoryFields> result = CategoryFields.Create(name, CategoryTestData.ValidEmoji, CategoryTestData.ValidColor);

            Assert.Equal(name, result.Value.Name);
        }

        [Fact]
        public void Create_NameOf30CharactersPlusSpaces_SucceedsBecauseTheLengthIsCountedAfterTrimming()
        {
            string name = new('a', 30);

            Result<CategoryFields> result = CategoryFields.Create($"  {name}  ", CategoryTestData.ValidEmoji, CategoryTestData.ValidColor);

            Assert.Equal(name, result.Value.Name);
        }

        [Fact]
        public void Create_CyrillicName_Succeeds()
        {
            Result<CategoryFields> result = CategoryFields.Create("Миленичиња", CategoryTestData.ValidEmoji, CategoryTestData.ValidColor);

            Assert.Equal("Миленичиња", result.Value.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void Create_EmptyOrBlankName_FailsWithNameInvalid(string name)
        {
            Result<CategoryFields> result = CategoryFields.Create(name, CategoryTestData.ValidEmoji, CategoryTestData.ValidColor);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_INVALID);
        }

        [Fact]
        public void Create_NameOf31Characters_FailsWithNameInvalid()
        {
            Result<CategoryFields> result = CategoryFields.Create(new string('a', 31), CategoryTestData.ValidEmoji, CategoryTestData.ValidColor);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_INVALID);
        }

        [Fact]
        public void Create_TheLongestStandardEmojiOf15Units_Succeeds()
        {
            Assert.Equal(15, LongestStandardEmoji.Length);

            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, LongestStandardEmoji, CategoryTestData.ValidColor);

            Assert.Equal(LongestStandardEmoji, result.Value.Emoji);
        }

        [Fact]
        public void Create_EmojiOf16Units_Succeeds()
        {
            string emoji = string.Concat(Enumerable.Repeat(CategoryTestData.ValidEmoji, 8));

            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, emoji, CategoryTestData.ValidColor);

            Assert.Equal(16, emoji.Length);
            Assert.Equal(emoji, result.Value.Emoji);
        }

        [Fact]
        public void Create_EmojiOf16UnitsWithSurroundingSpaces_SucceedsBecauseTheLengthIsCountedAfterTrimming()
        {
            string emoji = string.Concat(Enumerable.Repeat(CategoryTestData.ValidEmoji, 8));

            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, $"  {emoji} \t", CategoryTestData.ValidColor);

            Assert.Equal(emoji, result.Value.Emoji);
        }

        [Fact]
        public void Create_EmojiWithSurroundingSpaces_KeepsTheTrimmedEmoji()
        {
            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, " \U0001F43E ", CategoryTestData.ValidColor);

            Assert.Equal("\U0001F43E", result.Value.Emoji);
        }

        [Theory]
        [InlineData("\U0001F3F7️")]
        [InlineData("☕")]
        [InlineData("\U0001F37A")]
        [InlineData("\U0001F355")]
        [InlineData("\U0001F3E0")]
        [InlineData("\U0001F697")]
        [InlineData("⛽")]
        [InlineData("✈️")]
        [InlineData("\U0001F455")]
        [InlineData("\U0001F484")]
        [InlineData("\U0001F487")]
        [InlineData("\U0001F3CB️")]
        [InlineData("\U0001F4DA")]
        [InlineData("\U0001F3AE")]
        [InlineData("\U0001F3B5")]
        [InlineData("\U0001F4F1")]
        [InlineData("\U0001F4A1")]
        [InlineData("\U0001F436")]
        [InlineData("\U0001F476")]
        [InlineData("\U0001F393")]
        public void Create_EachOfTheTwentyQuickPickEmojis_Succeeds(string emoji)
        {
            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, emoji, CategoryTestData.ValidColor);

            Assert.Equal(emoji, result.Value.Emoji);
        }

        [Theory]
        [InlineData("\U0001F1F2\U0001F1F0")]
        [InlineData("1️⃣")]
        [InlineData("#️⃣")]
        [InlineData("\U0001F468‍\U0001F469‍\U0001F467")]
        [InlineData("\U0001F44D\U0001F3FD")]
        public void Create_FlagKeycapZwjSequenceOrSkinToneEmoji_Succeeds(string emoji)
        {
            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, emoji, CategoryTestData.ValidColor);

            Assert.Equal(emoji, result.Value.Emoji);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("xxxxxxxxxxxxxxxxx")]
        public void Create_EmptyBlankOrLongerThan16UnitsEmoji_FailsWithEmojiInvalid(string emoji)
        {
            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, emoji, CategoryTestData.ValidColor);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_EMOJI_INVALID);
        }

        [Fact]
        public void Create_EmojiOf17Units_FailsWithEmojiInvalid()
        {
            string emoji = string.Concat(Enumerable.Repeat(CategoryTestData.ValidEmoji, 8)) + "☕";

            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, emoji, CategoryTestData.ValidColor);

            Assert.Equal(17, emoji.Length);
            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_EMOJI_INVALID);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData(" x ")]
        [InlineData("x")]
        [InlineData("1")]
        [InlineData("?!")]
        [InlineData("a b")]
        [InlineData("#$%&*+")]
        public void Create_EmojiOfOnlyPlainAsciiLettersDigitsOrPunctuation_FailsWithEmojiInvalid(string emoji)
        {
            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, emoji, CategoryTestData.ValidColor);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_EMOJI_INVALID);
        }

        [Theory]
        [InlineData("orange")]
        [InlineData("green")]
        [InlineData("yellow")]
        [InlineData("blue")]
        [InlineData("pink")]
        [InlineData("purple")]
        [InlineData("slate")]
        [InlineData("red")]
        [InlineData("teal")]
        [InlineData("gray")]
        public void Create_EachOfTheTenAppColors_Succeeds(string color)
        {
            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, CategoryTestData.ValidEmoji, color);

            Assert.Equal(color, result.Value.Color);
        }

        [Fact]
        public void Colors_AreExactlyTheTenAppColorsInTheBuiltInOrder()
        {
            Assert.Equal(CategoryTestData.Colors, CategoryFields.Colors);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Orange")]
        [InlineData("TEAL")]
        [InlineData(" teal")]
        [InlineData("grey")]
        [InlineData("black")]
        [InlineData("#bf5900")]
        public void Create_ColorOutsideTheTenNames_FailsWithColorInvalid(string color)
        {
            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, CategoryTestData.ValidEmoji, color);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_COLOR_INVALID);
        }

        [Fact]
        public void Create_EverythingWrong_ReportsTheNameFirst()
        {
            Result<CategoryFields> result = CategoryFields.Create("", "", "black");

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_INVALID);
        }

        [Fact]
        public void Create_BadEmojiAndBadColor_ReportsTheEmojiBeforeTheColor()
        {
            Result<CategoryFields> result = CategoryFields.Create(CategoryTestData.ValidName, "", "black");

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_EMOJI_INVALID);
        }
    }
}
