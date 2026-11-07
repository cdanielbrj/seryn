using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Persistence;

namespace Seryn.Server.Infrastructure;

public static class SecurityConfiguration
{
    public static WebApplicationBuilder AddSerynSecurity(this WebApplicationBuilder builder)
    {
        var requireHttps = builder.Configuration.GetValue("Seryn:RequireHttps", true);
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
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
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
        });
        builder.Services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
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
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                return new ValueTask(Results.Problem(statusCode: 429, title: "Muitas tentativas. Aguarde um minuto.").ExecuteAsync(context.HttpContext));
            };
        });
        return builder;
    }

    private static async Task ValidateSession(CookieValidatePrincipalContext context)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<SerynDbContext>();
        var clock = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
        var id = context.Principal?.FindFirstValue("session");
        if (!await db.Sessions.AnyAsync(x => x.Id == id && x.ExpiresAtUnix > clock.GetUtcNow().ToUnixTimeSeconds()))
            context.RejectPrincipal();
    }
}
