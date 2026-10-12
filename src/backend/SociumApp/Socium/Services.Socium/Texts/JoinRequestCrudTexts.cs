namespace Services.Socium.Texts;

public static class JoinRequestCrudTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string Creating = "Начало подачи заявки на вступление в чат";
            public const string Accepting = "Начало принятия заявки на вступление в чат";
            public const string Rejecting = "Начало отклонения заявки на вступление в чат";
            public const string DisplayPendingListLoading = "Начало отображения списка заявок на вступление в чат";
        }

        public static class Success
        {
            public const string CreateCompleted = "Заявка на вступление в чат отправлена";
            public const string AcceptCompleted = "Заявка на вступление в чат принята";
            public const string RejectCompleted = "Заявка на вступление в чат отклонена";
            public const string DisplayPendingListCompleted = "Список заявок на вступление в чат успешно отображён";
        }

        public static class Error
        {
            public const string CreateError = "Не удалось подать заявку на вступление в чат";
            public const string AcceptError = "Не удалось принять заявку на вступление в чат";
            public const string RejectError = "Не удалось отклонить заявку на вступление в чат";
            public const string DisplayPendingListError = "Не удалось отобразить список заявок на вступление в чат";
        }

        public static class Canceled
        {
            public const string Create = "Подача заявки на вступление в чат отменена";
            public const string Accept = "Принятие заявки на вступление в чат отменено";
            public const string Reject = "Отклонение заявки на вступление в чат отменено";
            public const string DisplayPendingList = "Отображение списка заявок на вступление в чат отменено";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string IdCannotBeEmpty = "Идентификатор заявки не может быть пустым.";
            public const string ChatIdCannotBeEmpty = "Идентификатор чата не может быть пустым.";
            public const string CurrentUserNotFound = "Текущий пользователь не выбран или не найден.";
            public const string ChatNotFoundById = "Чат не найден по указанному идентификатору.";
            public const string JoinRequestNotFoundById = "Заявка не найдена по указанному идентификатору.";
            public const string AccessDenied = "Недостаточно прав для выполнения действия над заявкой.";
            public const string AlreadyParticipant = "Вы уже участник этого чата.";
            public const string PendingAlreadyExists = "Заявка на вступление в этот чат уже отправлена.";
            public const string NotPending = "Заявка уже обработана.";
            public const string TargetAlreadyParticipant = "Пользователь уже участник этого чата.";
        }
    }
}
