using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Records.Application;
using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Records.GetRecord;

internal sealed class GetRecordHandler(SerynDbContext db)
{
    public async Task<RecordItem?> Handle(Guid id)
    {
        var entity = await RecordReadModel.Query(db).SingleOrDefaultAsync(x => x.Id == id);
        return entity is null ? null : RecordReadModel.ToItem(entity);
    }
}
