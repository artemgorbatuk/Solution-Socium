namespace Services.Files.Stored.Texts;

public static class StoredFileTexts
{
    public static class Messages
    {
        public static class Success
        {
            public const string UploadCompleted = "Файл успешно загружен";
            public const string DownloadCompleted = "Файл успешно получен";
            public const string AttachCompleted = "Файлы привязаны к сообщению";
            public const string DeleteCompleted = "Файлы сообщения удалены";
        }

        public static class Error
        {
            public const string UploadFailed = "Не удалось загрузить файл";
            public const string DownloadFailed = "Не удалось скачать файл";
            public const string AttachFailed = "Не удалось привязать файлы к сообщению";
            public const string DeleteFailed = "Не удалось удалить файлы сообщения";
        }

        public static class Validation
        {
            public const string RequestCannotBeNull = "Запрос не может быть пустым.";
            public const string UserIdCannotBeEmpty = "Идентификатор пользователя не может быть пустым.";
            public const string FileIdCannotBeEmpty = "Идентификатор файла не может быть пустым.";
            public const string MessageIdCannotBeEmpty = "Идентификатор сообщения не может быть пустым.";
            public const string ContentRequired = "Содержимое файла обязательно.";
            public const string FileNameRequired = "Имя файла обязательно.";
            public const string ContentTypeNotAllowed = "Тип файла не разрешён.";
            public const string FileTooLarge = "Файл превышает допустимый размер.";
            public const string FileNotFound = "Файл не найден.";
            public const string NotUploader = "Файл принадлежит другому пользователю.";
            public const string AlreadyAttached = "Файл уже привязан к другому объекту.";
            public const string TooManyAttachments = "Слишком много вложений на одно сообщение.";
            public const string AccessDenied = "Нет доступа к файлу.";
            public const string FileIdsRequired = "Список файлов не может быть пустым.";
        }
    }
}
