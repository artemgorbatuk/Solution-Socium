namespace Services.Messaging.Texts;

public static class MessagingTexts
{
    public static class Messages
    {
        public static class Error
        {
            public const string ChannelNotRegistered = "Канал отправки сообщений не зарегистрирован.";
            public const string InvalidMessageType = "Тип сообщения не соответствует каналу отправки.";
            public const string SmtpHostNotConfigured = "SMTP Host не настроен.";
            public const string SmtpFromNotConfigured = "SMTP From не настроен.";
            public const string SendFailed = "Ошибка при отправке сообщения.";
            public const string UnsupportedConfirmationChannel = "Канал подтверждения не поддерживается.";
            public const string UnsupportedPasswordResetChannel = "Канал сброса пароля не поддерживается.";
        }

        public static class Success
        {
            public const string EmailSent = "Email отправлен.";
        }

        public static class Confirmation
        {
            public const string EmailSubject = "Подтверждение email";
            public const string EmailBody =
                "Здравствуйте, {0}!\n\nПодтвердите email по ссылке:\n{1}\n\nЕсли вы не регистрировались, проигнорируйте это письмо.";
        }

        public static class PasswordReset
        {
            public const string EmailSubject = "Сброс пароля";
            public const string EmailBody =
                "Здравствуйте, {0}!\n\nСбросьте пароль по ссылке:\n{1}\n\nЕсли вы не запрашивали сброс, проигнорируйте это письмо.";
        }
    }
}
