using Kvit.Domain.Services.Groups;

namespace Kvit.Api.Registers
{
    public static partial class Register
    {
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            services.Scan(scan => scan
                .FromAssemblyOf<CreateGroup>()
                .AddClasses(classes => classes.InNamespaces("Kvit.Domain.Services"))
                .AsSelf()
                .WithScopedLifetime());

            return services;
        }
    }
}
