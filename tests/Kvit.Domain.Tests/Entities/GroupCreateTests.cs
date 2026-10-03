using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class GroupCreateTests
    {
        [Fact]
        public void Create_ValidInput_ReturnsAnOpenGroupOfKindGroupWithTheGivenValues()
        {
            Result<Group> result = Group.Create("Greece trip", "\U0001F3D6", "EUR", GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            Assert.True(result.IsSuccess);
            Group group = result.Value;
            Assert.NotEqual(Guid.Empty, group.Id);
            Assert.Equal(GroupKind.Group, group.Kind);
            Assert.Equal(GroupStatus.Open, group.Status);
            Assert.Equal("Greece trip", group.Name);
            Assert.Equal("\U0001F3D6", group.Emoji);
            Assert.Equal(Currency.EUR, group.DefaultCurrency);
            Assert.Equal(GroupTestData.OwnerId, group.OwnerUserId);
            Assert.Equal(GroupTestData.OwnerId, group.CreatedByUserId);
        }

        [Fact]
        public void Create_ValidInput_StoresTheInviteTokenAndTheCreationMoment()
        {
            Group group = GroupTestData.NewGroup();

            Assert.Equal(GroupTestData.InviteToken, group.InviteToken);
            Assert.Equal(GroupTestData.Moment, group.InviteTokenCreatedAt);
            Assert.Equal(GroupTestData.Moment, group.CreatedAt);
        }

        [Fact]
        public void Create_ValidInput_StartsWithNoPreviousTokenNoDeletionAndNoClosing()
        {
            Group group = GroupTestData.NewGroup();

            Assert.Null(group.PreviousInviteToken);
            Assert.Null(group.DeletedAt);
            Assert.Null(group.DeletedByUserId);
            Assert.Null(group.ClosingStartedAt);
            Assert.Null(group.FinishedAt);
        }

        [Fact]
        public void Create_TwoGroups_GetDifferentIds()
        {
            Group first = GroupTestData.NewGroup();
            Group second = GroupTestData.NewGroup();

            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public void Create_NameWithSurroundingSpaces_StoresTheTrimmedName()
        {
            Result<Group> result = Group.Create("  Greece trip \t", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            Assert.Equal("Greece trip", result.Value.Name);
        }

        [Fact]
        public void Create_NameOf60Characters_Succeeds()
        {
            string name = new('a', 60);

            Result<Group> result = Group.Create(name, GroupTestData.ValidEmoji, GroupTestData.ValidCurrency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            Assert.Equal(name, result.Value.Name);
        }

        [Fact]
        public void Create_NameOf60CharactersPlusSpaces_SucceedsBecauseTheLengthIsCountedAfterTrimming()
        {
            string name = new('a', 60);

            Result<Group> result = Group.Create($"  {name}  ", GroupTestData.ValidEmoji, GroupTestData.ValidCurrency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            Assert.Equal(name, result.Value.Name);
        }

        [Fact]
        public void Create_NameOf61Characters_FailsWithGroupNameInvalid()
        {
            Result<Group> result = Group.Create(new string('a', 61), GroupTestData.ValidEmoji, GroupTestData.ValidCurrency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NAME_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void Create_EmptyOrWhitespaceName_FailsWithGroupNameInvalid(string name)
        {
            Result<Group> result = Group.Create(name, GroupTestData.ValidEmoji, GroupTestData.ValidCurrency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_NAME_INVALID);
        }

        [Fact]
        public void Create_EmojiOf32Characters_Succeeds()
        {
            string emoji = new('a', 32);

            Result<Group> result = Group.Create(GroupTestData.ValidName, emoji, GroupTestData.ValidCurrency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            Assert.Equal(emoji, result.Value.Emoji);
        }

        [Fact]
        public void Create_EmojiOf33Characters_FailsWithGroupEmojiInvalid()
        {
            Result<Group> result = Group.Create(GroupTestData.ValidName, new string('a', 33), GroupTestData.ValidCurrency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_EMOJI_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Create_EmptyOrWhitespaceEmoji_FailsWithGroupEmojiInvalid(string emoji)
        {
            Result<Group> result = Group.Create(GroupTestData.ValidName, emoji, GroupTestData.ValidCurrency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_EMOJI_INVALID);
        }

        [Theory]
        [InlineData("MKD", Currency.MKD)]
        [InlineData("EUR", Currency.EUR)]
        public void Create_SupportedCurrency_StoresIt(string currency, Currency expected)
        {
            Result<Group> result = Group.Create(GroupTestData.ValidName, GroupTestData.ValidEmoji, currency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            Assert.Equal(expected, result.Value.DefaultCurrency);
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("")]
        [InlineData("mkd")]
        [InlineData("Eur")]
        [InlineData(" MKD")]
        [InlineData("0")]
        public void Create_UnsupportedCurrency_FailsWithGroupCurrencyInvalid(string currency)
        {
            Result<Group> result = Group.Create(GroupTestData.ValidName, GroupTestData.ValidEmoji, currency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.GROUP_CURRENCY_INVALID);
        }

        [Theory]
        [InlineData("", "", "USD", ResultCodes.GROUP_NAME_INVALID)]
        [InlineData("", GroupTestData.ValidEmoji, "USD", ResultCodes.GROUP_NAME_INVALID)]
        [InlineData(GroupTestData.ValidName, "", "USD", ResultCodes.GROUP_EMOJI_INVALID)]
        [InlineData("", "", GroupTestData.ValidCurrency, ResultCodes.GROUP_NAME_INVALID)]
        public void Create_SeveralFieldsInvalid_FailsWithTheFirstCodeInTheOrderNameEmojiCurrency(string name, string emoji, string currency, string expectedErrorCode)
        {
            Result<Group> result = Group.Create(name, emoji, currency, GroupTestData.OwnerId, GroupTestData.InviteToken, GroupTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, expectedErrorCode);
        }
    }
}
