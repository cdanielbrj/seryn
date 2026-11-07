using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Classifications.Application;
using Seryn.Server.Modules.Classifications.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Classifications.PaymentMethods;

internal sealed class PaymentMethodsHandler(SimpleClassificationStore store, SerynDbContext db)
{
    public Task<List<ClassificationItem>> List() => store.List<PaymentMethod>();
    public Task<ClassificationResult<ClassificationItem>> Create(string? name) => store.Create<PaymentMethod>(name);
    public Task<ClassificationResult<ClassificationItem>> Update(Guid id, string? name) => store.Update<PaymentMethod>(id, name);

    public async Task<ClassificationFailure> Delete(Guid id)
    {
        if (await db.FinancialRecords.AnyAsync(x => x.PaymentMethodId == id))
            return ClassificationFailure.InUse;
        return await store.Delete<PaymentMethod>(id);
    }
}
