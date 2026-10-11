namespace Services.Messaging.Tokens.Texts;

public static class EmailTokenTexts
{
    public static class Messages
    {
        public static class Error
        {
            public const string SendConfirmationFailed = "Не удалось отправить письмо подтверждения.";
            public const string SendPasswordResetFailed = "Не удалось отправить письмо сброса пароля.";
            public const string AcceptFailed = "Не удалось подтвердить email.";
            public const string UnsupportedChannel = "Канал отправки не поддерживается.";
        }

        public static class Success
        {
            public const string ConfirmationSent = "Письмо подтверждения отправлено.";
            public const string PasswordResetSent = "Письмо сброса пароля отправлено.";
            public const string ConfirmationAccepted = "Email подтверждён.";
            public const string PasswordResetAccepted = "Токен сброса пароля принят.";
        }
    }
}
