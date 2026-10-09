namespace Services.Socium.Texts;

public static class MessageCrudTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string DisplayCreating = "Начало отображения страницы создания сообщения";
            public const string DisplayUpdating = "Начало отображения страницы обновления сообщения";
            public const string DisplayDeleting = "Начало отображения страницы удаления сообщения";
            public const string Creating = "Начало выполнения создания сообщения";
            public const string Updating = "Начало выполнения обновления сообщения";
            public const string Deleting = "Начало выполнения удаления сообщения";
            public const string DisplayInfoLoading = "Начало отображения страницы просмотра сообщения";
            public const string DisplayListLoading = "Начало отображения страницы списка сообщений";
        }

        public static class Success
        {
            public const string DisplayCreateCompleted = "Страница создания сообщения успешно отображена";
            public const string DisplayUpdateCompleted = "Страница обновления сообщения успешно отображена";
            public const string DisplayDeleteCompleted = "Страница удаления сообщения успешно отображена";
            public const string CreateCompleted = "Сообщение успешно отправлено";
            public const string UpdateCompleted = "Сообщение успешно обновлено";
            public const string DeleteCompleted = "Сообщение успешно удалено";
            public const string DisplayInfoCompleted = "Страница просмотра сообщения успешно отображена";
            public const string DisplayListCompleted = "Страница списка сообщений успешно отображена";
        }

        public static class Error
        {
            public const string DisplayCreateError = "Не удалось отобразить страницу создания сообщения";
            public const string DisplayUpdateError = "Не удалось отобразить страницу обновления сообщения";
            public const string DisplayDeleteError = "Не удалось отобразить страницу удаления сообщения";
            public const string CreateError = "Не удалось отправить сообщение";
            public const string UpdateError = "Не удалось обновить сообщение";
            public const string DeleteError = "Не удалось удалить сообщение";
            public const string DisplayInfoError = "Не удалось отобразить страницу просмотра сообщения";
            public const string DisplayListError = "Не удалось отобразить страницу списка сообщений";
        }

        public static class Canceled
        {
            public const string DisplayCreate = "Отображение страницы создания сообщения отменено";
            public const string DisplayUpdate = "Отображение страницы обновления сообщения отменено";
            public const string DisplayDelete = "Отображение страницы удаления сообщения отменено";
            public const string Create = "Отправка сообщения отменена";
            public const string Update = "Обновление сообщения отменено";
            public const string Delete = "Удаление сообщения отменено";
            public const string DisplayInfo = "Отображение страницы просмотра сообщения отменено";
            public const string DisplayList = "Отображение страницы списка сообщений отменено";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string IdCannotBeEmpty = "Идентификатор сообщения не может быть пустым.";
            public const string ChatIdCannotBeEmpty = "Идентификатор чата не может быть пустым.";
            public const string MessageNotFoundById = "Сообщение не найдено по указанному идентификатору.";
            public const string ChatNotFoundById = "Чат не найден по указанному идентификатору.";
            public const string TextNotEmpty = "Текст сообщения не может быть пустым.";
        }
    }
}
