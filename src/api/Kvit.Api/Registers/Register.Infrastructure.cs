using Kvit.Api.Settings;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Api.Registers
{
    public static partial class Register
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddHostedService<KvitDatabaseSettingCheck>();

            services.AddDbContext<AppDbContext>((serviceProvider, options) => options
                .UseNpgsql(KvitDatabaseSetting.Read(serviceProvider.GetRequiredService<IConfiguration>()))
                .UseSnakeCaseNamingConvention());

            return services;
        }
    }
}
