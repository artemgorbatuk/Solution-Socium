namespace Services.Realtime.Api;

/// <summary>
/// Проверка участия в чате для Join на хабе. Реализация — в WebApi (Socium).
/// </summary>
public interface IChatMembership
{
    Task<bool> IsParticipantAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
}
