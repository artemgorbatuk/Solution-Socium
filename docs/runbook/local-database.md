[← Runbook](../runbook.md)

# Локальная база данных

PostgreSQL для локальной разработки запускается в Docker из `deploy/development/docker-compose.yml`. WebApi работает вне Docker и подключается к нему по строке `ConnectionStrings:Socium` из `src/backend/SociumApp/WebApi/appsettings.Development.json`.

| Параметр     | Значение                       |
|--------------|--------------------------------|
| Контейнер    | `socium-dev-postgres`          |
| Образ        | `postgres:18`                  |
| Хост и порт  | `localhost:5433`               |
| База данных  | `Socium`                       |
| Пользователь | `postgres` / пароль `postgres` |
| Том данных   | `socium-volume-dev-postgres-data` |

Порт `5433` выбран потому, что `5432` на машине разработчика может быть занят PostgreSQL других проектов.

## Запуск и остановка

Команды выполняются из корня репозитория.

```powershell
# запуск
docker compose -f deploy/development/docker-compose.yml up -d

# состояние (healthy — сервер принимает подключения)
docker inspect --format "{{.State.Health.Status}}" socium-dev-postgres

# остановка без удаления данных
docker compose -f deploy/development/docker-compose.yml down

# остановка с удалением данных (БД создастся заново при следующем запуске)
docker compose -f deploy/development/docker-compose.yml down -v
```

## Миграции

Инструмент `dotnet ef` закреплён в манифесте `dotnet-tools.json` в корне репозитория. После клонирования его нужно восстановить один раз:

```powershell
dotnet tool restore
```

В БД `Socium` два контекста со своими историями миграций: `DbContextSocium` (`__EFMigrationsHistory_Socium`) и `DbContextSecurity` (`__EFMigrationsHistory_Security`, таблицы `Users` и `RefreshTokens`). В окружении `Development` WebApi накатывает миграции сам при старте — сначала Socium, потом Security. Команды ниже выполняются из `src/backend/SociumApp/`; для Security — `--project Security/Datasource.Security.Ef --context DbContextSecurity`.

```powershell
# создать миграцию задачи (имя — TaskNNNN, см. rule-0003)
dotnet ef migrations add Task0003 --project Socium/Datasource.Socium.Ef --startup-project WebApi --context DbContextSocium

# удалить последнюю неприменённую миграцию
dotnet ef migrations remove --project Socium/Datasource.Socium.Ef --startup-project WebApi --context DbContextSocium

# применить миграции вручную
dotnet ef database update --project Socium/Datasource.Socium.Ef --startup-project WebApi --context DbContextSocium
```

Обе design-time factory берут строку подключения из переменной окружения `ConnectionStrings__Socium`, а если она не задана — локальную строку на `localhost:5433`. Вручную миграции применяются в том же порядке: сначала Socium, потом Security.

## Проверка

```powershell
docker exec socium-dev-postgres psql -U postgres -d Socium -c "\dt"
```

В списке должны быть таблицы `Rooms`, `Chats`, `Messages`, `Participants`, `Senders`, `Recipients`, `Users`, `RefreshTokens`, `__EFMigrationsHistory_Socium` и `__EFMigrationsHistory_Security`.
