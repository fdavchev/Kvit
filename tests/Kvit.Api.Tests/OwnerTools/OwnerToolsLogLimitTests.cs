using System.Text.Json;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsLogLimitTests(OwnerToolsBulkLogApp _app) : IClassFixture<OwnerToolsBulkLogApp>
    {
        [Fact]
        public async Task Log_WithoutALimit_Answers50ItemsNewestFirst()
        {
            JsonElement log = await ReadAsync(null);

            Assert.Equal(OwnerToolsRequests.LogDefaultLimit, log.GetArrayLength());
            Assert.Equal("Bulk group 260", log[0].GetProperty("targetLabel").GetString());
            Assert.Equal("Bulk group 211", log[OwnerToolsRequests.LogDefaultLimit - 1].GetProperty("targetLabel").GetString());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(49)]
        [InlineData(50)]
        [InlineData(51)]
        [InlineData(120)]
        [InlineData(199)]
        [InlineData(200)]
        public async Task Log_WithALimitInRange_AnswersExactlyThatManyItems(int limit)
        {
            JsonElement log = await ReadAsync(limit);

            Assert.Equal(limit, log.GetArrayLength());
        }

        [Theory]
        [InlineData(201)]
        [InlineData(250)]
        [InlineData(1000)]
        [InlineData(100000)]
        public async Task Log_WithALimitAbove200_Answers200Items(int limit)
        {
            JsonElement log = await ReadAsync(limit);

            Assert.Equal(OwnerToolsRequests.LogMaxLimit, log.GetArrayLength());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-50)]
        [InlineData(-100000)]
        public async Task Log_WithALimitOfZeroOrLess_AnswersTheDefault50Items(int limit)
        {
            JsonElement log = await ReadAsync(limit);

            Assert.Equal(OwnerToolsRequests.LogDefaultLimit, log.GetArrayLength());
        }

        [Fact]
        public async Task Log_WithALimitOf3_AnswersTheThreeNewestInOrder()
        {
            JsonElement log = await ReadAsync(3);

            Assert.Equal(
                ["Bulk group 260", "Bulk group 259", "Bulk group 258"],
                log.EnumerateArray().Select(item => item.GetProperty("targetLabel").GetString()));
        }

        [Fact]
        public async Task Log_WithTheMaximumLimit_ReachesBack200RowsAndNoFurther()
        {
            JsonElement log = await ReadAsync(OwnerToolsRequests.LogMaxLimit);

            Assert.Equal("Bulk group 260", log[0].GetProperty("targetLabel").GetString());
            Assert.Equal("Bulk group 61", log[OwnerToolsRequests.LogMaxLimit - 1].GetProperty("targetLabel").GetString());
        }

        [Fact]
        public async Task Log_ItemsOfTheBulkRows_ShowTheAdminNameTheReasonAndTheGroupKind()
        {
            SignedInUser admin = await _app.SeededAdminAsync();

            JsonElement log = await ReadAsync(1);

            JsonElement newest = log[0];
            Assert.Equal(admin.Form.DisplayName, newest.GetProperty("adminName").GetString());
            Assert.Equal("Bulk reason 260", newest.GetProperty("reason").GetString());
            Assert.Equal("group", newest.GetProperty("targetKind").GetString());
            Assert.Equal("GroupClosed", newest.GetProperty("action").GetString());
        }

        private async Task<JsonElement> ReadAsync(int? limit)
        {
            SignedInUser admin = await _app.SeededAdminAsync();

            return await OwnerToolsRequests.ReadLogAsync(admin.Client, limit);
        }
    }
}
