namespace Services.Security.Texts;

public static class AccountTexts
{
    public static class Messages
    {
        public static class Start
        {
            public const string LoggingIn = "Начало выполнения входа";
            public const string Refreshing = "Начало выполнения обновления токенов";
            public const string LoggingOut = "Начало выполнения выхода";
            public const string DisplayProfileLoading = "Начало отображения профиля";
            public const string ProfileUpdating = "Начало выполнения обновления профиля";
            public const string ConfirmingEmail = "Начало подтверждения email";
            public const string ResendingConfirmation = "Начало повторной отправки письма подтверждения";
            public const string ForgettingPassword = "Начало запроса сброса пароля";
            public const string ResettingPassword = "Начало сброса пароля";
        }

        public static class Success
        {
            public const string LoginCompleted = "Вход выполнен";
            public const string RefreshCompleted = "Токены обновлены";
            public const string LogoutCompleted = "Выход выполнен";
            public const string DisplayProfileCompleted = "Профиль успешно отображён";
            public const string ProfileUpdateCompleted = "Профиль успешно обновлён";
            public const string ConfirmEmailCompleted = "Email подтверждён";
            public const string ResendConfirmationCompleted = "Если аккаунт существует, письмо отправлено";
            public const string ForgotPasswordCompleted = "Если аккаунт существует, письмо отправлено";
            public const string ResetPasswordCompleted = "Пароль изменён";
        }

        public static class Error
        {
            public const string LoginError = "Не удалось войти";
            public const string RefreshError = "Не удалось обновить токены";
            public const string LogoutError = "Не удалось выйти";
            public const string DisplayProfileError = "Не удалось отобразить профиль";
            public const string ProfileUpdateError = "Не удалось обновить профиль";
            public const string ConfirmEmailError = "Не удалось подтвердить email";
            public const string ResendConfirmationError = "Не удалось отправить письмо";
            public const string ForgotPasswordError = "Не удалось отправить письмо";
            public const string ResetPasswordError = "Не удалось сбросить пароль";
        }

        public static class Canceled
        {
            public const string Login = "Вход отменён";
            public const string Refresh = "Обновление токенов отменено";
            public const string Logout = "Выход отменён";
            public const string DisplayProfile = "Отображение профиля отменено";
            public const string ProfileUpdate = "Обновление профиля отменено";
            public const string ConfirmEmail = "Подтверждение email отменено";
            public const string ResendConfirmation = "Повторная отправка отменена";
            public const string ForgotPassword = "Запрос сброса пароля отменён";
            public const string ResetPassword = "Сброс пароля отменён";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string InvalidCredentials = "Неверный логин или пароль.";
            public const string RefreshTokenInvalid = "Сессия истекла, войдите снова.";
            public const string RefreshTokenReused = "Сессия отозвана: токен обновления использован повторно, войдите снова.";
            public const string CurrentUserNotFound = "Требуется вход в систему.";
            public const string TokenRequired = "Токен не может быть пустым.";
            public const string TokenInvalid = "Ссылка недействительна или устарела.";
        }
    }
}
