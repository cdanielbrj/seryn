using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Records.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Records.DeleteRecord;

internal sealed class DeleteRecordHandler(SerynDbContext db)
{
    public async Task<RecordFailure> Handle(Guid id)
    {
        var entity = await db.FinancialRecords.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return RecordFailure.NotFound;

        db.FinancialRecords.Remove(entity);
        await db.SaveChangesAsync();
        return RecordFailure.None;
    }
}
