using Datasource.Security.Ef.Contexts;

namespace Repositories.Security.Ef.Api;

public interface IUnitOfWorkSecurity
{
    IRepositoryUser Users { get; }
    IRepositoryRefreshToken RefreshTokens { get; }
    IRepositoryAppRoleAssignment AppRoleAssignments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWorkSecurity : IUnitOfWorkSecurity
{
    private readonly DbContextSecurity context;

    public UnitOfWorkSecurity(DbContextSecurity context)
    {
        this.context = context;
        Users = new RepositoryUser(context);
        RefreshTokens = new RepositoryRefreshToken(context);
        AppRoleAssignments = new RepositoryAppRoleAssignment(context);
    }

    public IRepositoryUser Users { get; }
    public IRepositoryRefreshToken RefreshTokens { get; }
    public IRepositoryAppRoleAssignment AppRoleAssignments { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
