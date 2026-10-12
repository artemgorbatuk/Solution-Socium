using Datasource.Security.Ef.Contexts;
using Datasource.Security.Ef.Models;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Security.Ef.Api;

public interface IRepositoryAppRoleAssignment
{
    Task<AppRoleAssignment> CreateAsync(AppRoleAssignment model, CancellationToken cancellationToken = default);
    Task DeleteAsync(AppRoleAssignment model, CancellationToken cancellationToken = default);
    AppRoleAssignment GetNew();
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AppRoleAssignment?> GetSingleOrDefaultAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
}

public class RepositoryAppRoleAssignment : IRepositoryAppRoleAssignment
{
    private readonly DbContextSecurity context;

    public RepositoryAppRoleAssignment(DbContextSecurity context)
    {
        this.context = context;
    }

    public async Task<AppRoleAssignment> CreateAsync(AppRoleAssignment model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    public Task DeleteAsync(AppRoleAssignment model, CancellationToken cancellationToken = default)
    {
        context.Remove(model);
        return Task.CompletedTask;
    }

    public AppRoleAssignment GetNew()
    {
        return new AppRoleAssignment
        {
            Id = Guid.Empty,
            UserId = Guid.Empty,
            RoleName = string.Empty
        };
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.AppRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId)
            .Select(assignment => assignment.RoleName)
            .ToListAsync(cancellationToken);
    }

    public async Task<AppRoleAssignment?> GetSingleOrDefaultAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        return await context.AppRoleAssignments
            .FirstOrDefaultAsync(
                assignment => assignment.UserId == userId && assignment.RoleName == roleName,
                cancellationToken);
    }
}
