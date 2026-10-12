using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Socium.Ef.Options;

namespace Repositories.Socium.Ef.Api;

public interface IRepositoryRoomRoleAssignment
{
    Task<RoomRoleAssignment> CreateAsync(RoomRoleAssignment model, CancellationToken cancellationToken = default);
    Task DeleteAsync(RoomRoleAssignment model, CancellationToken cancellationToken = default);
    RoomRoleAssignment GetNew();
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetRoomIdsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountByRoomAndRoleAsync(Guid roomId, string roleName, CancellationToken cancellationToken = default);
    Task<RoomRoleAssignment?> GetSingleOrDefaultAsync(Guid userId, Guid roomId, string roleName, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoomRoleAssignment>> GetListAsync(RoomRoleAssignmentQueryOptions? options = null, CancellationToken cancellationToken = default);
}

public class RepositoryRoomRoleAssignment : IRepositoryRoomRoleAssignment
{
    private readonly DbContextSocium context;

    public RepositoryRoomRoleAssignment(DbContextSocium context)
    {
        this.context = context;
    }

    public async Task<RoomRoleAssignment> CreateAsync(RoomRoleAssignment model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    public Task DeleteAsync(RoomRoleAssignment model, CancellationToken cancellationToken = default)
    {
        context.Remove(model);
        return Task.CompletedTask;
    }

    public RoomRoleAssignment GetNew()
    {
        return new RoomRoleAssignment
        {
            Id = Guid.Empty,
            UserId = Guid.Empty,
            RoomId = Guid.Empty,
            RoleName = string.Empty
        };
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default)
    {
        return await context.RoomRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId && assignment.RoomId == roomId)
            .Select(assignment => assignment.RoleName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetRoomIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.RoomRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId)
            .Select(assignment => assignment.RoomId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByRoomAndRoleAsync(Guid roomId, string roleName, CancellationToken cancellationToken = default)
    {
        return await context.RoomRoleAssignments
            .AsNoTracking()
            .CountAsync(assignment => assignment.RoomId == roomId && assignment.RoleName == roleName, cancellationToken);
    }

    public async Task<RoomRoleAssignment?> GetSingleOrDefaultAsync(Guid userId, Guid roomId, string roleName, CancellationToken cancellationToken = default)
    {
        return await context.RoomRoleAssignments
            .FirstOrDefaultAsync(
                assignment => assignment.UserId == userId
                    && assignment.RoomId == roomId
                    && assignment.RoleName == roleName,
                cancellationToken);
    }

    public async Task<IEnumerable<RoomRoleAssignment>> GetListAsync(RoomRoleAssignmentQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var query = context.RoomRoleAssignments.AsNoTracking();

        if (options?.UserId is Guid userId)
        {
            query = query.Where(assignment => assignment.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(options?.RoleName))
        {
            query = query.Where(assignment => assignment.RoleName == options.RoleName);
        }

        return await query
            .OrderBy(assignment => assignment.Id)
            .ToListAsync(cancellationToken);
    }
}
