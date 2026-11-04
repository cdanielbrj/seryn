using System.Text;
using Microsoft.EntityFrameworkCore;
using Seryn.Server.Persistence;

namespace Seryn.Server.Classifications;

public enum ClassificationFailure
{
    None,
    InvalidName,
    Duplicate,
    NotFound,
    InvalidParent,
    HasChildren
}

public sealed record ClassificationItem(Guid Id, string Nome);
public sealed record CategoryItem(Guid Id, string Nome, Guid? CategoriaPaiId);
public sealed record ClassificationResult<T>(T? Item, ClassificationFailure Failure) where T : class;

public sealed class ClassificationService(SerynDbContext db)
{
    public async Task<List<ClassificationItem>> ListTypes()
        => await db.FinancialTypes.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new ClassificationItem(x.Id, x.Name)).ToListAsync();

    public async Task<List<ClassificationItem>> ListPaymentMethods()
        => await db.PaymentMethods.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new ClassificationItem(x.Id, x.Name)).ToListAsync();

    public async Task<List<CategoryItem>> ListCategories()
        => await db.FinancialCategories.AsNoTracking().OrderBy(x => x.ParentId != null).ThenBy(x => x.Name)
            .Select(x => new CategoryItem(x.Id, x.Name, x.ParentId)).ToListAsync();

    public async Task<ClassificationResult<ClassificationItem>> CreateType(string? input)
        => await Create<FinancialType>(input);

    public async Task<ClassificationResult<ClassificationItem>> CreatePaymentMethod(string? input)
        => await Create<PaymentMethod>(input);

    private async Task<ClassificationResult<ClassificationItem>> Create<T>(string? input) where T : class, IClassification, new()
    {
        var prepared = Prepare(input);
        if (prepared is null)
            return new(null, ClassificationFailure.InvalidName);
        if (await db.Set<T>().AnyAsync(x => x.NormalizedName == prepared.Value.Normalized))
            return new(null, ClassificationFailure.Duplicate);
        var entity = new T { Id = Guid.NewGuid(), Name = prepared.Value.Name, NormalizedName = prepared.Value.Normalized };
        db.Add(entity);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Detached;
            return new(null, ClassificationFailure.Duplicate);
        }
        return new(new(entity.Id, entity.Name), ClassificationFailure.None);
    }

    public async Task<ClassificationResult<CategoryItem>> CreateCategory(string? input, Guid? parentId)
    {
        var prepared = Prepare(input);
        if (prepared is null)
            return new(null, ClassificationFailure.InvalidName);
        if (!await IsValidRoot(parentId))
            return new(null, ClassificationFailure.InvalidParent);
        var scope = Scope(parentId);
        if (await db.FinancialCategories.AnyAsync(x => x.ScopeKey == scope && x.NormalizedName == prepared.Value.Normalized))
            return new(null, ClassificationFailure.Duplicate);

        var entity = new FinancialCategory
        {
            Id = Guid.NewGuid(), Name = prepared.Value.Name, NormalizedName = prepared.Value.Normalized,
            ParentId = parentId, ScopeKey = scope
        };
        db.Add(entity);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Detached;
            return new(null, ClassificationFailure.Duplicate);
        }
        return new(new(entity.Id, entity.Name, entity.ParentId), ClassificationFailure.None);
    }

    public async Task<ClassificationResult<ClassificationItem>> UpdateType(Guid id, string? input)
        => await Update<FinancialType>(id, input);

    public async Task<ClassificationResult<ClassificationItem>> UpdatePaymentMethod(Guid id, string? input)
        => await Update<PaymentMethod>(id, input);

    private async Task<ClassificationResult<ClassificationItem>> Update<T>(Guid id, string? input) where T : class, IClassification
    {
        var prepared = Prepare(input);
        if (prepared is null)
            return new(null, ClassificationFailure.InvalidName);
        var entity = await db.Set<T>().SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return new(null, ClassificationFailure.NotFound);
        if (await db.Set<T>().AnyAsync(x => x.Id != id && x.NormalizedName == prepared.Value.Normalized))
            return new(null, ClassificationFailure.Duplicate);
        entity.Name = prepared.Value.Name;
        entity.NormalizedName = prepared.Value.Normalized;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return new(null, ClassificationFailure.Duplicate); }
        return new(new(entity.Id, entity.Name), ClassificationFailure.None);
    }

    public async Task<ClassificationResult<CategoryItem>> UpdateCategory(Guid id, string? input, Guid? parentId)
    {
        var prepared = Prepare(input);
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
        if (await db.FinancialCategories.AnyAsync(x => x.Id != id && x.ScopeKey == scope && x.NormalizedName == prepared.Value.Normalized))
            return new(null, ClassificationFailure.Duplicate);
        entity.Name = prepared.Value.Name;
        entity.NormalizedName = prepared.Value.Normalized;
        entity.ParentId = parentId;
        entity.ScopeKey = scope;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return new(null, ClassificationFailure.Duplicate); }
        return new(new(entity.Id, entity.Name, entity.ParentId), ClassificationFailure.None);
    }

    public Task<ClassificationFailure> DeleteType(Guid id) => Delete<FinancialType>(id);
    public Task<ClassificationFailure> DeletePaymentMethod(Guid id) => Delete<PaymentMethod>(id);

    public async Task<ClassificationFailure> DeleteCategory(Guid id)
    {
        var entity = await db.FinancialCategories.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return ClassificationFailure.NotFound;
        if (await db.FinancialCategories.AnyAsync(x => x.ParentId == id))
            return ClassificationFailure.HasChildren;
        db.Remove(entity);
        await db.SaveChangesAsync();
        return ClassificationFailure.None;
    }

    private async Task<ClassificationFailure> Delete<T>(Guid id) where T : class, IClassification
    {
        var entity = await db.Set<T>().SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return ClassificationFailure.NotFound;
        db.Remove(entity);
        await db.SaveChangesAsync();
        return ClassificationFailure.None;
    }

    private async Task<bool> IsValidRoot(Guid? parentId)
    {
        if (parentId is null) return true;
        return await db.FinancialCategories.AnyAsync(x => x.Id == parentId && x.ParentId == null);
    }

    private static string Scope(Guid? parentId) => parentId?.ToString("N") ?? FinancialCategory.RootScope;

    private static (string Name, string Normalized)? Prepare(string? input)
    {
        var name = input?.Trim().Normalize(NormalizationForm.FormC) ?? "";
        return name.Length is < 1 or > 80 ? null : (name, name.ToUpperInvariant());
    }
}
