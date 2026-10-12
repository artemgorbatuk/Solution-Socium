namespace Services.Socium.Texts;

public static class ParticipantCrudTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string Creating = "Начало добавления участника в чат";
            public const string CreateStaging = "Начало подготовки участия в чате (stage)";
            public const string Updating = "Начало смены роли участника чата";
            public const string DisplayDeleting = "Начало отображения страницы выхода из чата";
            public const string Deleting = "Начало выхода из чата";
            public const string Kicking = "Начало исключения участника из чата";
            public const string DisplayListLoading = "Начало отображения страницы списка участников чата";
        }

        public static class Success
        {
            public const string CreateCompleted = "Участник добавлен в чат";
            public const string CreateStageCompleted = "Участие в чате подготовлено";
            public const string UpdateCompleted = "Роль участника чата изменена";
            public const string DisplayDeleteCompleted = "Страница выхода из чата успешно отображена";
            public const string DeleteCompleted = "Вы покинули чат";
            public const string KickCompleted = "Участник исключён из чата";
            public const string DisplayListCompleted = "Страница списка участников чата успешно отображена";
        }

        public static class Error
        {
            public const string CreateError = "Не удалось добавить участника в чат";
            public const string CreateStageError = "Не удалось подготовить участие в чате";
            public const string UpdateError = "Не удалось изменить роль участника чата";
            public const string DisplayDeleteError = "Не удалось отобразить страницу выхода из чата";
            public const string DeleteError = "Не удалось покинуть чат";
            public const string KickError = "Не удалось исключить участника из чата";
            public const string DisplayListError = "Не удалось отобразить страницу списка участников чата";
        }

        public static class Canceled
        {
            public const string Create = "Добавление участника в чат отменено";
            public const string CreateStage = "Подготовка участия в чате (stage) отменена";
            public const string Update = "Смена роли участника чата отменена";
            public const string DisplayDelete = "Отображение страницы выхода из чата отменено";
            public const string Delete = "Выход из чата отменён";
            public const string Kick = "Исключение участника из чата отменено";
            public const string DisplayList = "Отображение страницы списка участников чата отменено";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string IdCannotBeEmpty = "Идентификатор участника не может быть пустым.";
            public const string ChatIdCannotBeEmpty = "Идентификатор чата не может быть пустым.";
            public const string UserIdCannotBeEmpty = "Идентификатор пользователя не может быть пустым.";
            public const string DeleteTargetRequired = "Укажите chatId для выхода или id участника для исключения.";
            public const string DeleteTargetAmbiguous = "Укажите либо chatId (выход), либо id участника (исключение), но не оба.";
            public const string CurrentUserNotFound = "Текущий пользователь не выбран или не найден.";
            public const string ChatNotFoundById = "Чат не найден по указанному идентификатору.";
            public const string ParticipantNotFoundById = "Участник не найден по указанному идентификатору.";
            public const string NotParticipant = "Вы не участник этого чата.";
            public const string NotAdmin = "Менять роли может только админ чата.";
            public const string AccessDenied = "Недостаточно прав для выполнения действия над участниками.";
            public const string AlreadyParticipant = "Пользователь уже участник этого чата.";
            public const string UserNotFoundById = "Пользователь не найден по указанному идентификатору.";
            public const string CannotKickSelf = "Себя исключить нельзя — воспользуйтесь «Покинуть чат».";
            public const string LastAdminCannotRevoke = "Нельзя снять роль с последнего админа чата — сначала назначьте админом другого участника.";
            public const string LastAdminCannotLeave = "Вы последний админ чата — сначала назначьте админом другого участника.";
            public const string LastAdminCannotKick = "Нельзя исключить последнего админа чата — сначала назначьте админом другого участника.";
            public const string SoleParticipantCannotLeave = "Вы единственный участник чата — покинуть его нельзя, можно только удалить чат.";
        }
    }
}
