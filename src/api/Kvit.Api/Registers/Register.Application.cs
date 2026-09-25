using Kvit.Application.Dispatching;

namespace Kvit.Api.Registers
{
    public static partial class Register
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IDispatcher, Dispatcher>();

            services.Scan(scan => scan
                .FromAssemblyOf<IDispatcher>()
                .AddClasses(classes => classes.AssignableToAny(
                    typeof(ICommandHandler<>),
                    typeof(ICommandHandler<,>),
                    typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            return services;
        }
    }
}
