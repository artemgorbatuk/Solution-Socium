# rule-0004: Тесты

[← Реестр правил](../rules.md)

**Назначение:**

Единая структура и порядок написания тестов бекенда, фронтенда и сквозных (E2E) тестов, чтобы тесты были изолированными, воспроизводимыми и одинаково устроенными для каждой сущности.

**Область применения:**

Проект `src/backend/SociumApp/Tests/` (тесты бекенда и E2E) и файлы `*.spec.ts` фронтенда `src/frontend/SociumWeb/`.

**Правила:**

1. Проект: все тесты бекенда — в одном проекте `src/backend/SociumApp/Tests/Tests.csproj`, xUnit v3 на Microsoft Testing Platform; раннер задаётся в `global.json` в корне репозитория (`"test": { "runner": "Microsoft.Testing.Platform" }`).
2. Запуск: `dotnet test` из `src/backend/SociumApp/` запускает все тесты бекенда и E2E; тесты не требуют ручной подготовки, кроме запущенного локального PostgreSQL (`deploy/development/docker-compose.yml`), а для E2E — Node.js и установленных зависимостей фронтенда (`npm install`).
3. Слои: каждый тест относится к одному слою, слой — папка верхнего уровня.

   | Папка           | Что проверяет                                                                 | БД  |
   |-----------------|-------------------------------------------------------------------------------|-----|
   | `Units/`        | Чистая логика: валидаторы, нормализаторы, мапперы, модель ответа, контроллерная база | Нет |
   | `Integrations/` | Сервис с реальной БД и API через `WebApplicationFactory<Program>`             | Да  |
   | `E2Es/`         | Сквозной путь через настоящие UI, WebApi и БД (Playwright), п. 23–28          | Да  |
   | `Infrastructure/` | Общая тестовая инфраструктура (`PostgresTestHost`, `TestProcess`), не тесты | —   |

4. Папки внутри слоя: по area или общей библиотеке — `Units/Socium/`, `Units/Shared/`, `Units/WebApi/`, `Integrations/Socium/`, `Integrations/Infrastructure/` (тесты тестовой инфраструктуры, которой нужна БД), `E2Es/Socium/`, `E2Es/Layout/` (общий каркас интерфейса: верхняя панель, тема).
5. Именование: имя каждого теста — бекенда, фронтенда и E2E — строится по одному шаблону `{Объект}_{Действие}_{Условие}_{Результат}`, на английском, части в PascalCase через `_`; по имени без чтения кода понятно, что проверяется, чем, в каком случае и что ожидается.
   - Набор тестов: класс unit- и интеграционных тестов — `{ТестируемыйКласс}Tests`, тестов API — `{Entity}ApiTests`, E2E — `{Экран}E2eTests`; namespace повторяет путь папки (`Tests.Units.Socium`). Во фронтенде — `describe('{Класс}')`; вложенный `describe` — только для общей подготовки, называется по объекту (`describe('Width')`).
   - Объект — что проверяется: страница CRUD (`Create`, `List`, `Info`, `Update`, `Delete`) в тестах сервиса и API; тип, фабрика или результат в unit-тестах (`CreateRequest`, `Success`, `ListModel`); часть экрана во фронтенде и E2E (`List`, `CreateForm`, `Menu`, `RenameForm`, `Dialog`, `Width`, `Room`).
   - Действие — чем воздействуем: HTTP-метод в API (`GET`, `POST`, `PUT`, `DELETE`); в сервисе — `Load` для `Display{Page}PageAsync` и `Submit` для `CreateAsync` / `UpdateAsync` / `DeleteAsync`; операция в unit-тестах (`ValidatePrimary`, `ValidateDomain`, `Map`, `Apply`, `Normalize`, `Create`, `Convert`); событие или действие пользователя во фронтенде и E2E (`Load`, `Render`, `Input`, `Click`, `Submit`, `Escape`, `Drag`, `Resize`, `Rename`, `Delete`).
   - Условие — обязательно, начинается с `With` или `Without`: входные данные или состояние (`WithEmptyId`, `WithExistingRoom`, `WithoutPress`). Основной сценарий тоже называется явно (`WithValidName`, `WithNoRooms`, `WithNoParameters`); слова-заглушки (`Request`, `Default`, `Value`) не используются.
   - Результат — начинается с `Should` и глагола (`ShouldReturn404`, `ShouldShowProblem`, `ShouldKeepRoom`); несколько проверяемых эффектов соединяются `And` (`ShouldReturn200AndRenameRoom`). Код ответа в API — HTTP-код (`ShouldReturn422`), в сервисе — `MessageType` (`ShouldReturnInvalid`).
   - Словарь условий: один и тот же случай во всех слоях называется одинаково — `WithEmptyId`, `WithUnknownId`, `WithExisting{Entity}`, `WithNullRequest`, `WithEmptyName`, `WithBlankName` (только пробелы в UI), `WithTooLongName`, `WithDuplicateName` (название другой записи), `WithServerError`.
   - Примеры: `Update_PUT_WithEmptyId_ShouldReturn400` (API), `Update_Submit_WithEmptyId_ShouldReturnBadRequest` (сервис), `CreateRequest_ValidateDomain_WithTooLongName_ShouldReturnNameMaximumLength` (unit), `it('RenameForm_Submit_WithUnchangedName_ShouldCloseWithoutRequest')` (фронтенд), `Room_Rename_WithDuplicateName_ShouldShowServerError` (E2E).
