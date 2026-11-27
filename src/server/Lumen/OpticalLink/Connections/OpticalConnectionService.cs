using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Lumen.Core.Providers;
using Seryn.Server.Lumen.OpticalLink.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Lumen.OpticalLink.Connections;

internal sealed record ResolvedOpticalConnection(string Provider, string Model, string ApiKey, string? Name);

internal sealed class OpticalConnectionService(SerynDbContext db, IDataProtectionProvider protection, TimeProvider clock)
{
    private const int MaximumConnections = 10;
    private readonly IDataProtector protector = protection.CreateProtector("Seryn.OpticalLink.ApiKeys.v1");

    public async Task<List<OpticalConnectionItem>> List(CancellationToken cancellationToken) =>
        (await db.OpticalLinkConnections.AsNoTracking().OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken)).Select(Item).ToList();

    public async Task<(OpticalConnectionItem? Value, Dictionary<string, string[]>? Errors)> Create(OpticalConnectionRequest request, CancellationToken cancellationToken)
    {
        var values = Validate(request);
        if (values.Errors.Count > 0) return (null, values.Errors);
        if (await db.OpticalLinkConnections.CountAsync(cancellationToken) >= MaximumConnections)
            return (null, new() { ["conexoes"] = ["Remova uma conexão antes de adicionar outra; o limite é 10."] });
        if (await db.OpticalLinkConnections.AnyAsync(x => x.Name == values.Name, cancellationToken))
            return (null, new() { ["nome"] = ["Já existe uma conexão com este nome."] });

        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var makeDefault = request.Padrao || !await db.OpticalLinkConnections.AnyAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (makeDefault) await db.OpticalLinkConnections.Where(x => x.IsDefault).ExecuteUpdateAsync(update => update.SetProperty(x => x.IsDefault, false), cancellationToken);
        var entity = new OpticalLinkConnection
        {
            Id = Guid.NewGuid(),
            Name = values.Name,
            Provider = values.Provider,
            Model = values.Model,
            ProtectedApiKey = protector.Protect(values.ApiKey),
            KeyHint = Hint(values.ApiKey),
            IsDefault = makeDefault,
            CreatedAtUnix = now,
            UpdatedAtUnix = now
        };
        db.OpticalLinkConnections.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (Item(entity), null);
    }

    public async Task<(OpticalConnectionItem? Value, Dictionary<string, string[]>? Errors, bool Found)> Update(Guid id, OpticalConnectionRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.OpticalLinkConnections.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return (null, null, false);
        var values = Validate(request, requireKey: false);
        if (values.Errors.Count > 0) return (null, values.Errors, true);
        if (await db.OpticalLinkConnections.AnyAsync(x => x.Id != id && x.Name == values.Name, cancellationToken))
            return (null, new() { ["nome"] = ["Já existe uma conexão com este nome."] }, true);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (request.Padrao) await db.OpticalLinkConnections.Where(x => x.IsDefault && x.Id != id).ExecuteUpdateAsync(update => update.SetProperty(x => x.IsDefault, false), cancellationToken);
        entity.Name = values.Name; entity.Provider = values.Provider; entity.Model = values.Model;
        if (values.ApiKey.Length > 0) { entity.ProtectedApiKey = protector.Protect(values.ApiKey); entity.KeyHint = Hint(values.ApiKey); }
        entity.IsDefault = request.Padrao || entity.IsDefault;
        entity.UpdatedAtUnix = clock.GetUtcNow().ToUnixTimeSeconds();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (Item(entity), null, true);
    }

    public async Task<bool> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.OpticalLinkConnections.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var wasDefault = entity.IsDefault;
        db.OpticalLinkConnections.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        if (wasDefault)
        {
            var replacement = await db.OpticalLinkConnections.OrderBy(x => x.CreatedAtUnix).FirstOrDefaultAsync(cancellationToken);
            if (replacement is not null) { replacement.IsDefault = true; replacement.UpdatedAtUnix = clock.GetUtcNow().ToUnixTimeSeconds(); await db.SaveChangesAsync(cancellationToken); }
        }
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<ResolvedOpticalConnection?> Resolve(CancellationToken cancellationToken)
    {
        var entity = await db.OpticalLinkConnections.AsNoTracking().SingleOrDefaultAsync(x => x.IsDefault, cancellationToken);
        if (entity is not null)
        {
            try { return new(entity.Provider, entity.Model, protector.Unprotect(entity.ProtectedApiKey), entity.Name); }
            catch (CryptographicException) { throw new ChatFailure(502, "A credencial da conexão não pôde ser lida. Substitua a chave no Optical Link."); }
        }
        return null;
    }

    private static (string Name, string Provider, string Model, string ApiKey, Dictionary<string, string[]> Errors) Validate(OpticalConnectionRequest request, bool requireKey = true)
    {
        var name = request.Nome?.Trim() ?? ""; var provider = request.Provedor?.Trim().ToLowerInvariant() ?? "";
        var model = request.Modelo?.Trim() ?? ""; var apiKey = request.Chave?.Trim() ?? "";
        var errors = new Dictionary<string, string[]>();
        if (name.Length is < 1 or > 80) errors["nome"] = ["Informe um nome entre 1 e 80 caracteres."];
        if (provider != "openrouter") errors["provedor"] = ["Escolha um provedor disponível."];
        if (model.Length is < 1 or > 160) errors["modelo"] = ["Informe um modelo entre 1 e 160 caracteres."];
        if ((requireKey && apiKey.Length < 8) || apiKey.Length > 2048) errors["chave"] = ["Informe uma chave de API válida com até 2.048 caracteres."];
        return (name, provider, model, apiKey, errors);
    }

    private static string Hint(string value) => value.Length <= 8 ? "••••" : $"{value[..4]}••••{value[^4..]}";
    private static OpticalConnectionItem Item(OpticalLinkConnection value) => new(value.Id, value.Name, value.Provider, value.Model, value.KeyHint, value.IsDefault, Format(value.CreatedAtUnix), Format(value.UpdatedAtUnix));
    private static string Format(long value) => DateTimeOffset.FromUnixTimeSeconds(value).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
