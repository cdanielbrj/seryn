using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Modules.Records.Domain;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Records.Application;

internal static class RecordReadModel
{
    public static IQueryable<FinancialRecord> Query(SerynDbContext db) => db.FinancialRecords.AsNoTracking()
        .Include(x => x.Type)
        .Include(x => x.Category)
        .Include(x => x.PaymentMethod);

    public static RecordItem ToItem(FinancialRecord entity) => new(
        entity.Id,
        entity.Name,
        entity.Direction == RecordDirection.Income ? "entrada" : "saida",
        entity.TypeId,
        entity.Type.Name,
        entity.CategoryId,
        entity.Category?.Name,
        entity.PaymentMethodId,
        entity.PaymentMethod.Name,
        entity.AmountCents,
        $"{entity.StartYear:0000}-{entity.StartMonth:00}",
        entity.Recurrence switch
        {
            RecordRecurrence.Once => "unica",
            RecordRecurrence.Monthly => "mensal",
            RecordRecurrence.Quarterly => "trimestral",
            RecordRecurrence.Semiannual => "semestral",
            _ => "anual"
        },
        entity.OccurrenceCount is null ? "indefinida" : "definida",
        entity.OccurrenceCount);
}
