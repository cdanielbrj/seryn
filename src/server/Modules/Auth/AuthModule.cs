using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Auth.Application;
using Seryn.Server.Modules.Auth.Contracts;
using Seryn.Server.Modules.Auth.Domain;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddScoped<AdminProvisioner>();
        services.AddSingleton<AuthGate>();
        services.AddScoped<AuthService>();
        services.AddScoped<IPasswordHasher<LocalUser>, PasswordHasher<LocalUser>>();
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
        return services;
    }

    public static void MapAuthModule(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Autenticação");
        auth.MapGet("/status", async (SerynDbContext db) => new AuthStatus(await db.Users.AnyAsync())).AllowAnonymous();
        auth.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
            new CsrfResponse(antiforgery.GetAndStoreTokens(context).RequestToken!)).AllowAnonymous();

        auth.MapPost("/login", async (LoginRequest request, AuthService service, HttpContext context) =>
        {
            if (!Credentials.Valid(request.Username, request.Password))
                return Results.Problem(statusCode: 400, title: "Informe usuário e senha válidos.");
            var user = await service.Login(request.Username!, request.Password!);
            if (user is null)
                return Results.Problem(statusCode: 401, title: "Não foi possível entrar. Confira suas credenciais ou aguarde alguns minutos antes de tentar novamente.");
            var session = await service.CreateSession();
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, user.Username),
                new Claim("session", session.Id)
            ], CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.FromUnixTimeSeconds(session.ExpiresAtUnix) });
            return Results.Ok(new SessionResponse(user.Username));
        }).AllowAnonymous().RequireRateLimiting("auth");

        auth.MapGet("/session", (HttpContext context) => new SessionResponse(context.User.Identity!.Name!));
        auth.MapPost("/logout", async (HttpContext context, SerynDbContext db) =>
        {
            var id = context.User.FindFirstValue("session");
            await db.Sessions.Where(x => x.Id == id).ExecuteDeleteAsync();
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });
    }

}