6. Минимальный набор для CRUD-сущности: unit-тесты `{Entity}CrudValidators`, `{Entity}Normalizer`, `{Entity}Mapper`; интеграционные тесты `Service{Entity}` с реальной БД; интеграционные тесты `{Entity}Controller` через HTTP — на каждый эндпоинт и каждый код ответа из rule-0003 (п. 38).
7. Общий контур: изменение `Services.Shared` или `WebApi/Controllers/Shared` сопровождается unit-тестами `ResponseInfo` и `ApiControllerBase.FromResponse`.
8. PostgreSQL в тестах: `Infrastructure/PostgresTestHost` создаёт на локальном сервере отдельную временную БД с уникальным именем и удаляет её на dispose; рабочая БД `Socium` тестами не используется.
9. Подключение: шаблон подключения берётся из `TEST_POSTGRES`, затем `ConnectionStrings__Socium`, иначе — локальный сервер `localhost:5433`; имя БД в шаблоне игнорируется.
10. Недоступный сервер: если PostgreSQL недоступен, тест падает сразу с сообщением, как его запустить; тесты не поднимают Docker-контейнеры сами.
11. Схема в тестах: фикстура интеграционных тестов применяет миграции (`Database.MigrateAsync`) к своей временной БД — так проверяются и миграции; `EnsureCreated` не используется.
12. Фикстуры: одна фикстура на набор тестов (`ICollectionFixture`) с `IAsyncLifetime`; фикстура владеет временной БД, `ServiceProvider` или `WebApplicationFactory` и освобождает их в `DisposeAsync`. Фикстура сервиса собирает DI теми же методами `ServiceRegistration`, что и WebApi, и вызывает сервис в новом scope на каждую операцию — как отдельный HTTP-запрос.
13. Изоляция: тест не зависит от порядка запуска и от данных других тестов — создаёт свои данные с уникальными значениями (например, суффикс из `Guid`), а проверки списков делает через наличие своих записей, а не через общее количество.
14. Окружение API-тестов: `WebApplicationFactory<Program>` запускается в окружении `Testing`, строка подключения подменяется через конфигурацию (`ConnectionStrings:{Area}`); `Program` объявлен как `public partial class Program`.
15. Проверки: результат сервиса проверяется по `MessageType` и текстам из `{Entity}CrudTexts`, а не по строковым литералам; HTTP-ответ — по коду и телу (`ApiSuccessResponse` / Problem Details).
16. Моки: внешние зависимости (HTTP-клиенты, каналы сообщений) подменяются; собственная БД не мокается — для неё используются интеграционные тесты.
17. Регрессия: исправление ошибки сопровождается тестом, который падал до исправления.
18. Фронтенд, стек и запуск: Vitest через Angular CLI — `npm test -- --watch=false` из `src/frontend/SociumWeb/`; spec-файл `{файл}.spec.ts` лежит рядом с тестируемым компонентом, директивой или сервисом.
19. Фронтенд, состав: у каждого компонента, директивы и сервиса с логикой есть spec; компонент проверяется через DOM — то, что видит и делает пользователь, — а не через внутреннее состояние; входы задаются `fixture.componentRef.setInput`, выходы проверяются подпиской.
20. Фронтенд, HTTP: настоящий бекенд не используется — `provideHttpClient()` и `provideHttpClientTesting()`; запрос проверяется `HttpTestingController.expectOne` по методу, URL и параметрам; в `afterEach` вызывается `http.verify()`. Успешный ответ строится через `successBody` из `shared/api/api-response.testing.ts`, ошибка — Problem Details `{ detail }` с HTTP-кодом.
21. Фронтенд, поиск элементов: по роли, `aria-label` и видимому тексту (`[role=alert]`, `[aria-label="…"]`); CSS-класс — только для элемента без доступной роли или подписи.
22. Фронтенд, глобальное состояние: `localStorage`, размер окна и другие глобальные значения тест задаёт сам и восстанавливает после себя (`beforeEach` / `afterEach`).
23. E2E, стек: Playwright для .NET (`Microsoft.Playwright`) в проекте `Tests`, папка `E2Es/{Area}/`; класс помечается `[Trait("Category", "E2E")]`, чтобы его можно было исключить фильтром `--filter-not-trait "Category=E2E"`.
24. E2E, контур: фикстура `E2Es/E2eAppFixture` поднимает всё сама — временную БД (`PostgresTestHost`), WebApi отдельным процессом (`dotnet exec WebApi.dll`, окружение `Development` — миграции применяет сам WebApi, строка подключения — `ConnectionStrings__{Area}`), сборку фронтенда во временную папку и статический сервер `scripts/e2e/serve-frontend-static.mjs` с прокси `/api` без изменения пути; рабочая БД и запущенные dev-серверы не используются.
25. E2E, порты и процессы: порты WebApi и фронтенда выбираются свободные; процессы запускаются через `Infrastructure/TestProcess`, их вывод попадает в сообщение об ошибке; на dispose процессы останавливаются, временные БД и сборка удаляются.
26. E2E, браузер: Chromium в headless-режиме; если он не установлен, фикстура ставит его сама (`Microsoft.Playwright.Program.Main(["install", "chromium"])`); каждый тест работает в своём контексте браузера (`NewContextAsync`) — без общего `localStorage` и cookies.
27. E2E, поиск и проверки: элементы ищутся по роли и доступному имени (`GetByRole`), проверки — через `Assertions.Expect` с автоожиданием, без `Task.Delay`; данные для подготовки и итоговой проверки — через HTTP-клиент фикстуры к тому же WebApi.
28. E2E, объём: E2E покрывают пользовательские сценарии экрана целиком (создать → изменить → удалить, отказ, ошибка сервера); варианты валидации и коды ответов проверяются интеграционными и фронтенд-тестами, а не E2E.

**Исключения:**

Нет.

**Связанные документы:**

- Реестр правил: `docs/rules.md`
- Backend: `docs/rules/rule-0003-backend.md`
- Локальная БД: `docs/runbook/local-database.md`
- Запуск тестов: `docs/runbook/tests.md`
- Правило эталона: `autoclass-reports.ru/docs/solution-rules/rule-0008-tests.md`
