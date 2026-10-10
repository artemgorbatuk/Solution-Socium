[← Runbook](../runbook.md)

# Вход и ключ подписи

Вход устроен по [task-0011](../tasks/task-0011.md): access-токен JWT живёт 15 минут в памяти фронтенда, refresh-токен — в `HttpOnly`-cookie `socium_refresh` (14 дней с «Запомнить меня», иначе до закрытия браузера). Учётные записи — таблицы `Users` и `RefreshTokens` области `Security` в той же БД `Socium`.

## Ключ подписи JWT

Ключ `Security:Jwt:SigningKey` в репозитории не хранится. Без него (или с ключом короче 32 байт) WebApi не запускается с ошибкой проверки `JwtOptions`.

Локально ключ лежит в user-secrets проекта `WebApi`. `scripts/local/start-solution.ps1` при первом запуске сам создаёт случайный ключ (48 байт, Base64) и значение не выводит. Вручную — из корня репозитория:

```powershell
# есть ли ключ (значение будет видно в консоли)
dotnet user-secrets list --project src/backend/SociumApp/WebApi

# задать свой ключ
dotnet user-secrets set "Security:Jwt:SigningKey" "<не короче 32 байт>" --project src/backend/SociumApp/WebApi
```

Смена ключа делает недействительными все выданные access-токены; сессии восстанавливаются сами по refresh-cookie при следующем запросе.

Вне локального окружения ключ задаётся переменной окружения `Security__Jwt__SigningKey`. Тесты генерируют новый ключ на каждый запуск и user-secrets не используют.

## Настройки

Секция `Security` в `src/backend/SociumApp/WebApi/appsettings.json`:

| Ключ                            | По умолчанию | Смысл                                              |
|---------------------------------|--------------|----------------------------------------------------|
| `Security:Jwt:AccessTokenMinutes` | `15`       | Срок жизни access-токена                           |
| `Security:Jwt:RefreshTokenDays`   | `14`       | Срок жизни refresh-токена, продлевается при обновлении |
| `Security:RateLimit:PermitLimit`  | `10`       | Попыток входа и регистрации с одного IP за окно    |
| `Security:RateLimit:WindowSeconds` | `60`      | Окно ограничения частоты, секунды                  |

## Cookie на `http://127.0.0.1`

Cookie помечена `Secure`, но Chromium принимает такие cookie и на `http://127.0.0.1` и `http://localhost` — dev-сервер Angular с прокси `/api` работает без HTTPS. Если браузер не сохраняет сессию после перезагрузки, проверьте, что приложение открыто по `127.0.0.1` или `localhost`, а не по IP сети.

## Забытый пароль

Сброса пароля нет. Локально пользователя можно удалить из БД (refresh-токены удалятся каскадом) и зарегистрировать заново:

```powershell
"DELETE FROM ""Users"" WHERE ""Login"" = 'login';" | docker exec -i socium-dev-postgres psql -U postgres -d Socium
```

Его участие в чатах и сообщения останутся и будут подписаны «Удалённый пользователь».

## Разовая очистка task-0011

`scripts/local/cleanup-task-0011.ps1` удаляет пользователей task-0010, созданных без пароля, вместе с их сообщениями и участием в чатах, а затем чаты без участников; комнаты не трогает. Запускается один раз до применения миграций `Task0011`. На локальной БД разработчика уже выполнен 10.10.2026; копия БД до очистки — `%LOCALAPPDATA%\Temp\socium-before-task-0011.sql` (восстановление — `Get-Content <файл> | docker exec -i socium-dev-postgres psql -U postgres -d <новая БД>`).
