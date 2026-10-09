using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Socium.Ef.Options;

namespace Repositories.Socium.Ef.Api;

public interface IRepositoryRoom
{
    Task<Room> CreateAsync(Room model, CancellationToken cancellationToken = default);
    Task<Room> UpdateAsync(Room model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Room model, CancellationToken cancellationToken = default);
    Room GetNew();
    Task<Room?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Room>> GetListAsync(RoomQueryOptions? options = null, CancellationToken cancellationToken = default);
}

public class RepositoryRoom : IRepositoryRoom
{
    private readonly DbContextSocium context;

    public RepositoryRoom(DbContextSocium context)
    {
        this.context = context;
    }

    public async Task<Room> CreateAsync(Room model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    public Task<Room> UpdateAsync(Room model, CancellationToken cancellationToken = default)
    {
        context.Update(model);
        return Task.FromResult(model);
    }

    public Task DeleteAsync(Room model, CancellationToken cancellationToken = default)
    {
        context.Remove(model);
        return Task.CompletedTask;
    }

    public Room GetNew()
    {
        return new Room
        {
            Id = Guid.Empty,
            Name = string.Empty,
        };
    }

    public async Task<Room?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Rooms.FirstOrDefaultAsync(room => room.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Room>> GetListAsync(RoomQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var query = context.Rooms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(options?.Name))
        {
            query = query.Where(room => room.Name == options.Name);
        }

        if (options?.ExcludeId is Guid excludeId)
        {
            query = query.Where(room => room.Id != excludeId);
        }

        return await query
            .OrderBy(room => room.Name)
            .ToListAsync(cancellationToken);
    }
}
