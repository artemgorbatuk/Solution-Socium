namespace Services.Socium.Api;

/// <summary>
/// Пользователь, от имени которого выполняется запрос. Реализация только сообщает <c>Id</c>;
/// существует ли пользователь и не удалён ли он, проверяет сервис.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}
