namespace Services.Socium.Texts;

public static class ChatCrudTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string DisplayCreating = "Начало отображения страницы создания чата";
            public const string DisplayUpdating = "Начало отображения страницы обновления чата";
            public const string DisplayDeleting = "Начало отображения страницы удаления чата";
            public const string Creating = "Начало выполнения создания чата";
            public const string Updating = "Начало выполнения обновления чата";
            public const string Deleting = "Начало выполнения удаления чата";
            public const string DisplayInfoLoading = "Начало отображения страницы просмотра чата";
            public const string DisplayListLoading = "Начало отображения страницы списка чатов";
        }

        public static class Success
        {
            public const string DisplayCreateCompleted = "Страница создания чата успешно отображена";
            public const string DisplayUpdateCompleted = "Страница обновления чата успешно отображена";
            public const string DisplayDeleteCompleted = "Страница удаления чата успешно отображена";
            public const string CreateCompleted = "Чат успешно создан";
            public const string UpdateCompleted = "Чат успешно обновлён";
            public const string DeleteCompleted = "Чат успешно удалён";
            public const string DisplayInfoCompleted = "Страница просмотра чата успешно отображена";
            public const string DisplayListCompleted = "Страница списка чатов успешно отображена";
        }

        public static class Error
        {
            public const string DisplayCreateError = "Не удалось отобразить страницу создания чата";
            public const string DisplayUpdateError = "Не удалось отобразить страницу обновления чата";
            public const string DisplayDeleteError = "Не удалось отобразить страницу удаления чата";
            public const string CreateError = "Не удалось создать чат";
            public const string UpdateError = "Не удалось обновить чат";
            public const string DeleteError = "Не удалось удалить чат";
            public const string DisplayInfoError = "Не удалось отобразить страницу просмотра чата";
            public const string DisplayListError = "Не удалось отобразить страницу списка чатов";
        }

        public static class Canceled
        {
            public const string DisplayCreate = "Отображение страницы создания чата отменено";
            public const string DisplayUpdate = "Отображение страницы обновления чата отменено";
            public const string DisplayDelete = "Отображение страницы удаления чата отменено";
            public const string Create = "Создание чата отменено";
            public const string Update = "Обновление чата отменено";
            public const string Delete = "Удаление чата отменено";
            public const string DisplayInfo = "Отображение страницы просмотра чата отменено";
            public const string DisplayList = "Отображение страницы списка чатов отменено";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string IdCannotBeEmpty = "Идентификатор чата не может быть пустым.";
            public const string RoomIdCannotBeEmpty = "Идентификатор комнаты не может быть пустым.";
            public const string ChatNotFoundById = "Чат не найден по указанному идентификатору.";
            public const string RoomNotFoundById = "Комната не найдена по указанному идентификатору.";
            public const string NameNotEmpty = "Название чата не может быть пустым.";
            public static string NameMaximumLength(int length)
                => $"Название чата не может превышать {length} символов.";
        }
    }
}
