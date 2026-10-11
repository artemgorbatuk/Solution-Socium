[← Runbook](../runbook.md)

# Почта и Mailpit

Подтверждение email и сброс пароля устроены по [task-0012](../tasks/task-0012.md). Письма уходят через `Messaging/Services.Messaging` (SMTP); одноразовые токены — `EmailTokens` в той же области (`Messaging`, БД `Socium`).

## Mailpit (локально)

В `deploy/development/docker-compose.yml` контейнер `socium-dev-mailpit` (`axllent/mailpit:latest`) с `MP_SMTP_DISABLE_RDNS=true` (на Docker Desktop reverse DNS клиента иначе тормозит SMTP). `scripts/local/start-solution.ps1` поднимает compose целиком — Mailpit стартует вместе с Postgres. SMTP в Development — `127.0.0.1:1025` через `System.Net.Mail.SmtpClient` (как в Автоклассе).

| Порт | Назначение |
|------|------------|
| `1025` | SMTP — WebApi шлёт сюда (`Messaging:Smtp` → `localhost:1025`) |
| `8025` | UI писем — http://localhost:8025 |

From в Development: `noreply@localhost`. Ссылки в письмах ведут на фронтенд: `http://127.0.0.1:4200/confirm-email?token=…` и `…/reset-password?token=…`.

## Настройки

Секция `Messaging` в `src/backend/SociumApp/WebApi/appsettings.json` (переопределения Development — в `appsettings.Development.json`):

| Ключ | Смысл |
|------|--------|
| `Messaging:Confirmation:BaseUrl` | Базовый URL страницы подтверждения (к нему добавляется `?token=`) |
| `Messaging:Confirmation:TokenDays` | Срок токена подтверждения |
| `Messaging:PasswordReset:BaseUrl` | Базовый URL страницы нового пароля |
| `Messaging:PasswordReset:TokenDays` | Срок токена сброса |
| `Messaging:Smtp:*` | Host, Port, From, SSL, `TimeoutSeconds` (по умолчанию 10) |
| `Messaging:RateLimit:PermitLimit` | Лимит повторной отправки и «забыли пароль» с одного IP |
| `Messaging:RateLimit:WindowSeconds` | Окно лимита, секунды |

Пароль SMTP в репозиторий не кладётся; вне локального окружения — переменные окружения / секреты.

## Тесты

В окружении `Testing` SMTP не используется: в DI регистрируется `CapturingMessageChannel`, письма копятся в памяти. Фикстуры подтверждают email в БД напрямую, чтобы остальные сценарии не ждали почту.
