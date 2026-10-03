using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public static class InvalidGroupInput
    {
        public static TheoryData<string, string, string, string> Cases => new()
        {
            { "", GroupRows.DefaultEmoji, GroupRows.DefaultCurrency, ResultCodes.GROUP_NAME_INVALID },
            { "   ", GroupRows.DefaultEmoji, GroupRows.DefaultCurrency, ResultCodes.GROUP_NAME_INVALID },
            { new string('a', 61), GroupRows.DefaultEmoji, GroupRows.DefaultCurrency, ResultCodes.GROUP_NAME_INVALID },
            { GroupRows.DefaultName, "", GroupRows.DefaultCurrency, ResultCodes.GROUP_EMOJI_INVALID },
            { GroupRows.DefaultName, "   ", GroupRows.DefaultCurrency, ResultCodes.GROUP_EMOJI_INVALID },
            { GroupRows.DefaultName, new string('a', 33), GroupRows.DefaultCurrency, ResultCodes.GROUP_EMOJI_INVALID },
            { GroupRows.DefaultName, GroupRows.DefaultEmoji, "USD", ResultCodes.GROUP_CURRENCY_INVALID },
            { GroupRows.DefaultName, GroupRows.DefaultEmoji, "", ResultCodes.GROUP_CURRENCY_INVALID },
            { GroupRows.DefaultName, GroupRows.DefaultEmoji, "mkd", ResultCodes.GROUP_CURRENCY_INVALID },
            { GroupRows.DefaultName, GroupRows.DefaultEmoji, "eur", ResultCodes.GROUP_CURRENCY_INVALID },
            { "", "", "USD", ResultCodes.GROUP_NAME_INVALID },
            { "", GroupRows.DefaultEmoji, "USD", ResultCodes.GROUP_NAME_INVALID },
            { GroupRows.DefaultName, "", "USD", ResultCodes.GROUP_EMOJI_INVALID },
        };
    }
}
