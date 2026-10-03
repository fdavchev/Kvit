using Kvit.Api.Settings;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;

namespace Kvit.Api.Registers
{
    public static partial class Register
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>((serviceProvider, options) => options
                .UseKvitDatabase(KvitDatabaseSetting.Read(serviceProvider.GetRequiredService<IConfiguration>())));

            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<PasswordResetService>();
            services.AddSingleton<IGoogleTokenChecker>(serviceProvider =>
                new GoogleTokenChecker(GoogleSetting.Read(serviceProvider.GetRequiredService<IConfiguration>())));

            return services;
        }
    }
}
