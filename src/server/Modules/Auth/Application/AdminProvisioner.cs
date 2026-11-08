using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Auth.Domain;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Auth.Application;

// Runs once during startup, before HTTP requests are accepted. Existing SQLite
// credentials (including restored databases) always take precedence over configuration.
public sealed class AdminProvisioner(SerynDbContext db, IConfiguration configuration,
    IPasswordHasher<LocalUser> hasher, ILogger<AdminProvisioner> logger)
{
    public async Task Provision()
    {
        if (await db.Users.AnyAsync()) return;

        var password = configuration["SERYN_ADMIN_PASSWORD"];
        var passwordFile = configuration["SERYN_ADMIN_PASSWORD_FILE"];
        if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(passwordFile))
            throw new InvalidOperationException("Defina apenas SERYN_ADMIN_PASSWORD ou SERYN_ADMIN_PASSWORD_FILE.");

        if (!string.IsNullOrEmpty(passwordFile))
        {
            try
            {
                await using var file = File.OpenRead(passwordFile);
                if (file.Length > 1024)
                    throw new InvalidOperationException("O arquivo de senha inicial deve ter no máximo 1024 bytes.");
                using var reader = new StreamReader(file);
                password = (await reader.ReadToEndAsync()).TrimEnd('\r', '\n');
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Do not include secret contents, paths or configuration values in logs.
                throw new InvalidOperationException("Não foi possível ler SERYN_ADMIN_PASSWORD_FILE. Verifique o arquivo e suas permissões.");
            }
        }
        else if (string.IsNullOrEmpty(password))
        {
            logger.LogWarning("Administrador não configurado. Defina SERYN_ADMIN_USER e SERYN_ADMIN_PASSWORD (ou SERYN_ADMIN_PASSWORD_FILE) e recrie o container. O acesso permanece bloqueado.");
            return;
        }

        var username = configuration["SERYN_ADMIN_USER"] ?? "admin";
        if (!Credentials.ValidInitial(username, password))
            throw new InvalidOperationException("Credenciais iniciais inválidas: usuário de 1 a 64 caracteres, sem controles, e senha de 16 a 128 caracteres, não composta apenas de espaços.");

        var user = new LocalUser { Username = username.Trim() };
        user.PasswordHash = hasher.HashPassword(user, password!);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        logger.LogInformation("Administrador provisionado. Use a tela de login; as credenciais existentes serão preservadas nas próximas inicializações.");
    }
}
