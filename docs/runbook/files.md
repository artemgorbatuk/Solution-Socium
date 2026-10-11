[← Runbook](../runbook.md)

# Файлы и вложения

Вложения к сообщениям чата — [task-0013](../tasks/task-0013.md). Байты на диске (`Services.Files` / `LocalFileStore`), метаданные — область `Files` (`StoredFiles`, БД `Socium`).

## Каталог хранения

| Окружение | `Files:RootPath` |
|-----------|------------------|
| Development (WebApi на хосте) | `data/files` относительно рабочей директории WebApi (`WebApi/data/files`, в `.gitignore`) |
| Testing / E2E | уникальный temp-каталог, задаётся фикстурой (`Files:RootPath` / `Files__RootPath`) |
| Docker (когда backend в compose) | volume → `/app/files` |

Каталог создаётся при старте `LocalFileStore`, если его ещё нет.

## API

| Метод | Маршрут | Назначение |
|-------|---------|------------|
| `POST` | `/api/file` | multipart `file` — загрузка, ответ с `id` |
| `GET` | `/api/file/{id}` | скачивание (участник чата сообщения или загрузивший unbound-файл) |
| `POST` | `/api/message` | тело с `text` и/или `fileIds` |

## Настройки

Секция `Files` в `src/backend/SociumApp/WebApi/appsettings.json`:

| Ключ | Смысл |
|------|--------|
| `Files:RootPath` | Корень на диске |
| `Files:MaxBytes` | Макс. размер файла (5 MiB) |
| `Files:MaxAttachmentsPerMessage` | Макс. вложений на сообщение |
| `Files:AllowedContentTypes` | Разрешённые MIME (картинки, PDF, Office, код/текст) |

Если клиент присылает пустой Content-Type или `application/octet-stream`, сервис выводит MIME по расширению файла (`ContentTypeResolver`).

## Категории в системном диалоге

Скрепка открывает системный диалог через `showOpenFilePicker` (Chrome/Edge): в списке «Тип файла» — **Все / Картинки / Office / PDF / Код**. Отдельного выбора категории на странице нет.

Если API недоступен — fallback на `<input type="file">` с общим `accept` (один пункт «пользовательские файлы»).

| Категория | Примеры |
|-----------|---------|
| Все | объединение ниже |
| Картинки | jpeg, png, webp, gif |
| Office | doc/docx, xls/xlsx, ppt/pptx, odt/ods/odp, rtf |
| PDF | pdf |
| Код | html, css, js/ts, cs, json, xml, py, sql, md, txt, yaml, sh |

## Тесты

Фикстуры задают отдельный `Files:RootPath` в temp, общий каталог разработчика не используется.
