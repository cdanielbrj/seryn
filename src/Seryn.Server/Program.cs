using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Api;
using Seryn.Server.Auth;
using Seryn.Server.Persistence;

if (args.Contains("--healthcheck"))
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    try { return (await client.GetAsync("http://127.0.0.1:8080/health")).IsSuccessStatusCode ? 0 : 1; }
    catch (HttpRequestException) { return 1; }
    catch (TaskCanceledException) { return 1; }
}

var builder = WebApplication.CreateBuilder(args);
var dataDirectory = Path.GetFullPath(builder.Configuration["Seryn:DataDirectory"] ?? "/data");
Directory.CreateDirectory(dataDirectory);
var keysDirectory = Directory.CreateDirectory(Path.Combine(dataDirectory, "keys"));
if (!OperatingSystem.IsWindows())
{
    File.SetUnixFileMode(dataDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    File.SetUnixFileMode(keysDirectory.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
}
var requireHttps = builder.Configuration.GetValue("Seryn:RequireHttps", true);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16 * 1024);
builder.Services.AddDbContext<SerynDbContext>(options => options.UseSqlite(
    new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
    { DataSource = Path.Combine(dataDirectory, "seryn.sqlite"), ForeignKeys = true, DefaultTimeout = 10 }.ToString()));
builder.Services.AddDataProtection().SetApplicationName("Seryn").PersistKeysToFileSystem(keysDirectory);
builder.Services.AddScoped<AdminProvisioner>();
builder.Services.AddSingleton<AuthGate>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IPasswordHasher<LocalUser>, PasswordHasher<LocalUser>>();
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi("v1");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    // Keep a nonempty explicit allowlist: empty lists would trust every proxy.
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
    options.Events.OnValidatePrincipal = async context =>
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<SerynDbContext>();
        var clock = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
        var id = context.Principal?.FindFirstValue("session");
        if (!await db.Sessions.AnyAsync(x => x.Id == id && x.ExpiresAtUnix > clock.GetUtcNow().ToUnixTimeSeconds()))
            context.RejectPrincipal();
    };
});
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
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
    { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        return new ValueTask(Results.Problem(statusCode: 429, title: "Muitas tentativas. Aguarde um minuto.").ExecuteAsync(context.HttpContext));
    };
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SerynDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    await scope.ServiceProvider.GetRequiredService<AdminProvisioner>().Provision();
    // Remove obsolete setup secrets when upgrading the previous first-access flow.
    File.Delete(Path.Combine(dataDirectory, "bootstrap.token"));
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
    context.Response.Headers.CacheControl = "no-store";
    if (context.Request.IsHttps) context.Response.Headers.StrictTransportSecurity = "max-age=31536000";
    if (requireHttps && !context.Request.IsHttps && context.Request.Path.StartsWithSegments("/api"))
    {
        await Results.Problem(statusCode: 400, title: "Use HTTPS para acessar a aplicação.").ExecuteAsync(context);
        return;
    }
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
    {
        try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException)
        {
            await Results.Problem(statusCode: 400, title: "A verificação de segurança expirou. Atualize a página e tente novamente.").ExecuteAsync(context);
            return;
        }
    }
    await next();
});
app.MapGet("/health", async (SerynDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ok" }) : Results.StatusCode(503)).AllowAnonymous().ExcludeFromDescription();
var api = app.MapGroup("/api/v1").RequireAuthorization();
api.MapAuth();
api.MapGet("/foundation", () => new { phase = 1, financialDataAvailable = false }).WithTags("Fundação");
app.MapOpenApi("/api/v1/openapi/{documentName}.json").RequireAuthorization();
app.Map("/api/{**path}", () => Results.Problem(statusCode: 404, title: "Endpoint não disponível.")).RequireAuthorization();
app.MapFallbackToFile("index.html").AllowAnonymous();
await app.RunAsync();
return 0;

public partial class Program;
