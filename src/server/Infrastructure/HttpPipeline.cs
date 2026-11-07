using Microsoft.AspNetCore.Antiforgery;
using Seryn.Server.Persistence;

namespace Seryn.Server.Infrastructure;

public static class HttpPipeline
{
    public static WebApplication UseSerynHttpPipeline(this WebApplication app)
    {
        var requireHttps = app.Configuration.GetValue("Seryn:RequireHttps", true);
        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.Use(async (context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
            context.Response.Headers.CacheControl = "no-store";
            if (context.Request.IsHttps)
                context.Response.Headers.StrictTransportSecurity = "max-age=31536000";
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
        app.Use(ValidateAntiforgery);
        return app;
    }

    public static void MapSerynInfrastructure(this WebApplication app)
    {
        app.MapGet("/health", async (SerynDbContext db) =>
            await db.Database.CanConnectAsync()
                ? Results.Ok(new { status = "ok" })
                : Results.StatusCode(503))
            .AllowAnonymous()
            .ExcludeFromDescription();
        app.MapOpenApi("/api/v1/openapi/{documentName}.json").RequireAuthorization();
        app.Map("/api/{**path}", () => Results.Problem(statusCode: 404, title: "Endpoint não disponível."))
            .RequireAuthorization();
        app.MapFallbackToFile("index.html").AllowAnonymous();
    }

    private static async Task ValidateAntiforgery(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/api") &&
            !HttpMethods.IsGet(context.Request.Method) &&
            !HttpMethods.IsHead(context.Request.Method) &&
            !HttpMethods.IsOptions(context.Request.Method))
        {
            try
            {
                await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                await Results.Problem(statusCode: 400, title: "A verificação de segurança expirou. Atualize a página e tente novamente.").ExecuteAsync(context);
                return;
            }
        }
        await next(context);
    }
}
