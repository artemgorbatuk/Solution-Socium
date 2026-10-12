using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Socium.Ef.Api;

public interface IRepositoryChatRoleAssignment
{
    Task<ChatRoleAssignment> CreateAsync(ChatRoleAssignment model, CancellationToken cancellationToken = default);
    Task DeleteAsync(ChatRoleAssignment model, CancellationToken cancellationToken = default);
    ChatRoleAssignment GetNew();
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, Guid chatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetRoomIdsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetChatIdsInRoomAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRoleNamesInRoomAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default);
    Task<int> CountByChatAndRoleAsync(Guid chatId, string roleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetUserIdsByChatAndRoleAsync(Guid chatId, string roleName, CancellationToken cancellationToken = default);
    Task<ChatRoleAssignment?> GetSingleOrDefaultAsync(Guid userId, Guid chatId, string roleName, CancellationToken cancellationToken = default);
}

public class RepositoryChatRoleAssignment : IRepositoryChatRoleAssignment
{
    private readonly DbContextSocium context;

    public RepositoryChatRoleAssignment(DbContextSocium context)
    {
        this.context = context;
    }

    public async Task<ChatRoleAssignment> CreateAsync(ChatRoleAssignment model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    public Task DeleteAsync(ChatRoleAssignment model, CancellationToken cancellationToken = default)
    {
        context.Remove(model);
        return Task.CompletedTask;
    }

    public ChatRoleAssignment GetNew()
    {
        return new ChatRoleAssignment
        {
            Id = Guid.Empty,
            UserId = Guid.Empty,
            ChatId = Guid.Empty,
            RoleName = string.Empty
        };
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, Guid chatId, CancellationToken cancellationToken = default)
    {
        return await context.ChatRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId && assignment.ChatId == chatId)
            .Select(assignment => assignment.RoleName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetRoomIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.ChatRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId)
            .Select(assignment => assignment.Chat.RoomId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetChatIdsInRoomAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default)
    {
        return await context.ChatRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId && assignment.Chat.RoomId == roomId)
            .Select(assignment => assignment.ChatId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesInRoomAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default)
    {
        return await context.ChatRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId && assignment.Chat.RoomId == roomId)
            .Select(assignment => assignment.RoleName)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByChatAndRoleAsync(Guid chatId, string roleName, CancellationToken cancellationToken = default)
    {
        return await context.ChatRoleAssignments
            .AsNoTracking()
            .CountAsync(assignment => assignment.ChatId == chatId && assignment.RoleName == roleName, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsByChatAndRoleAsync(Guid chatId, string roleName, CancellationToken cancellationToken = default)
    {
        return await context.ChatRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.ChatId == chatId && assignment.RoleName == roleName)
            .Select(assignment => assignment.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatRoleAssignment?> GetSingleOrDefaultAsync(Guid userId, Guid chatId, string roleName, CancellationToken cancellationToken = default)
    {
        return await context.ChatRoleAssignments
            .FirstOrDefaultAsync(
                assignment => assignment.UserId == userId
                    && assignment.ChatId == chatId
                    && assignment.RoleName == roleName,
                cancellationToken);
    }
}
