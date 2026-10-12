namespace Services.Realtime.Models;

/// <summary>Без ролей: клиент перечитывает данные REST с проверкой прав.</summary>
public class UserAccessChangedNotifyModel
{
    public required Guid UserId { get; set; }
}
