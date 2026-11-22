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
        services.AddScoped<ApiTokenService>();
        services.AddScoped<IPasswordHasher<LocalUser>, PasswordHasher<LocalUser>>();
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
        return services;
    }

    public static void MapAuthModule(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Autenticação");
        auth.MapGet("/status", async (SerynDbContext db) => new AuthStatus(await db.Users.AnyAsync())).AllowAnonymous()
            .WithName("GetAuthenticationStatus").WithSummary("Informa se a instalação possui administrador");
        auth.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
            new CsrfResponse(antiforgery.GetAndStoreTokens(context).RequestToken!)).AllowAnonymous()
            .WithName("GetCsrfToken").WithSummary("Obtém proteção CSRF para a sessão interativa");

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
        }).AllowAnonymous().RequireRateLimiting("auth").WithName("Login").WithSummary("Inicia uma sessão administrativa interativa");

        auth.MapGet("/session", (HttpContext context) => new SessionResponse(context.User.Identity!.Name!)).RequireAuthorization("admin-session")
            .WithName("GetSession").WithSummary("Obtém a sessão administrativa atual");
        auth.MapPost("/logout", async (HttpContext context, SerynDbContext db) =>
        {
            var id = context.User.FindFirstValue("session");
            await db.Sessions.Where(x => x.Id == id).ExecuteDeleteAsync();
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization("admin-session").WithName("Logout").WithSummary("Revoga a sessão administrativa atual");

        var tokens = auth.MapGroup("/tokens").RequireAuthorization("admin-session");
        tokens.MapGet("/", (ApiTokenService service) => service.List())
            .WithName("ListApiTokens").WithSummary("Lista tokens de integração ativos");
        tokens.MapPost("/", async (ApiTokenRequest request, ApiTokenService service) =>
        {
            var result = await service.Create(request);
            return result.Errors is null ? Results.Created($"./{result.Value!.Id}", result.Value) : Results.ValidationProblem(result.Errors);
        }).WithName("CreateApiToken").WithSummary("Cria um token de integração")
            .WithDescription("O segredo é retornado uma única vez. Os escopos não podem ser alterados depois da criação.");
        tokens.MapDelete("/{id:guid}", async (Guid id, ApiTokenService service) =>
            await service.Revoke(id) ? Results.NoContent() : Results.Problem(statusCode: 404, title: "Token não encontrado."))
            .WithName("RevokeApiToken").WithSummary("Revoga um token de integração");
    }

}
