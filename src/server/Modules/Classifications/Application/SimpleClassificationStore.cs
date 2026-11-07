using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Classifications.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Classifications.Application;

internal sealed class SimpleClassificationStore(SerynDbContext db)
{
    public async Task<List<ClassificationItem>> List<T>() where T : class, IClassification =>
        await db.Set<T>().AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new ClassificationItem(x.Id, x.Name)).ToListAsync();

    public async Task<ClassificationResult<ClassificationItem>> Create<T>(string? input)
        where T : class, IClassification, new()
    {
        var prepared = ClassificationName.Prepare(input);
        if (prepared is null)
            return new(null, ClassificationFailure.InvalidName);
        if (await db.Set<T>().AnyAsync(x => x.NormalizedName == prepared.Value.Normalized))
            return new(null, ClassificationFailure.Duplicate);

        var entity = new T
        {
            Id = Guid.NewGuid(),
            Name = prepared.Value.Name,
            NormalizedName = prepared.Value.Normalized
        };
        db.Add(entity);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Detached;
            return new(null, ClassificationFailure.Duplicate);
        }
        return new(new(entity.Id, entity.Name), ClassificationFailure.None);
    }

    public async Task<ClassificationResult<ClassificationItem>> Update<T>(Guid id, string? input)
        where T : class, IClassification
    {
        var prepared = ClassificationName.Prepare(input);
        if (prepared is null)
            return new(null, ClassificationFailure.InvalidName);

        var entity = await db.Set<T>().SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return new(null, ClassificationFailure.NotFound);
        if (await db.Set<T>().AnyAsync(x => x.Id != id && x.NormalizedName == prepared.Value.Normalized))
            return new(null, ClassificationFailure.Duplicate);

        entity.Name = prepared.Value.Name;
        entity.NormalizedName = prepared.Value.Normalized;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return new(null, ClassificationFailure.Duplicate);
        }
        return new(new(entity.Id, entity.Name), ClassificationFailure.None);
    }

    public async Task<ClassificationFailure> Delete<T>(Guid id) where T : class, IClassification
    {
        var entity = await db.Set<T>().SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return ClassificationFailure.NotFound;
        db.Remove(entity);
        await db.SaveChangesAsync();
        return ClassificationFailure.None;
    }
}
