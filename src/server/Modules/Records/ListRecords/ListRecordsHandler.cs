using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Records.Application;
using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Records.ListRecords;

internal sealed class ListRecordsHandler(SerynDbContext db)
{
    public async Task<(List<RecordItem> Items, bool HasMore)> Search(string search, string? direction, int offset, CancellationToken cancellationToken)
    {
        var query = RecordReadModel.Query(db);
        if (direction is not null)
        {
            var value = direction == "entrada" ? Domain.RecordDirection.Income : Domain.RecordDirection.Expense;
            query = query.Where(record => record.Direction == value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(record => record.Name.ToLower().Contains(term) || record.Type.Name.ToLower().Contains(term)
                || record.PaymentMethod.Name.ToLower().Contains(term) || record.Category != null && record.Category.Name.ToLower().Contains(term));
        }
        var records = await query.OrderByDescending(record => record.AmountCents).ThenBy(record => record.Id)
            .Skip(offset).Take(21).ToListAsync(cancellationToken);
        return (records.Take(20).Select(RecordReadModel.ToItem).ToList(), records.Count > 20);
    }

    public async Task<List<RecordItem>> Handle()
    {
        var records = await RecordReadModel.Query(db)
            .OrderByDescending(x => x.StartYear)
            .ThenByDescending(x => x.StartMonth)
            .ThenBy(x => x.Name)
            .ToListAsync();
        return records.Select(RecordReadModel.ToItem).ToList();
    }
}
