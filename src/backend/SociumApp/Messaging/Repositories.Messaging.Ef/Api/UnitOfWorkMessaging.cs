using Datasource.Messaging.Ef.Contexts;

namespace Repositories.Messaging.Ef.Api;

public interface IUnitOfWorkMessaging
{
    IRepositoryEmailToken EmailTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWorkMessaging : IUnitOfWorkMessaging
{
    private readonly DbContextMessaging context;

    public UnitOfWorkMessaging(DbContextMessaging context)
    {
        this.context = context;
        EmailTokens = new RepositoryEmailToken(context);
    }

    public IRepositoryEmailToken EmailTokens { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
