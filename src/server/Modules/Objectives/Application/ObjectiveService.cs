using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Objectives.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Objectives.Application;

internal sealed class ObjectiveService(SerynDbContext db, TimeProvider clock)
{
    public async Task<List<ObjectiveItem>> List()
    {
        var values = await db.Objectives.AsNoTracking().ToListAsync();
        return values.OrderByDescending(x => InterestRank(x.Interest)).ThenByDescending(x => x.Priority).ThenByDescending(x => x.CreatedAtUnix).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(ToItem).ToList();
    }

    public async Task<ObjectiveItem?> Get(Guid id) => await db.Objectives.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) is { } value ? ToItem(value) : null;

    public async Task<ObjectiveResult> Create(ObjectiveCommand command)
    {
        var prepared = Prepare(command);
        if (prepared.Errors.Count > 0) return new(null, ObjectiveFailure.Invalid, prepared.Errors);
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var entity = new Objective { Id = Guid.NewGuid(), CreatedAtUnix = now, UpdatedAtUnix = now };
        Apply(entity, prepared.Value!);
        db.Objectives.Add(entity);
        await db.SaveChangesAsync();
        return new(ToItem(entity), ObjectiveFailure.None);
    }

    public async Task<ObjectiveResult> Update(Guid id, ObjectiveCommand command)
    {
        var entity = await db.Objectives.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return new(null, ObjectiveFailure.NotFound);
        var prepared = Prepare(command);
        if (prepared.Errors.Count > 0) return new(null, ObjectiveFailure.Invalid, prepared.Errors);
        Apply(entity, prepared.Value!);
        entity.UpdatedAtUnix = clock.GetUtcNow().ToUnixTimeSeconds();
        await db.SaveChangesAsync();
        return new(ToItem(entity), ObjectiveFailure.None);
    }

    public async Task<ObjectiveFailure> Delete(Guid id)
    {
        var entity = await db.Objectives.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return ObjectiveFailure.NotFound;
        db.Objectives.Remove(entity);
        await db.SaveChangesAsync();
        return ObjectiveFailure.None;
    }

    private static (PreparedObjective? Value, Dictionary<string, string[]> Errors) Prepare(ObjectiveCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        var name = command.Nome?.Trim().Normalize(NormalizationForm.FormC) ?? "";
        if (name.Length is < 1 or > 120) errors["nome"] = ["Informe um nome entre 1 e 120 caracteres."];
        if (command.ValorEstimadoCentavos <= 0) errors["valorEstimadoCentavos"] = ["Informe um valor estimado maior que zero."];
        var interest = command.Interesse switch { "listado" => ObjectiveInterest.Listed, "considerado" => ObjectiveInterest.Considered, "desejado" => ObjectiveInterest.Desired, _ => (ObjectiveInterest?)null };
        if (interest is null) errors["interesse"] = ["Escolha listado, considerado ou desejado."];
        if (command.Prioridade is < -3 or > 3) errors["prioridade"] = ["Escolha uma prioridade entre -3 e 3."];
        var link = EmptyToNull(command.Link);
        if (link is not null && (link.Length > 2048 || !Uri.TryCreate(link, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))) errors["link"] = ["Informe um link HTTP ou HTTPS válido."];
        var notes = EmptyToNull(command.Observacoes);
        if (notes?.Length > 2000) errors["observacoes"] = ["Use no máximo 2.000 caracteres."];
        var desired = EmptyToNull(command.DataDesejada);
        var desiredMonth = desired is null ? null : ParseMonth(desired);
        if (desired is not null && desiredMonth is null) errors["dataDesejada"] = ["Informe uma data desejada válida no formato AAAA-MM."];
        return errors.Count > 0 || interest is null ? (null, errors) : (new(name, command.ValorEstimadoCentavos, interest.Value, command.Prioridade, link, notes, desiredMonth?.Year, desiredMonth?.Month), errors);
    }

    private static void Apply(Objective entity, PreparedObjective value)
    {
        entity.Name = value.Name; entity.EstimatedValueCents = value.ValueCents; entity.Interest = value.Interest; entity.Priority = value.Priority;
        entity.Link = value.Link; entity.Notes = value.Notes; entity.DesiredYear = value.DesiredYear; entity.DesiredMonth = value.DesiredMonth;
    }
    private static ObjectiveItem ToItem(Objective value) => new(value.Id, value.Name, value.EstimatedValueCents, InterestName(value.Interest), value.Priority, value.Link, value.Notes,
        value.DesiredYear is { } year && value.DesiredMonth is { } month ? $"{year:0000}-{month:00}" : null,
        DateTimeOffset.FromUnixTimeSeconds(value.CreatedAtUnix).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    private static string InterestName(ObjectiveInterest value) => value switch { ObjectiveInterest.Listed => "listado", ObjectiveInterest.Considered => "considerado", _ => "desejado" };
    private static int InterestRank(ObjectiveInterest value) => value switch { ObjectiveInterest.Desired => 2, ObjectiveInterest.Considered => 1, _ => 0 };
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Normalize(NormalizationForm.FormC);
    private static (int Year, int Month)? ParseMonth(string value) => DateOnly.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && value.Length == 7 ? (date.Year, date.Month) : null;
    private sealed record PreparedObjective(string Name, long ValueCents, ObjectiveInterest Interest, int Priority, string? Link, string? Notes, int? DesiredYear, int? DesiredMonth);
}
