using Datasource.Files.Ef.Contexts;

namespace Repositories.Files.Ef.Api;

public interface IUnitOfWorkFiles
{
    IRepositoryStoredFile StoredFiles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWorkFiles : IUnitOfWorkFiles
{
    private readonly DbContextFiles context;

    public UnitOfWorkFiles(DbContextFiles context)
    {
        this.context = context;
        StoredFiles = new RepositoryStoredFile(context);
    }

    public IRepositoryStoredFile StoredFiles { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
