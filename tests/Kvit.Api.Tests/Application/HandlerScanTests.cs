using System.Net;
using System.Security.Claims;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Application.Commands.Me;
using Kvit.Application.Dispatching;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Application
{
    public class HandlerScanTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        private const int KnownHandlerCount = 5;

        private static readonly Type[] HandlerInterfaces = [typeof(ICommandHandler<>), typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

        [Fact]
        public void Scan_FindsAtLeastTheFiveKnownHandlers()
        {
            List<Type> handlerClasses = [.. FindHandlers().Select(handler => handler.HandlerClass).Distinct()];

            Assert.True(
                handlerClasses.Count >= KnownHandlerCount,
                $"Expected at least {KnownHandlerCount} handler classes in Kvit.Application, found {handlerClasses.Count}: {string.Join(", ", handlerClasses.Select(handlerClass => handlerClass.FullName))}.");
        }

        [Fact]
        public void EveryHandlerInKvitApplication_ResolvesFromTheRealContainerToItsOwnClass()
        {
            using IServiceScope scope = _app.CreateScope();

            foreach ((Type handlerClass, Type handlerInterface) in FindHandlers())
            {
                object resolved = scope.ServiceProvider.GetRequiredService(handlerInterface);

                Assert.IsType(handlerClass, resolved);
            }
        }

        [Fact]
        public async Task Dispatcher_ChangeLanguageCommand_StoresTheNewLanguage()
        {
            using IServiceScope scope = _app.CreateScope();
            AppUser user = await AddUserAsync(scope);
            SignInAs(scope, user.Id);
            IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            Result result = await dispatcher.Send(new ChangeLanguageCommand("mk"), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal("mk", await ReadLanguageAsync(user.Id));
        }

        [Fact]
        public async Task Dispatcher_ChangeLanguageCommandWithUnsupportedLanguage_FailsAndKeepsTheStoredLanguage()
        {
            using IServiceScope scope = _app.CreateScope();
            AppUser user = await AddUserAsync(scope);
            SignInAs(scope, user.Id);
            IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            Result result = await dispatcher.Send(new ChangeLanguageCommand("de"), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.LANGUAGE_INVALID, result.ErrorCode);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal("en", await ReadLanguageAsync(user.Id));
        }

        [Fact]
        public async Task Dispatcher_ChangeLanguageCommandWithoutASignedInUser_IsUnauthorized()
        {
            using IServiceScope scope = _app.CreateScope();
            AppUser user = await AddUserAsync(scope);
            SignInAnonymously(scope);
            IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            Result result = await dispatcher.Send(new ChangeLanguageCommand("mk"), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
            Assert.Equal("en", await ReadLanguageAsync(user.Id));
        }

        private static List<(Type HandlerClass, Type HandlerInterface)> FindHandlers()
        {
            List<(Type HandlerClass, Type HandlerInterface)> handlers = [];

            foreach (Type type in typeof(IDispatcher).Assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
                {
                    continue;
                }

                foreach (Type implemented in type.GetInterfaces())
                {
                    if (implemented.IsGenericType && HandlerInterfaces.Contains(implemented.GetGenericTypeDefinition()))
                    {
                        handlers.Add((type, implemented));
                    }
                }
            }

            return handlers;
        }

        private static async Task<AppUser> AddUserAsync(IServiceScope scope)
        {
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            context.Users.Add(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return user;
        }

        private static void SignInAs(IServiceScope scope, Guid userId)
        {
            Claim[] claims = [new Claim(ClaimTypes.NameIdentifier, userId.ToString())];
            DefaultHttpContext httpContext = new()
            {
                RequestServices = scope.ServiceProvider,
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication")),
            };
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        }

        private static void SignInAnonymously(IServiceScope scope)
        {
            DefaultHttpContext httpContext = new() { RequestServices = scope.ServiceProvider };
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;
        }

        private async Task<string> ReadLanguageAsync(Guid userId)
        {
            using IServiceScope scope = _app.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await context.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.Language)
                .SingleAsync(TestContext.Current.CancellationToken);
        }
    }
}
