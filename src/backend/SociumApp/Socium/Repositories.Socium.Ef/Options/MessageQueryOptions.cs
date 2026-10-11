namespace Repositories.Socium.Ef.Options;

public class MessageQueryOptions
{
    public Guid? ChatId { get; set; }

    /// <summary>
    /// Вернуть сообщения старше указанного (для подгрузки истории вверх).
    /// </summary>
    public Guid? BeforeId { get; set; }

    /// <summary>
    /// Сколько записей взять (после сортировки от новых к старым). По умолчанию 50.
    /// </summary>
    public int Take { get; set; } = 50;
}
