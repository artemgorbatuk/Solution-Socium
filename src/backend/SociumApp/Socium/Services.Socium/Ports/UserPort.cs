namespace Services.Socium.Ports;

/// <summary>
/// Пользователи живут в области Security; Socium хранит только их <c>Id</c>.
/// Адаптер — в <c>WebApi/Ports/</c>. Несуществующий пользователь не возвращается;
/// удалённый — только из <see cref="GetListAsync"/> (с <see cref="UserPortModel.IsDeleted"/>), чтобы подписать его старые сообщения.
/// </summary>
public interface IUserPort
{
    Task<UserPortModel?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, UserPortModel>> GetListAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
