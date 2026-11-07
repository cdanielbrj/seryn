using Seryn.Server.Modules.Records.Application;
using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Modules.Records.GetRecord;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Records.CreateRecord;

internal sealed class CreateRecordHandler(
    SerynDbContext db,
    RecordCommandProcessor processor,
    GetRecordHandler getRecord,
    TimeProvider clock)
{
    public async Task<RecordResult> Handle(RecordCommand command)
    {
        var prepared = await processor.Prepare(command);
        if (prepared.Errors.Count > 0)
            return new(null, RecordFailure.Invalid, prepared.Errors);

        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var entity = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            CreatedAtUnix = now,
            UpdatedAtUnix = now
        };
        RecordCommandProcessor.Apply(entity, prepared.Value!);
        db.FinancialRecords.Add(entity);
        await db.SaveChangesAsync();
        return new(await getRecord.Handle(entity.Id), RecordFailure.None);
    }
}
