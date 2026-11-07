namespace Seryn.Server.Modules.Classifications.Contracts;

public sealed record ClassificationRequest(string? Nome);
public sealed record CategoryRequest(string? Nome, Guid? CategoriaPaiId);

public enum ClassificationFailure
{
    None,
    InvalidName,
    Duplicate,
    NotFound,
    InvalidParent,
    HasChildren,
    InUse
}

public sealed record ClassificationItem(Guid Id, string Nome);
public sealed record CategoryItem(Guid Id, string Nome, Guid? CategoriaPaiId);
public sealed record ClassificationResult<T>(T? Item, ClassificationFailure Failure) where T : class;
