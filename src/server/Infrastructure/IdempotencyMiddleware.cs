using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Persistence;

namespace Seryn.Server.Infrastructure;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    private const string Header = "Idempotency-Key";
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task Invoke(HttpContext context, SerynDbContext db, TimeProvider clock)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !context.User.HasClaim(claim => claim.Type == "api_token") ||
            !context.Request.Headers.TryGetValue(Header, out var values))
        {
            await next(context);
            return;
        }

        var key = values.ToString().Trim();
        if (key.Length is < 8 or > 128 || key.Any(char.IsControl))
        {
            await Results.ValidationProblem(new Dictionary<string, string[]>
            { ["idempotencyKey"] = ["Use uma chave de idempotência entre 8 e 128 caracteres, sem controles."] }).ExecuteAsync(context);
            return;
        }

        var tokenId = Guid.Parse(context.User.FindFirst("api_token")!.Value);
        await Gate.WaitAsync(context.RequestAborted);
        try
        {
            await Process(context, db, clock, tokenId, key);
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task Process(HttpContext context, SerynDbContext db, TimeProvider clock, Guid tokenId, string key)
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
        var requestHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{context.Request.Method}\n{context.Request.Path}\n{context.Request.QueryString}\n{body}")));
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        await db.IdempotencyRecords.Where(x => x.ExpiresAtUnix <= now).ExecuteDeleteAsync();
        var existing = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.ApiTokenId == tokenId && x.Key == key);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                await Results.Problem(statusCode: 409, title: "A chave de idempotência já foi usada com outra requisição.").ExecuteAsync(context);
                return;
            }
            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = existing.ContentType;
            context.Response.Headers["Idempotency-Replayed"] = "true";
            await context.Response.Body.WriteAsync(existing.ResponseBody);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await next(context);
            buffer.Position = 0;
            var responseBody = buffer.ToArray();
            if (context.Response.StatusCode < 500)
            {
                db.IdempotencyRecords.Add(new IdempotencyRecord
                {
                    ApiTokenId = tokenId,
                    Key = key,
                    RequestHash = requestHash,
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType ?? "application/json",
                    ResponseBody = responseBody,
                    ExpiresAtUnix = now + 24 * 60 * 60
                });
                await db.SaveChangesAsync();
            }
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
