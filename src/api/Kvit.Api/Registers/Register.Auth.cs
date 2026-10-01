using Kvit.Api.Authorization;
using Kvit.Domain.Accounts;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Kvit.Api.Registers
{
    public static partial class Register
    {
        private const string AuthCookieName = "kvit_auth";
        private const int AuthCookieDays = 90;

        public static IServiceCollection AddAuth(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();

            services.AddIdentityCore<AppUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.User.AllowedUserNameCharacters = string.Empty;
                    options.Password.RequiredLength = 1;
                    options.Password.RequiredUniqueChars = 1;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = LockoutLadder.FailedTriesBeforeLock;
                    options.Lockout.DefaultLockoutTimeSpan = LockoutLadder.LockDurationFor(1);
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddSignInManager()
                .AddClaimsPrincipalFactory<KvitUserClaimsPrincipalFactory>();

            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = AuthCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromDays(AuthCookieDays);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context => AnswerWithStatus(context.Response, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context => AnswerWithStatus(context.Response, StatusCodes.Status403Forbidden);
            });

            AuthorizationPolicy signedInPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PasswordChangedRequirement())
                .Build();
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = signedInPolicy;
                options.FallbackPolicy = signedInPolicy;
            });
            services.AddSingleton<IAuthorizationMiddlewareResultHandler, MustChangePasswordResultHandler>();

            return services;
        }

        private static Task AnswerWithStatus(HttpResponse response, int statusCode)
        {
            response.StatusCode = statusCode;
            return Task.CompletedTask;
        }
    }
}
