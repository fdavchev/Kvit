using Kvit.Api.Settings;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Groups;
using Kvit.Infrastructure.Persistence;
using Kvit.Infrastructure.Repositories;

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
            services.AddSingleton<IInviteTokenGenerator, InviteTokenGenerator>();

            services.Scan(scan => scan
                .FromAssemblyOf<GroupRepository>()
                .AddClasses(classes => classes.InNamespaceOf<GroupRepository>())
                .AsMatchingInterface()
                .WithScopedLifetime());

            services.AddSingleton<IGoogleTokenChecker>(serviceProvider =>
                new GoogleTokenChecker(GoogleSetting.Read(serviceProvider.GetRequiredService<IConfiguration>())));

            return services;
        }
    }
}
