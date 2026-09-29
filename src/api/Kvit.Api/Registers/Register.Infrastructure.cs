using Kvit.Api.Settings;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Api.Registers
{
    public static partial class Register
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>((serviceProvider, options) => options
                .UseNpgsql(KvitDatabaseSetting.Read(serviceProvider.GetRequiredService<IConfiguration>()))
                .UseSnakeCaseNamingConvention());

            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
            services.AddScoped<IAccountService, AccountService>();

            return services;
        }
    }
}
