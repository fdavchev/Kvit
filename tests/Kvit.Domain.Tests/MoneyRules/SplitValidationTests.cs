using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Xunit;
using static Kvit.Domain.Tests.MoneyRules.MoneyTestData;

namespace Kvit.Domain.Tests.MoneyRules
{
    public class SplitValidationTests
    {
        [Theory]
        [InlineData(SplitType.Equal, Currency.MKD, 0L)]
        [InlineData(SplitType.Exact, Currency.EUR, 0L)]
        [InlineData(SplitType.Percentage, Currency.MKD, -100000L)]
        [InlineData(SplitType.Shares, Currency.EUR, -1L)]
        public void AmountNotAboveZero_FailsWithAmountNotPositive(SplitType splitType, Currency currency, long totalMinorUnits)
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                splitType,
                Amount(totalMinorUnits, currency),
                Filip,
                [new SplitInput(Filip, 1)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE, result.ErrorCode);
        }

        [Theory]
        [InlineData(SplitType.Equal)]
        [InlineData(SplitType.Exact)]
        [InlineData(SplitType.Percentage)]
        [InlineData(SplitType.Shares)]
        public void NoPeopleInTheSplit_FailsWithNoParticipants(SplitType splitType)
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(splitType, Steps(1000, Currency.MKD), Filip, []);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_SPLIT_NO_PARTICIPANTS, result.ErrorCode);
        }

        [Theory]
        [InlineData(SplitType.Equal)]
        [InlineData(SplitType.Exact)]
        [InlineData(SplitType.Percentage)]
        [InlineData(SplitType.Shares)]
        public void SameMemberTwice_FailsWithDuplicateMember(SplitType splitType)
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                splitType,
                Amount(1000, Currency.EUR),
                Filip,
                [new SplitInput(Ana, 500), new SplitInput(Ana, 500)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_SPLIT_DUPLICATE_MEMBER, result.ErrorCode);
            Assert.Contains(Ana.ToString(), result.Error);
        }

        [Theory]
        [InlineData(SplitType.Equal)]
        [InlineData(SplitType.Exact)]
        [InlineData(SplitType.Percentage)]
        [InlineData(SplitType.Shares)]
        public void NegativeInput_FailsWithNegativeInput(SplitType splitType)
        {
            Result<IReadOnlyList<SplitShare>> result = Splitter.Calculate(
                splitType,
                Amount(1000, Currency.EUR),
                Filip,
                [new SplitInput(Filip, 1100), new SplitInput(Ana, -100)]);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.EXPENSE_SPLIT_NEGATIVE_INPUT, result.ErrorCode);
            Assert.Contains("-100", result.Error);
        }

        [Fact]
        public void TotalNotOnWholeDenar_CannotBeBuiltSoNeverReachesTheSplitter()
        {
            Result<Money> total = Money.Create(12050, Currency.MKD);

            Assert.False(total.IsSuccess);
            Assert.Equal(ResultCodes.MONEY_NOT_ON_CURRENCY_STEP, total.ErrorCode);
        }

        [Fact]
        public void UnknownSplitType_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Splitter.Calculate((SplitType)99, Steps(1000, Currency.MKD), Filip, [new SplitInput(Filip, 0)]));
        }
    }
}
