namespace Services.Socium.Texts;

public static class UserCrudTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string DisplayCreating = "Начало отображения страницы создания пользователя";
            public const string DisplayUpdating = "Начало отображения страницы обновления пользователя";
            public const string DisplayDeleting = "Начало отображения страницы удаления пользователя";
            public const string Creating = "Начало выполнения создания пользователя";
            public const string Updating = "Начало выполнения обновления пользователя";
            public const string Deleting = "Начало выполнения удаления пользователя";
            public const string DisplayInfoLoading = "Начало отображения страницы просмотра пользователя";
            public const string DisplayListLoading = "Начало отображения страницы списка пользователей";
        }

        public static class Success
        {
            public const string DisplayCreateCompleted = "Страница создания пользователя успешно отображена";
            public const string DisplayUpdateCompleted = "Страница обновления пользователя успешно отображена";
            public const string DisplayDeleteCompleted = "Страница удаления пользователя успешно отображена";
            public const string CreateCompleted = "Пользователь успешно создан";
            public const string UpdateCompleted = "Пользователь успешно обновлён";
            public const string DeleteCompleted = "Пользователь успешно удалён";
            public const string DisplayInfoCompleted = "Страница просмотра пользователя успешно отображена";
            public const string DisplayListCompleted = "Страница списка пользователей успешно отображена";
        }

        public static class Error
        {
            public const string DisplayCreateError = "Не удалось отобразить страницу создания пользователя";
            public const string DisplayUpdateError = "Не удалось отобразить страницу обновления пользователя";
            public const string DisplayDeleteError = "Не удалось отобразить страницу удаления пользователя";
            public const string CreateError = "Не удалось создать пользователя";
            public const string UpdateError = "Не удалось обновить пользователя";
            public const string DeleteError = "Не удалось удалить пользователя";
            public const string DisplayInfoError = "Не удалось отобразить страницу просмотра пользователя";
            public const string DisplayListError = "Не удалось отобразить страницу списка пользователей";
        }

        public static class Canceled
        {
            public const string DisplayCreate = "Отображение страницы создания пользователя отменено";
            public const string DisplayUpdate = "Отображение страницы обновления пользователя отменено";
            public const string DisplayDelete = "Отображение страницы удаления пользователя отменено";
            public const string Create = "Создание пользователя отменено";
            public const string Update = "Обновление пользователя отменено";
            public const string Delete = "Удаление пользователя отменено";
            public const string DisplayInfo = "Отображение страницы просмотра пользователя отменено";
            public const string DisplayList = "Отображение страницы списка пользователей отменено";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string IdCannotBeEmpty = "Идентификатор пользователя не может быть пустым.";
            public const string UserNotFoundById = "Пользователь не найден по указанному идентификатору.";
            public const string LoginNotEmpty = "Логин не может быть пустым.";
            public const string LoginFormat = "Логин может содержать только латинские буквы, цифры и символы «.», «_», «-».";
            public const string LoginAlreadyExists = "Пользователь с таким логином уже существует.";
            public const string NameNotEmpty = "Имя пользователя не может быть пустым.";
            public static string LoginLength(int minimumLength, int maximumLength)
                => $"Логин должен содержать от {minimumLength} до {maximumLength} символов.";
            public static string NameMaximumLength(int length)
                => $"Имя пользователя не может превышать {length} символов.";
        }
    }
}
