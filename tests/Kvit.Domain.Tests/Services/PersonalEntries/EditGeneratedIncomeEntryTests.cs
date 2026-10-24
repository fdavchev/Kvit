using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Domain.Services.PersonalEntries;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.PersonalEntries
{
    public class EditGeneratedIncomeEntryTests
    {
        private static readonly DateTimeOffset Later = PersonalEntryTestData.Moment.AddHours(3);
        private static readonly DateOnly PayDay = new(2026, 10, 15);

        private readonly FakePersonalEntryRepository _entries = new();
        private readonly FakeExchangeRateRepository _rates = new();
        private readonly EditPersonalEntry _editPersonalEntry;
        private readonly RecurringIncome _salary = RecurringIncomeTestData.NewMonthly();
        private readonly PersonalEntry _generated;

        public EditGeneratedIncomeEntryTests()
        {
            _generated = PersonalEntry.CreateFromRecurringIncome(Guid.CreateVersion7(), _salary, PayDay, PersonalEntryTestData.Rate, PersonalEntryTestData.Moment);
            _entries.Seed(_generated);
            PersonalEntryTestData.SeedRate(_rates, PersonalEntryTestData.Rate);
            FixedTimeProvider timeProvider = new(Later);
            _editPersonalEntry = new EditPersonalEntry(_entries, new FakeCategoryRepository(), PersonalEntryTestData.CurrentRateFrom(_rates, new FakeExchangeRateSource(), timeProvider), timeProvider);
        }

        [Fact]
        public async Task Execute_GeneratedEntryMovedToAnotherDate_FailsWithDateLockedAndChangesNothing()
        {
            Result result = await ExecuteAsync(_generated.Id, PersonalEntryTestData.IncomeInputOf(5_000_000, "MKD", "2026-10-16", "Raise"));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_LOCKED);
            Assert.Equal(PayDay, _generated.EntryDate);
            Assert.Equal(RecurringIncomeTestData.Salary, _generated.AmountMinor);
            Assert.Equal("Salary", _generated.Note);
            Assert.Null(_generated.UpdatedAt);
        }

        [Fact]
        public async Task Execute_GeneratedEntryKeepingItsDate_ChangesTheAmountCurrencyAndNote()
        {
            Result result = await ExecuteAsync(_generated.Id, PersonalEntryTestData.IncomeInputOf(1_550, "EUR", "2026-10-15", "Bonus"));

            Assert.True(result.IsSuccess);
            Assert.Equal(PayDay, _generated.EntryDate);
            Assert.Equal(1_550, _generated.AmountMinor);
            Assert.Equal(Currency.EUR, _generated.Currency);
            Assert.Equal("Bonus", _generated.Note);
            Assert.Equal(_salary.Id, _generated.RecurringIncomeId);
            Assert.Equal(Later, _generated.UpdatedAt);
        }

        [Fact]
        public async Task Execute_IncomeEntryTheUserAdded_CanStillMoveItsDate()
        {
            PersonalEntry added = PersonalEntryTestData.NewIncome();
            _entries.Seed(added);

            Result result = await ExecuteAsync(added.Id, PersonalEntryTestData.IncomeInputOf(entryDate: "2026-10-17"));

            Assert.True(result.IsSuccess);
            Assert.Equal(new DateOnly(2026, 10, 17), added.EntryDate);
        }

        private Task<Result> ExecuteAsync(Guid entryId, IncomeInput input)
        {
            return _editPersonalEntry.Execute(PersonalEntryTestData.UserId, entryId, input, TestContext.Current.CancellationToken);
        }
    }
}
