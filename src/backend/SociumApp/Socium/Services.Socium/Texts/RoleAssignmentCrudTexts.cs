namespace Services.Socium.Texts;

public static class RoleAssignmentCrudTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string DisplayListLoading = "Начало отображения доступов пользователя";
            public const string Creating = "Начало выдачи роли пользователю";
            public const string Deleting = "Начало снятия роли с пользователя";
            public const string DisplayLastAdminListLoading = "Начало поиска мест, где пользователь — последний админ";
            public const string DeletingByUser = "Начало снятия всех ролей удалённого пользователя";
        }

        public static class Success
        {
            public const string DisplayListCompleted = "Доступы пользователя успешно отображены";
            public const string CreateCompleted = "Роль выдана";
            public const string DeleteCompleted = "Роль снята";
            public const string DisplayLastAdminListCompleted = "Места, где пользователь — последний админ, найдены";
            public const string DeleteByUserCompleted = "Все роли удалённого пользователя сняты";
        }

        public static class Error
        {
            public const string DisplayListError = "Не удалось отобразить доступы пользователя";
            public const string CreateError = "Не удалось выдать роль";
            public const string DeleteError = "Не удалось снять роль";
            public const string DisplayLastAdminListError = "Не удалось найти места, где пользователь — последний админ";
            public const string DeleteByUserError = "Не удалось снять роли удалённого пользователя";
        }

        public static class Canceled
        {
            public const string DisplayList = "Отображение доступов пользователя отменено";
            public const string Create = "Выдача роли отменена";
            public const string Delete = "Снятие роли отменено";
            public const string DisplayLastAdminList = "Поиск мест, где пользователь — последний админ, отменён";
            public const string DeleteByUser = "Снятие ролей удалённого пользователя отменено";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string UserIdCannotBeEmpty = "Идентификатор пользователя не может быть пустым.";
            public const string UnknownRole = "Роль должна быть ролью комнаты или чата.";
            public const string RoomIdRequired = "Для роли комнаты укажите только идентификатор комнаты.";
            public const string ChatIdRequired = "Для роли чата укажите только идентификатор чата.";
            public const string CurrentUserNotFound = "Текущий пользователь не выбран или не найден.";
            public const string UserNotFoundById = "Пользователь не найден по указанному идентификатору.";
            public const string RoomNotFoundById = "Комната не найдена по указанному идентификатору.";
            public const string ChatNotFoundById = "Чат не найден по указанному идентификатору.";
            public const string AccessDenied = "Недостаточно прав для управления доступами пользователей.";
            public const string CannotManageSelf = "Свои доступы менять нельзя — их меняет другой админ приложения.";
            public const string UserDeleted = "Пользователь удалён — его доступы менять нельзя.";
            public const string AlreadyRoomAdmin = "Пользователь уже админ этой комнаты.";
            public const string NotRoomAdmin = "Пользователь не админ этой комнаты.";
            public const string LastRoomAdminCannotRevoke = "Нельзя снять последнего админа комнаты — сначала назначьте другого.";
            public const string NotParticipant = "Пользователь не участник этого чата.";
            public const string AlreadyChatAdmin = "Пользователь уже админ этого чата.";
            public const string NotChatAdmin = "Пользователь не админ этого чата.";
            public const string UserNotDeleted = "Снять все роли можно только у удалённого пользователя.";

            public static string RoomPlace(string roomName) => $"комната «{roomName}»";

            public static string ChatPlace(string roomName, string chatName) => $"чат «{chatName}» в комнате «{roomName}»";
        }
    }
}
