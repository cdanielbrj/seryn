using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Persistence;
using Seryn.Server.Lumen.SerynLink;
using Seryn.Server.Modules.Auth.Application;

namespace Seryn.Server.Infrastructure;

public static class SecurityConfiguration
{
    public static WebApplicationBuilder AddSerynSecurity(this WebApplicationBuilder builder)
    {
        var requireHttps = builder.Configuration.GetValue("Seryn:RequireHttps", true);
        ValidateTransportConfiguration(builder.Configuration, requireHttps);
        var loginLimit = Positive(builder.Configuration, "Seryn:Security:LoginRateLimit", 10);
        var apiLimit = Positive(builder.Configuration, "Seryn:Security:ApiRateLimit", 120);
        _ = Positive(builder.Configuration, "Seryn:Security:LoginLockoutAttempts", 5);
        _ = Positive(builder.Configuration, "Seryn:Security:LoginLockoutSeconds", 300);
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
            foreach (var proxy in builder.Configuration.GetSection("Seryn:TrustedProxies").Get<string[]>() ?? [])
                options.KnownProxies.Add(IPAddress.Parse(proxy));
        });
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "SerynAuthentication";
            options.DefaultChallengeScheme = "SerynAuthentication";
        }).AddPolicyScheme("SerynAuthentication", null, options =>
        {
            options.ForwardDefaultSelector = context => context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? SerynLinkAuthenticationHandler.SchemeName
                : CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "Seryn.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = requireHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context => Results.Problem(statusCode: 401, title: "Entre para continuar.").ExecuteAsync(context.HttpContext);
            options.Events.OnRedirectToAccessDenied = context => Results.Problem(statusCode: 403, title: "Acesso negado.").ExecuteAsync(context.HttpContext);
            options.Events.OnValidatePrincipal = ValidateSession;
        }).AddScheme<AuthenticationSchemeOptions, SerynLinkAuthenticationHandler>(SerynLinkAuthenticationHandler.SchemeName, null);
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy("admin-session", policy => policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme).RequireAuthenticatedUser());
            AddDataPolicy(options, SerynLinkScopes.Read);
            AddDataPolicy(options, SerynLinkScopes.Write);
            AddDataPolicy(options, SerynLinkScopes.Delete);
        });
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "Seryn.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = requireHttps ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("auth", _ => RateLimitPartition.GetFixedWindowLimiter("single-user-auth", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
            options.AddPolicy("api", context =>
            {
                var partition = context.User.FindFirstValue("seryn_link") ?? "browser-session";
                return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = apiLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                return new ValueTask(Results.Problem(statusCode: 429, title: "Muitas tentativas. Aguarde um minuto.").ExecuteAsync(context.HttpContext));
            };
        });
        return builder;
    }

    private static int Positive(IConfiguration configuration, string key, int fallback)
    {
        var value = configuration.GetValue(key, fallback);
        if (value <= 0) throw new InvalidOperationException($"{key} deve ser maior que zero.");
        return value;
    }

    private static void ValidateTransportConfiguration(IConfiguration configuration, bool requireHttps)
    {
        var publishedAddress = configuration["Seryn:PublishedBindAddress"];
        if (string.IsNullOrWhiteSpace(publishedAddress)) return;
        if (!IPAddress.TryParse(publishedAddress, out var address))
            throw new InvalidOperationException("Seryn:PublishedBindAddress deve ser um endereço IP válido.");
        var local = IPAddress.IsLoopback(address);
        var allowInsecureRemote = configuration.GetValue("Seryn:AllowInsecureRemote", false);
        if (!local && !requireHttps && !allowInsecureRemote)
            throw new InvalidOperationException("A publicação HTTP fora do loopback exige HTTPS ou SERYN_ALLOW_INSECURE_REMOTE=true.");
    }

    private static void AddDataPolicy(AuthorizationOptions options, string scope) =>
        options.AddPolicy(scope, policy => policy.RequireAssertion(context =>
            context.User.Identity?.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme ||
            context.User.HasClaim("scope", scope)));

    private static async Task ValidateSession(CookieValidatePrincipalContext context)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<SerynDbContext>();
        var clock = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
        var id = context.Principal?.FindFirstValue("session");
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var session = await db.Sessions.SingleOrDefaultAsync(x => x.Id == id && x.ExpiresAtUnix > now);
        if (session is null)
            context.RejectPrincipal();
        else if (session.LastUsedAtUnix < now - 3600)
        {
            session.LastUsedAtUnix = now;
            await db.SaveChangesAsync();
        }
    }
}
