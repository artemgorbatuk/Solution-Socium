namespace Services.Security.Texts;

public static class AppRoleCrudTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string Creating = "Начало выдачи роли приложения";
            public const string Deleting = "Начало снятия роли приложения";
        }

        public static class Success
        {
            public const string GetRoleNames = "Роли приложения загружены.";
            public const string CreateCompleted = "Роль приложения выдана";
            public const string DeleteCompleted = "Роль приложения снята";
        }

        public static class Error
        {
            public const string GetRoleNames = "Не удалось загрузить роли приложения.";
            public const string CreateError = "Не удалось выдать роль приложения";
            public const string DeleteError = "Не удалось снять роль приложения";
        }

        public static class Canceled
        {
            public const string GetRoleNames = "Загрузка ролей приложения отменена.";
            public const string Create = "Выдача роли приложения отменена";
            public const string Delete = "Снятие роли приложения отменено";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string UserIdCannotBeEmpty = "Идентификатор пользователя не может быть пустым.";
            public const string UnknownRole = "Неизвестная роль приложения.";
            public const string CurrentUserNotFound = "Требуется вход в систему.";
            public const string UserNotFoundById = "Пользователь не найден по указанному идентификатору.";
            public const string AccessDenied = "Недостаточно прав для управления ролями приложения.";
            public const string CannotGrantSelf = "Себе роль выдать нельзя — это делает другой админ приложения.";
            public const string AlreadyAssigned = "У пользователя уже есть эта роль приложения.";
            public const string NotAssigned = "У пользователя нет этой роли приложения.";
            public const string LastAppAdminCannotRevoke = "Нельзя снять роль с последнего админа приложения — сначала назначьте другого.";
        }
    }
}
