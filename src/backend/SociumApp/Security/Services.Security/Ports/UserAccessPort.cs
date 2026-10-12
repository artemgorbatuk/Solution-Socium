namespace Services.Security.Ports;

/// <summary>
/// Порт Security к области Socium: роли пользователя в комнатах и чатах.
/// Адаптер в WebApi вызывает <c>IServiceRoleAssignment</c>.
/// </summary>
public interface IUserAccessPort
{
    /// <summary>Комнаты и чаты, где пользователь — единственный админ (готовые подписи для сообщения).</summary>
    Task<IReadOnlyList<string>> GetLastAdminPlacesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(Guid userId, CancellationToken cancellationToken = default);
}
