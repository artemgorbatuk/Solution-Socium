namespace Services.Realtime.Api;

/// <summary>
/// Проверка участия в чате для Join / typing на хабе. Реализация — в WebApi (Socium).
/// </summary>
public interface IChatMembership
{
    Task<bool> IsParticipantAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Имя участника для typing; <c>null</c> — не участник или пользователь не найден.</summary>
    Task<string?> GetParticipantNameAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
}
