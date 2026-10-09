namespace Services.Shared.Enums;

public static class MessageType
{
    public const int NONE = 0;
    public const int LOADED = 1;
    public const int SAVED = 2;
    public const int ERROR = 3;
    public const int INVALID = 4;
    public const int BAD_REQUEST = 5;
    public const int NOT_FOUND = 6;
    public const int WARNING = 7;
    public const int UNAUTHORIZED = 8;
    public const int FORBIDDEN = 9;

    public static string GetDescription(int messageTypeId)
    {
        return messageTypeId switch
        {
            NONE => "Нет",
            LOADED => "Загружено",
            SAVED => "Сохранено",
            ERROR => "Ошибка",
            INVALID => "Невалидные данные",
            BAD_REQUEST => "Некорректный запрос",
            NOT_FOUND => "Не найдено",
            WARNING => "Предупреждение",
            UNAUTHORIZED => "Пользователь не определён",
            FORBIDDEN => "Доступ запрещён",
            _ => "Неизвестный тип"
        };
    }

    public static string GetAlertClass(int messageTypeId)
    {        
        return messageTypeId switch
        {
            NONE => "alert-secondary",
            LOADED => "alert-info",
            SAVED => "alert-success",
            ERROR => "alert-danger",
            INVALID => "alert-warning",
            BAD_REQUEST => "alert-danger",
            NOT_FOUND => "alert-danger",
            WARNING => "alert-warning",
            UNAUTHORIZED => "alert-danger",
            FORBIDDEN => "alert-danger",
            _ => "alert-secondary"
        };
    }

    public static string GetTextColorClass(int messageTypeId)
    {
        return messageTypeId switch
        {
            NONE => "text-secondary",
            LOADED => "text-info",
            SAVED => "text-success",
            ERROR => "text-danger",
            INVALID => "text-warning",
            BAD_REQUEST => "text-danger",
            NOT_FOUND => "text-danger",
            WARNING => "text-warning",
            UNAUTHORIZED => "text-danger",
            FORBIDDEN => "text-danger",
            _ => "text-secondary"
        };
    }

    public static string GetBorderClass(int messageTypeId)
    {
        return messageTypeId switch
        {
            NONE => "border-secondary",
            LOADED => "border-info",
            SAVED => "border-success",
            ERROR => "border-danger",
            INVALID => "border-warning",
            BAD_REQUEST => "border-danger",
            NOT_FOUND => "border-danger",
            WARNING => "border-warning",
            UNAUTHORIZED => "border-danger",
            FORBIDDEN => "border-danger",
            _ => "border-secondary"
        };
    }

    public static string GetBackgroundClass(int messageTypeId)
    {
        return messageTypeId switch
        {
            NONE => "bg-secondary",
            LOADED => "bg-info",
            SAVED => "bg-success",
            ERROR => "bg-danger",
            INVALID => "bg-warning",
            BAD_REQUEST => "bg-danger",
            NOT_FOUND => "bg-danger",
            WARNING => "bg-warning",
            UNAUTHORIZED => "bg-danger",
            FORBIDDEN => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public static string GetIconClass(int messageTypeId)
    {
        return messageTypeId switch
        {
            LOADED => "fa-info-circle",
            SAVED => "fa-check-circle",
            ERROR => "fa-exclamation-triangle",
            INVALID => "fa-exclamation-circle",
            BAD_REQUEST => "fa-exclamation-triangle",
            NOT_FOUND => "fa-times-circle",
            WARNING => "fa-exclamation-circle",
            UNAUTHORIZED => "fa-user-times",
            FORBIDDEN => "fa-ban",
            _ => "fa-info-circle"
        };
    }

    /// <summary>
    /// Возвращает ключ для TempData в зависимости от типа сообщения.
    /// </summary>
    public static string GetTempDataKey(int messageTypeId)
    {
        return messageTypeId switch
        {
            ERROR => "ErrorMessage",
            INVALID => "InvalidMessage",
            BAD_REQUEST => "ErrorMessage",
            NOT_FOUND => "ErrorMessage",
            UNAUTHORIZED => "ErrorMessage",
            FORBIDDEN => "ErrorMessage",
            SAVED => "SuccessMessage",
            LOADED => "SuccessMessage",
            WARNING => "WarningMessage",
            _ => "SuccessMessage"
        };
    }
}
