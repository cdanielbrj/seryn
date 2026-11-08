using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Records.Application;
using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Modules.Records.GetRecord;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Records.UpdateRecord;

internal sealed class UpdateRecordHandler(
    SerynDbContext db,
    RecordCommandProcessor processor,
    GetRecordHandler getRecord,
    TimeProvider clock)
{
    public async Task<RecordResult> Handle(Guid id, RecordCommand command)
    {
        var entity = await db.FinancialRecords.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return new(null, RecordFailure.NotFound);

        var prepared = await processor.Prepare(command);
        if (prepared.Errors.Count > 0)
            return new(null, RecordFailure.Invalid, prepared.Errors);

        RecordCommandProcessor.Apply(entity, prepared.Value!);
        entity.UpdatedAtUnix = clock.GetUtcNow().ToUnixTimeSeconds();
        await db.SaveChangesAsync();
        return new(await getRecord.Handle(entity.Id), RecordFailure.None);
    }
}
