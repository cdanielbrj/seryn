using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Classifications.Application;
using Seryn.Server.Modules.Classifications.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Classifications.Types;

internal sealed class TypesHandler(SimpleClassificationStore store, SerynDbContext db)
{
    public Task<List<ClassificationItem>> List() => store.List<FinancialType>();
    public Task<ClassificationResult<ClassificationItem>> Create(string? name) => store.Create<FinancialType>(name);
    public Task<ClassificationResult<ClassificationItem>> Update(Guid id, string? name) => store.Update<FinancialType>(id, name);

    public async Task<ClassificationFailure> Delete(Guid id)
    {
        if (await db.FinancialRecords.AnyAsync(x => x.TypeId == id))
            return ClassificationFailure.InUse;
        return await store.Delete<FinancialType>(id);
    }
}
