[← Runbook](../runbook.md)

# Тесты

Устройство тестов описано в [rule-0004](../rules/rule-0004-tests.md).

| Вид       | Где                                    | Чем                                                      |
|-----------|----------------------------------------|----------------------------------------------------------|
| Бекенд    | `src/backend/SociumApp/Tests/Units/`, `Integrations/` | xUnit v3 на Microsoft Testing Platform (раннер задан в `global.json`) |
| E2E       | `src/backend/SociumApp/Tests/E2Es/`    | Playwright для .NET в том же проекте `Tests`             |
| Фронтенд  | `src/frontend/SociumWeb/src/**/*.spec.ts` | Vitest через Angular CLI                              |

## Тесты бекенда и E2E

### Подготовка

- Запущенный локальный PostgreSQL — см. [local-database.md](local-database.md). Тесты создают на нём временные БД `{префикс}_{guid}` и удаляют их после прогона; рабочая БД `Socium` не затрагивается.
- Для E2E — Node.js и зависимости фронтенда (`npm install` в `src/frontend/SociumWeb/`). Chromium для Playwright при первом прогоне скачивается автоматически в `%LOCALAPPDATA%\ms-playwright` (нужен интернет).

Если в консоли запущен `dotnet watch` для WebApi, сборка тестов не сможет перезаписать `WebApi.exe`. Перед прогоном остановите бекенд и фронтенд, после прогона запустите снова (из корня репозитория):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/local/stop-solution.ps1
powershell -ExecutionPolicy Bypass -File scripts/local/start-solution.ps1
```

### Запуск

Команды выполняются из `src/backend/SociumApp/`.

```powershell
# все тесты (бекенд и E2E)
dotnet test

# без E2E — быстрый прогон
dotnet test -- --filter-not-trait "Category=E2E"

# только E2E
dotnet test -- --filter-trait "Category=E2E"

# один класс тестов
dotnet test -- --filter-class Tests.Integrations.Socium.RoomApiTests

# только unit-тесты (без БД)
dotnet test -- --filter-namespace "Tests.Units.*"
```

E2E-фикстура сама собирает фронтенд во временную папку, поднимает WebApi (окружение `Development`, временная БД) и статический сервер `scripts/e2e/serve-frontend-static.mjs` на свободных портах — запущенные dev-серверы для этого не нужны. Первый прогон E2E занимает около 30 секунд, в основном на сборку фронтенда.

### Подключение к PostgreSQL

Шаблон подключения берётся из переменной окружения `TEST_POSTGRES`, затем `ConnectionStrings__Socium`, иначе используется `localhost:5433` (`postgres` / `postgres`). Имя БД в шаблоне игнорируется.

Если сервер недоступен, тесты падают сразу с сообщением `PostgreSQL для тестов недоступен (...)` и командой запуска compose.

### Проверка

После прогона в списке БД не должно остаться временных баз:

```powershell
docker exec socium-dev-postgres psql -U postgres -At -c "SELECT datname FROM pg_database ORDER BY 1"
```

Если E2E упал при старте, в сообщении теста есть вывод процесса (WebApi, сборки фронтенда или статического сервера), на котором он остановился.

## Тесты фронтенда

Команды выполняются из `src/frontend/SociumWeb/`. Бекенд и БД не нужны: HTTP-запросы подменяются в тестах.

```powershell
# один прогон
npm test -- --watch=false

# в режиме наблюдения
npm test
```
