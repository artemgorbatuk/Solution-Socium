using Datasource.Files.Ef.Models;

namespace Services.Files.Stored.Ports;

/// <summary>
/// Проверка доступа читателя к файлу по владельцу (сообщение чата и т.п.).
/// Адаптер — в WebApi.
/// </summary>
public interface IFileOwnerAccess
{
    Task<bool> CanReadAsync(FileOwnerType ownerType, Guid ownerId, Guid userId, CancellationToken cancellationToken = default);
}
