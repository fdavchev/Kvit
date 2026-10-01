using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Persistence
{
    public static class AppDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseKvitDatabase(this DbContextOptionsBuilder options, string connectionString)
        {
            return options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();
        }
    }
}
