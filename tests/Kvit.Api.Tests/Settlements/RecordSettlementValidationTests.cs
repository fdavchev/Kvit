using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RecordSettlementValidationTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData("MKD", 0)]
        [InlineData("MKD", -1)]
        [InlineData("MKD", -30_000)]
        [InlineData("EUR", 0)]
        [InlineData("EUR", -1)]
        public async Task Record_AmountOfZeroOrLess_Answers400AmountNotPositive(string currency, long amountMinor)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, group.MemberRowId, group.OwnerRowId, amountMinor, currency, ResultCodes.SETTLEMENT_AMOUNT_NOT_POSITIVE);
        }

        [Theory]
        [InlineData("MKD", 1_000_000_000_000)]
        [InlineData("EUR", 1_000_000_000_000)]
        [InlineData("EUR", 1_000_000_000_001)]
        [InlineData("EUR", long.MaxValue)]
        public async Task Record_AmountAbove999999999999MinorUnits_Answers400AmountTooLarge(string currency, long amountMinor)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, group.MemberRowId, group.OwnerRowId, amountMinor, currency, ResultCodes.SETTLEMENT_AMOUNT_TOO_LARGE);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(99)]
        [InlineData(150)]
        [InlineData(12_345)]
        public async Task Record_MkdAmountNotAWholeDenar_Answers400NotOnCurrencyStep(long amountMinor)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, group.MemberRowId, group.OwnerRowId, amountMinor, ExpenseInputs.Mkd, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP);
        }

        [Theory]
        [InlineData("mkd")]
        [InlineData("eur")]
        [InlineData("Mkd")]
        [InlineData("USD")]
        [InlineData("")]
        [InlineData(" MKD")]
        public async Task Record_CurrencyOutsideTheTwoKnownNames_Answers400CurrencyInvalid(string currency)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, group.MemberRowId, group.OwnerRowId, 30_000, currency, ResultCodes.SETTLEMENT_CURRENCY_INVALID);
        }

        [Fact]
        public async Task Record_SameMemberOnBothSides_Answers400SameMember()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, group.OwnerRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, ResultCodes.SETTLEMENT_SAME_MEMBER);
        }

        [Fact]
        public async Task Record_SamePlainNameOnBothSidesByTheOwner_Answers400SameMember()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, group.MarkoRowId, group.MarkoRowId, 30_000, ExpenseInputs.Mkd, ResultCodes.SETTLEMENT_SAME_MEMBER);
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        public async Task Record_SenderWhoIsNoLongerAMember_Answers400MemberNotCurrent(string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            await AssertRejectedAsync(group, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
        }

        [Theory]
        [InlineData("Removed")]
        [InlineData("Left")]
        public async Task Record_ReceiverWhoIsNoLongerAMember_Answers400MemberNotCurrent(string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Third.UserId, endKind);

            await AssertRejectedAsync(group, group.OwnerRowId, group.ThirdRowId, 30_000, ExpenseInputs.Mkd, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
        }

        [Fact]
        public async Task Record_PlainNameThatWasRemoved_Answers400MemberNotCurrent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ExpenseRows.EndMemberRowAsync(_app, group.MarkoRowId, group.Owner.UserId);

            await AssertRejectedAsync(group, group.MarkoRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
        }

        [Fact]
        public async Task Record_SenderWhoIsAMemberOfAnotherGroup_Answers400MemberNotCurrent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup other = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, other.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
        }

        [Fact]
        public async Task Record_ReceiverWhoIsAMemberOfAnotherGroup_Answers400MemberNotCurrent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SettlementGroup other = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, group.OwnerRowId, other.MarkoRowId, 30_000, ExpenseInputs.Mkd, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
        }

        [Fact]
        public async Task Record_SenderThatDoesNotExist_Answers400MemberNotCurrent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, Guid.CreateVersion7(), group.OwnerRowId, 30_000, ExpenseInputs.Mkd, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
        }

        [Fact]
        public async Task Record_ReceiverThatDoesNotExist_Answers400MemberNotCurrent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            await AssertRejectedAsync(group, group.OwnerRowId, Guid.CreateVersion7(), 30_000, ExpenseInputs.Mkd, ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
        }

        private async Task AssertRejectedAsync(SettlementGroup group, Guid fromMemberId, Guid toMemberId, long amountMinor, string currency, string errorCode)
        {
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.Owner.Client, group.GroupId, fromMemberId, toMemberId, amountMinor, currency);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, errorCode);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(0, await SettlementRows.CountConfirmedUsageEventsAsync(_app, group.Owner.UserId));
        }
    }
}
