namespace Services.Socium.Ports;

/// <summary>
/// Роли уровня приложения живут в Security. Адаптер — в WebApi/Ports/.
/// </summary>
public interface IUserRolePort
{
    Task<IReadOnlyList<string>> GetAppRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);
}
