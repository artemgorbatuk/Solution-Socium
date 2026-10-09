namespace Services.Socium.Texts;

public static class ParticipantCrudTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string Creating = "Начало вступления в чат";
            public const string Updating = "Начало смены роли участника чата";
            public const string DisplayDeleting = "Начало отображения страницы выхода из чата";
            public const string Deleting = "Начало выхода из чата";
            public const string DisplayListLoading = "Начало отображения страницы списка участников чата";
        }

        public static class Success
        {
            public const string CreateCompleted = "Вы вступили в чат";
            public const string UpdateCompleted = "Роль участника чата изменена";
            public const string DisplayDeleteCompleted = "Страница выхода из чата успешно отображена";
            public const string DeleteCompleted = "Вы покинули чат";
            public const string DisplayListCompleted = "Страница списка участников чата успешно отображена";
        }

        public static class Error
        {
            public const string CreateError = "Не удалось вступить в чат";
            public const string UpdateError = "Не удалось изменить роль участника чата";
            public const string DisplayDeleteError = "Не удалось отобразить страницу выхода из чата";
            public const string DeleteError = "Не удалось покинуть чат";
            public const string DisplayListError = "Не удалось отобразить страницу списка участников чата";
        }

        public static class Canceled
        {
            public const string Create = "Вступление в чат отменено";
            public const string Update = "Смена роли участника чата отменена";
            public const string DisplayDelete = "Отображение страницы выхода из чата отменено";
            public const string Delete = "Выход из чата отменён";
            public const string DisplayList = "Отображение страницы списка участников чата отменено";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string IdCannotBeEmpty = "Идентификатор участника не может быть пустым.";
            public const string ChatIdCannotBeEmpty = "Идентификатор чата не может быть пустым.";
            public const string CurrentUserNotFound = "Текущий пользователь не выбран или не найден.";
            public const string ChatNotFoundById = "Чат не найден по указанному идентификатору.";
            public const string ParticipantNotFoundById = "Участник не найден по указанному идентификатору.";
            public const string NotParticipant = "Вы не участник этого чата.";
            public const string NotAdmin = "Менять роли может только админ чата.";
            public const string AlreadyParticipant = "Вы уже участник этого чата.";
            public const string LastAdminCannotRevoke = "Нельзя снять роль с последнего админа чата — сначала назначьте админом другого участника.";
            public const string LastAdminCannotLeave = "Вы последний админ чата — сначала назначьте админом другого участника.";
            public const string SoleParticipantCannotLeave = "Вы единственный участник чата — покинуть его нельзя, можно только удалить чат.";
        }
    }
}
