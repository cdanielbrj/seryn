using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Seryn.Server.Api;
using Seryn.Server.Persistence;

namespace Seryn.Tests;

public sealed class TestApplication(string directory, bool httpsRequired = false,
    string username = "daniel", string? password = "Uma frase de teste segura 123!", string? passwordFile = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("SERYN_ADMIN_USER", username);
        builder.UseSetting("SERYN_ADMIN_PASSWORD", password ?? "");
        builder.UseSetting("SERYN_ADMIN_PASSWORD_FILE", passwordFile ?? "");
        builder.UseSetting("Seryn:DataDirectory", directory);
        builder.UseSetting("Seryn:RequireHttps", httpsRequired.ToString());
        builder.UseSetting("Logging:LogLevel:Default", "Error");
    }
}

public sealed class FoundationTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "seryn-test-" + Guid.NewGuid());
    private const string Password = "Uma frase de teste segura 123!";

    private static async Task<HttpResponseMessage> Post(HttpClient client, string endpoint, object data)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/" + endpoint) { Content = JsonContent.Create(data) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Anonymous_access_is_denied_for_all_financial_paths_and_openapi()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        foreach (var path in new[] { "records", "types", "categories", "projections", "import/preview", "export/records", "backups", "foundation", "openapi/v1.json", "unknown" })
        {
            var response = await client.GetAsync("/api/v1/" + path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.True(response.Headers.CacheControl!.NoStore);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        var blockedWrite = await client.PostAsJsonAsync("/api/v1/records", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, blockedWrite.StatusCode);
    }

    [Fact]
    public async Task Initial_credentials_are_hashed_and_obsolete_setup_is_removed()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "bootstrap.token"), "obsolete-code");
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        Assert.True((await client.GetFromJsonAsync<AuthStatus>("/api/v1/auth/status"))!.Configured);
        Assert.False(File.Exists(Path.Combine(directory, "bootstrap.token")));
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SerynDbContext>();
        var user = await db.Users.SingleAsync();
        Assert.Equal("daniel", user.Username);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.DoesNotContain(Password, user.PasswordHash);
        var migrations = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20261103191308_Foundation", migrations);
        Assert.Contains("20261103225105_Classifications", migrations);
        Assert.Contains("20261103232541_CategoryHierarchy", migrations);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "daniel", password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post(client, "login", new { username = "daniel", password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(client, "setup", new { username = "intruder", password = Password })).StatusCode);
        var openapi = await client.GetStringAsync("/api/v1/openapi/v1.json");
        Assert.DoesNotContain("/api/v1/auth/setup", openapi);
    }

    [Fact]
    public async Task Missing_password_keeps_access_closed_until_configured_on_restart()
    {
        await using (var app = new TestApplication(directory, password: null))
        {
            using var client = app.CreateClient();
            Assert.False((await client.GetFromJsonAsync<AuthStatus>("/api/v1/auth/status"))!.Configured);
            Assert.False(File.Exists(Path.Combine(directory, "bootstrap.token")));
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "setup", new { username = "intruder", password = Password })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "login", new { username = "daniel", password = Password })).StatusCode);
            using var scope = app.Services.CreateScope();
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<SerynDbContext>().Users.ToListAsync());
        }
        await using var configured = new TestApplication(directory);
        using var configuredClient = configured.CreateClient();
        Assert.True((await configuredClient.GetFromJsonAsync<AuthStatus>("/api/v1/auth/status"))!.Configured);
        Assert.Equal(HttpStatusCode.OK, (await Post(configuredClient, "login", new { username = "daniel", password = Password })).StatusCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Outra senha longa e diferente!", null)]
    [InlineData("short", "/missing/secret")]
    public async Task Existing_database_ignores_changed_missing_or_invalid_provisioning(string? newPassword, string? passwordFile)
    {
        string originalHash;
        await using (var original = new TestApplication(directory))
        {
            using var scope = original.Services.CreateScope();
            originalHash = (await scope.ServiceProvider.GetRequiredService<SerynDbContext>().Users.SingleAsync()).PasswordHash;
        }
        await using var restarted = new TestApplication(directory, username: "replacement", password: newPassword, passwordFile: passwordFile);
        using var client = restarted.CreateClient();
        using var newScope = restarted.Services.CreateScope();
        var user = await newScope.ServiceProvider.GetRequiredService<SerynDbContext>().Users.SingleAsync();
        Assert.Equal("daniel", user.Username);
        Assert.Equal(originalHash, user.PasswordHash);
        Assert.Equal(HttpStatusCode.OK, (await Post(client, "login", new { username = "daniel", password = Password })).StatusCode);
    }

    [Theory]
    [InlineData("daniel", "short")]
    [InlineData("daniel", "                ")]
    [InlineData(" ", "Uma frase de teste segura 123!")]
    [InlineData("user\nname", "Uma frase de teste segura 123!")]
    public async Task Invalid_initial_credentials_fail_without_creating_user(string username, string password)
    {
        await using var app = new TestApplication(directory, username: username, password: password);
        var exception = Assert.Throws<InvalidOperationException>(() => app.CreateClient());
        Assert.Contains("Credenciais iniciais inválidas", exception.Message);
        Assert.DoesNotContain(password, exception.Message);
        await using var retry = new TestApplication(directory, password: null);
        using var client = retry.CreateClient();
        Assert.False((await client.GetFromJsonAsync<AuthStatus>("/api/v1/auth/status"))!.Configured);
    }

    [Fact]
    public async Task File_secret_preserves_spaces_and_only_removes_line_ending()
    {
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "admin-secret");
        var password = "  " + Password + "  ";
        await File.WriteAllTextAsync(file, password + "\r\n");
        await using var app = new TestApplication(directory, password: null, passwordFile: file);
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await Post(client, "login", new { username = "daniel", password })).StatusCode);
    }

    [Theory]
    [InlineData(null, "missing")]
    [InlineData("Uma frase de teste segura 123!", "missing")]
    public async Task Missing_secret_file_or_conflicting_sources_fail_closed(string? password, string file)
    {
        await using var app = new TestApplication(directory, password: password, passwordFile: Path.Combine(directory, file));
        Assert.Throws<InvalidOperationException>(() => app.CreateClient());
    }

    [Fact]
    public async Task Cookie_session_is_protected_logout_requires_csrf_and_revokes_cookie()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await Post(client, "login", new { username = "daniel", password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("Seryn.Session="));
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Equal("daniel", (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!.Username);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/foundation")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/records")).StatusCode);
        var openapi = await client.GetStringAsync("/api/v1/openapi/v1.json");
        Assert.Contains("/api/v1/auth/login", openapi);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(client, "logout", new { })).StatusCode);
        using var replay = app.CreateClient(new() { HandleCookies = false });
        replay.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/v1/foundation")).StatusCode);
    }

    [Fact]
    public async Task Migration_user_session_and_keys_survive_restart()
    {
        string cookie;
        await using (var app = new TestApplication(directory))
        {
            using var client = app.CreateClient();
            var login = await Post(client, "login", new { username = "daniel", password = Password });
            cookie = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("Seryn.Session=")).Split(';')[0];
        }
        await using (var restarted = new TestApplication(directory, password: null))
        {
            using var client = restarted.CreateClient(new() { HandleCookies = false });
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            Assert.True((await client.GetFromJsonAsync<AuthStatus>("/api/v1/auth/status"))!.Configured);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/session")).StatusCode);
            Assert.False(File.Exists(Path.Combine(directory, "bootstrap.token")));
            using var scope = restarted.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SerynDbContext>();
            Assert.False(db.Database.HasPendingModelChanges());
            await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA journal_mode";
            Assert.Equal("wal", await command.ExecuteScalarAsync());
        }
    }

    [Fact]
    public async Task Failed_attempts_lock_the_account_and_survive_restart()
    {
        await using (var app = new TestApplication(directory))
        {
            using var client = app.CreateClient();
            for (var i = 0; i < 5; i++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "login", new { username = "daniel", password = "invalid" })).StatusCode);
        }
        await using (var app = new TestApplication(directory))
        {
            using var client = app.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "login", new { username = "daniel", password = Password })).StatusCode);
        }
    }

    [Fact]
    public async Task Authentication_has_global_rate_limit()
    {
        await using var app = new TestApplication(directory);
        using var client = app.CreateClient();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "login", new { username = "nobody", password = Password })).StatusCode);
        var limited = await Post(client, "login", new { username = "nobody", password = Password });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(60), limited.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task Https_mode_refuses_plain_http()
    {
        await using var app = new TestApplication(directory, httpsRequired: true);
        using var plain = app.CreateClient(new() { BaseAddress = new Uri("http://localhost") });
        Assert.Equal(HttpStatusCode.BadRequest, (await plain.GetAsync("/api/v1/auth/csrf")).StatusCode);
        using var secure = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.OK, (await secure.GetAsync("/api/v1/auth/csrf")).StatusCode);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
