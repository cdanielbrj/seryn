using Microsoft.EntityFrameworkCore;
using Seryn.Server.Modules.Classifications.Application;
using Seryn.Server.Modules.Classifications.Contracts;
using Seryn.Server.Persistence;

namespace Seryn.Server.Modules.Classifications.Categories;

internal sealed class CategoriesHandler(SerynDbContext db)
{
    public async Task<List<CategoryItem>> List() =>
        await db.FinancialCategories.AsNoTracking()
            .OrderBy(x => x.ParentId != null)
            .ThenBy(x => x.Name)
            .Select(x => new CategoryItem(x.Id, x.Name, x.ParentId))
            .ToListAsync();

    public async Task<ClassificationResult<CategoryItem>> Create(string? input, Guid? parentId)
    {
        var prepared = ClassificationName.Prepare(input);
        if (prepared is null)
            return new(null, ClassificationFailure.InvalidName);
        if (!await IsValidRoot(parentId))
            return new(null, ClassificationFailure.InvalidParent);

        var scope = Scope(parentId);
        if (await IsDuplicate(scope, prepared.Value.Normalized))
            return new(null, ClassificationFailure.Duplicate);

        var entity = new FinancialCategory
        {
            Id = Guid.NewGuid(),
            Name = prepared.Value.Name,
            NormalizedName = prepared.Value.Normalized,
            ParentId = parentId,
            ScopeKey = scope
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
        return new(new(entity.Id, entity.Name, entity.ParentId), ClassificationFailure.None);
    }

    public async Task<ClassificationResult<CategoryItem>> Update(Guid id, string? input, Guid? parentId)
    {
        var prepared = ClassificationName.Prepare(input);
        if (prepared is null)
            return new(null, ClassificationFailure.InvalidName);

        var entity = await db.FinancialCategories.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return new(null, ClassificationFailure.NotFound);
        if (parentId == id || !await IsValidRoot(parentId))
            return new(null, ClassificationFailure.InvalidParent);
        if (parentId is not null && await db.FinancialCategories.AnyAsync(x => x.ParentId == id))
            return new(null, ClassificationFailure.InvalidParent);

        var scope = Scope(parentId);
        if (await IsDuplicate(scope, prepared.Value.Normalized, id))
            return new(null, ClassificationFailure.Duplicate);

        entity.Name = prepared.Value.Name;
        entity.NormalizedName = prepared.Value.Normalized;
        entity.ParentId = parentId;
        entity.ScopeKey = scope;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return new(null, ClassificationFailure.Duplicate);
        }
        return new(new(entity.Id, entity.Name, entity.ParentId), ClassificationFailure.None);
    }

    public async Task<ClassificationFailure> Delete(Guid id)
    {
        var entity = await db.FinancialCategories.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return ClassificationFailure.NotFound;
        if (await db.FinancialCategories.AnyAsync(x => x.ParentId == id))
            return ClassificationFailure.HasChildren;

        var records = await db.FinancialRecords.Where(x => x.CategoryId == id).ToListAsync();
        foreach (var record in records)
            record.CategoryId = entity.ParentId;
        db.Remove(entity);
        await db.SaveChangesAsync();
        return ClassificationFailure.None;
    }

    private async Task<bool> IsValidRoot(Guid? parentId) =>
        parentId is null || await db.FinancialCategories.AnyAsync(x => x.Id == parentId && x.ParentId == null);

    private async Task<bool> IsDuplicate(string scope, string normalizedName, Guid? excludedId = null) =>
        await db.FinancialCategories.AnyAsync(x =>
            x.Id != excludedId && x.ScopeKey == scope && x.NormalizedName == normalizedName);

    private static string Scope(Guid? parentId) => parentId?.ToString("N") ?? FinancialCategory.RootScope;
}
