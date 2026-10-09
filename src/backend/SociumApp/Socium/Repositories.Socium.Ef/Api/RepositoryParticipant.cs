using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Socium.Ef.Options;

namespace Repositories.Socium.Ef.Api;

public interface IRepositoryParticipant
{
    Task<Participant> CreateAsync(Participant model, CancellationToken cancellationToken = default);
    Task<Participant> UpdateAsync(Participant model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Participant model, CancellationToken cancellationToken = default);
    Participant GetNew();
    Participant GetNew(ParticipantGetNewOptions options);
    Task<Participant?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetListAsync(ParticipantQueryOptions? options = null, CancellationToken cancellationToken = default);
}

public class RepositoryParticipant : IRepositoryParticipant
{
    private readonly DbContextSocium context;

    public RepositoryParticipant(DbContextSocium context)
    {
        this.context = context;
    }

    public async Task<Participant> CreateAsync(Participant model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    public Task<Participant> UpdateAsync(Participant model, CancellationToken cancellationToken = default)
    {
        context.Update(model);
        return Task.FromResult(model);
    }

    public Task DeleteAsync(Participant model, CancellationToken cancellationToken = default)
    {
        context.Remove(model);
        return Task.CompletedTask;
    }

    public Participant GetNew()
    {
        return new Participant
        {
            Id = Guid.Empty,
            ChatId = Guid.Empty,
            UserId = Guid.Empty,
            IsAdmin = false,
        };
    }

    public Participant GetNew(ParticipantGetNewOptions options)
    {
        var model = GetNew();
        model.ChatId = options.ChatId;
        model.UserId = options.UserId;
        model.IsAdmin = options.IsAdmin;
        return model;
    }

    public async Task<Participant?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Participants.FirstOrDefaultAsync(participant => participant.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetListAsync(ParticipantQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var query = context.Participants
            .AsNoTracking()
            .Include(participant => participant.User)
            .AsQueryable();

        if (options?.ChatId is Guid chatId)
        {
            query = query.Where(participant => participant.ChatId == chatId);
        }

        if (options?.UserId is Guid userId)
        {
            query = query.Where(participant => participant.UserId == userId);
        }

        if (options?.IsAdmin is bool isAdmin)
        {
            query = query.Where(participant => participant.IsAdmin == isAdmin);
        }

        return await query
            .OrderBy(participant => participant.User.Name)
            .ThenBy(participant => participant.User.Login)
            .ThenBy(participant => participant.Id)
            .ToListAsync(cancellationToken);
    }
}
