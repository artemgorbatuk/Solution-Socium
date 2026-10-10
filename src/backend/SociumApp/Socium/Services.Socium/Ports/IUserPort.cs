namespace Services.Socium.Ports;

/// <summary>
/// Пользователи живут в области Security; Socium хранит только их <c>Id</c>.
/// Адаптер — в <c>WebApi/Ports/</c>. Удалённый и несуществующий пользователь не возвращается.
/// </summary>
public interface IUserPort
{
    Task<UserPortModel?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, UserPortModel>> GetListAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
