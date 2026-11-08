using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Records.Application;
using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Records.ListRecords;

internal sealed class ListRecordsHandler(SerynDbContext db)
{
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
