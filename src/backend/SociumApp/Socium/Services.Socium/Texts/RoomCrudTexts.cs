namespace Services.Socium.Texts;

public static class RoomCrudTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string DisplayCreating = "Начало отображения страницы создания комнаты";
            public const string DisplayUpdating = "Начало отображения страницы обновления комнаты";
            public const string DisplayDeleting = "Начало отображения страницы удаления комнаты";
            public const string Creating = "Начало выполнения создания комнаты";
            public const string Updating = "Начало выполнения обновления комнаты";
            public const string Deleting = "Начало выполнения удаления комнаты";
            public const string DisplayInfoLoading = "Начало отображения страницы просмотра комнаты";
            public const string DisplayListLoading = "Начало отображения страницы списка комнат";
        }

        public static class Success
        {
            public const string DisplayCreateCompleted = "Страница создания комнаты успешно отображена";
            public const string DisplayUpdateCompleted = "Страница обновления комнаты успешно отображена";
            public const string DisplayDeleteCompleted = "Страница удаления комнаты успешно отображена";
            public const string CreateCompleted = "Комната успешно создана";
            public const string UpdateCompleted = "Комната успешно обновлена";
            public const string DeleteCompleted = "Комната успешно удалена";
            public const string DisplayInfoCompleted = "Страница просмотра комнаты успешно отображена";
            public const string DisplayListCompleted = "Страница списка комнат успешно отображена";
        }

        public static class Error
        {
            public const string DisplayCreateError = "Не удалось отобразить страницу создания комнаты";
            public const string DisplayUpdateError = "Не удалось отобразить страницу обновления комнаты";
            public const string DisplayDeleteError = "Не удалось отобразить страницу удаления комнаты";
            public const string CreateError = "Не удалось создать комнату";
            public const string UpdateError = "Не удалось обновить комнату";
            public const string DeleteError = "Не удалось удалить комнату";
            public const string DisplayInfoError = "Не удалось отобразить страницу просмотра комнаты";
            public const string DisplayListError = "Не удалось отобразить страницу списка комнат";
        }

        public static class Canceled
        {
            public const string DisplayCreate = "Отображение страницы создания комнаты отменено";
            public const string DisplayUpdate = "Отображение страницы обновления комнаты отменено";
            public const string DisplayDelete = "Отображение страницы удаления комнаты отменено";
            public const string Create = "Создание комнаты отменено";
            public const string Update = "Обновление комнаты отменено";
            public const string Delete = "Удаление комнаты отменено";
            public const string DisplayInfo = "Отображение страницы просмотра комнаты отменено";
            public const string DisplayList = "Отображение страницы списка комнат отменено";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string IdCannotBeEmpty = "Идентификатор комнаты не может быть пустым.";
            public const string RoomNotFoundById = "Комната не найдена по указанному идентификатору.";
            public const string NameNotEmpty = "Название комнаты не может быть пустым.";
            public const string NameAlreadyExists = "Комната с таким названием уже существует.";
            public static string NameMaximumLength(int length)
                => $"Название комнаты не может превышать {length} символов.";
        }
    }
}
