using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Socium.Ef.Options;

namespace Repositories.Socium.Ef.Api;

/// <summary>
/// Пользователи удаляются мягко — флагом <see cref="User.IsDeleted"/>, поэтому физического удаления нет.
/// </summary>
public interface IRepositoryUser
{
    Task<User> CreateAsync(User model, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User model, CancellationToken cancellationToken = default);
    User GetNew();
    Task<User?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetListAsync(UserQueryOptions? options = null, CancellationToken cancellationToken = default);
}

public class RepositoryUser : IRepositoryUser
{
    private readonly DbContextSocium context;

    public RepositoryUser(DbContextSocium context)
    {
        this.context = context;
    }

    public async Task<User> CreateAsync(User model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    public Task<User> UpdateAsync(User model, CancellationToken cancellationToken = default)
    {
        context.Update(model);
        return Task.FromResult(model);
    }

    public User GetNew()
    {
        return new User
        {
            Id = Guid.Empty,
            Login = string.Empty,
            Name = string.Empty,
        };
    }

    public async Task<User?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetListAsync(UserQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(options?.Login))
        {
            query = query.Where(user => user.Login == options.Login);
        }

        if (options?.ExcludeId is Guid excludeId)
        {
            query = query.Where(user => user.Id != excludeId);
        }

        if (options?.IsDeleted is bool isDeleted)
        {
            query = query.Where(user => user.IsDeleted == isDeleted);
        }

        return await query
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Login)
            .ToListAsync(cancellationToken);
    }
}
