using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore.Storage;

namespace Repositories.Socium.Ef.Api;

public interface IUnitOfWorkSocium
{
    IRepositoryRoom Rooms { get; }
    IRepositoryChat Chats { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
}

public class UnitOfWorkSocium : IUnitOfWorkSocium
{
    private readonly DbContextSocium context;

    public UnitOfWorkSocium(DbContextSocium context)
    {
        this.context = context;
        Rooms = new RepositoryRoom(context);
        Chats = new RepositoryChat(context);
    }

    public IRepositoryRoom Rooms { get; }
    public IRepositoryChat Chats { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        await transaction.CommitAsync(cancellationToken);
        await transaction.DisposeAsync();
    }

    public async Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        await transaction.RollbackAsync(cancellationToken);
        await transaction.DisposeAsync();
    }
}
