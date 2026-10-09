# rule-0003: Backend

[← Реестр правил](../rules.md)

**Назначение:**

Единая структура бекенда: состав и именование библиотек, слоёв, файлов и порядок работы с данными, чтобы каждая новая сущность делалась по одному шаблону.

**Область применения:**

Все проекты в `src/backend/SociumApp/`: area-библиотеки, общие библиотеки, `WebApi`, миграции и конфигурация.

**Правила:**

1. Area: предметная область (`Socium`, …) — папка `src/backend/SociumApp/{Area}/` с цепочкой библиотек `Datasource.{Area}.Ef` → `Repositories.{Area}.Ef` → `Services.{Area}`; в `Solution-Socium.slnx` проекты area лежат в папке решения `/{Area}/`.
2. Общие библиотеки: код, нужный нескольким area, — в корне `src/backend/SociumApp/` (`Services.Shared`, …); общая библиотека не ссылается на area и на ASP.NET.
3. Зависимости: `WebApi` → `Services.{Area}` → `Repositories.{Area}.Ef` → `Datasource.{Area}.Ef`; `Services.{Area}` также ссылается на `Services.Shared`; `WebApi` дополнительно ссылается на `Datasource.{Area}.Ef` только ради регистрации контекста. Обратные и «перепрыгивающие» ссылки (контроллер → репозиторий) запрещены.
4. Папки: папка создаётся только при появлении в ней файла; папки называются по роли кода, а не по фиче.
   - `Datasource.{Area}.Ef`: `Common/`, `Configurations/`, `Contexts/`, `Factories/`, `Migrations/`, `Models/`, `Seeds/`.
   - `Repositories.{Area}.Ef`: `Api/`, `Options/`, `Models/` (проекции запросов).
   - `Services.{Area}`: `Api/`, `Models/`, `Mapping/`, `Validation/`, `Texts/`, `Rules/`, `Normalizers/`.
5. Файлы: один публичный класс — один файл, имя файла совпадает с именем класса; интерфейс лежит в одном файле со своей единственной реализацией.
6. Именование:

   | Элемент                  | Имя                                  | Пример                             |
   |--------------------------|--------------------------------------|------------------------------------|
   | Сущность EF              | `{Entity}` в единственном числе      | `Room`                             |
   | Таблица                  | `{Entity}` во множественном числе    | `Rooms`                            |
   | Конфигурация сущности    | `Configuration{Entity}`              | `ConfigurationRoom`                |
   | Seed-данные              | `Seed{Entity}`                       | `SeedRoom`                         |
   | Контекст                 | `DbContext{Area}`                    | `DbContextSocium`                  |
   | Design-time factory      | `{Area}DbContextFactoryDesignTime`   | `SociumDbContextFactoryDesignTime` |
   | Репозиторий              | `IRepository{Entity}` / `Repository{Entity}` | `RepositoryRoom`           |
   | Unit of Work             | `IUnitOfWork{Area}` / `UnitOfWork{Area}` | `UnitOfWorkSocium`             |
   | Фильтры выборки          | `{Entity}QueryOptions`               | `RoomQueryOptions`                 |
   | Параметры `GetNew`       | `{Entity}GetNewOptions`              | `RoomGetNewOptions`                |
   | Сервис                   | `IService{Entity}` / `Service{Entity}` | `ServiceRoom`                    |
   | Контракт CRUD            | файл `{Entity}Crud.cs`, классы `{Entity}{Action}Request` / `{Entity}{Action}Response` | `RoomCreateRequest` |
   | Маппер                   | `{Entity}Mapper`                     | `RoomMapper`                       |
   | Валидаторы CRUD          | `{Entity}CrudValidators`             | `RoomCrudValidators`               |
   | Тексты CRUD              | `{Entity}CrudTexts`                  | `RoomCrudTexts`                    |
   | Контроллер               | `{Entity}Controller`                 | `RoomController`                   |

7. Контекст: `OnModelCreating` подключает конфигурации только через `ApplyConfigurationsFromAssembly`; у каждого контекста своя таблица истории миграций `__EFMigrationsHistory_{Area}`.
8. Design-time factory: используется только инструментами `dotnet ef` и в DI не регистрируется; строку подключения берёт из переменной окружения `ConnectionStrings__{Area}`, а при её отсутствии — локальную строку разработки.
9. Сущность: первичный ключ — `Guid` v7; обязательные строки — `required`; navigation properties — `virtual`; коллекции инициализируются `= []`. Сущность не содержит логики и атрибутов валидации.
10. Генерация ключа: в конфигурации ключ задаётся как `ValueGeneratedOnAdd().HasValueGenerator<GuidV7ValueGenerator>()`; генератор лежит в `Datasource.{Area}.Ef/Common/`.
11. Конфигурация: одна сущность — один `Configuration{Entity}`; таблица задаётся `ToTable` явно; свойства описываются в порядке модели, связи (`HasOne` / `HasMany`) — в конце.
12. Строки и индексы: обязательные строки — `IsRequired()`, у каждой строковой колонки задан `HasMaxLength(...)` (кроме `jsonb` / `text` по решению задачи); индексы именуются явно `IX_{Table}_{Column1}_{Column2}`; уникальные бизнес-ключи — `IsUnique()`.
13. Связи: «один ко многим» описывается с зависимой стороны (`HasOne` → `WithMany`) с явными `HasForeignKey` и `OnDelete`; «многие ко многим» — `HasMany` → `WithMany` → `UsingEntity` с явной join-таблицей, FK и составным ключом.
14. Seed-данные: только с фиксированными `Guid`, в `Seed{Entity}`, подключаются из `Configuration{Entity}`.
15. Миграции: имя — `TaskNNNN` по номеру задачи; одна миграция на задачу на контекст (если модель изменилась после создания — `dotnet ef migrations remove` и создать заново с тем же именем); хранятся в `Datasource.{Area}.Ef/Migrations/`; startup-проект — `WebApi`.
    - Создание: `dotnet ef migrations add Task0003 --project Socium/Datasource.Socium.Ef --startup-project WebApi --context DbContextSocium`.
16. Применение миграций: приложение накатывает миграции при старте только в окружении `Development`; в остальных окружениях схема применяется отдельным шагом развёртывания.
17. Репозиторий: только меняет состояние контекста (`Add` / `Update` / `Remove`) и выполняет запросы; не вызывает `SaveChanges`, не валидирует и не бросает исключения ради бизнес-правил.
18. Методы репозитория: базовый набор — `GetNew`, `GetSingleOrDefaultAsync(id)`, `GetListAsync({Entity}QueryOptions?)`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`; методы добавляются только при появлении потребности в сервисе.
19. Фильтры выборки: все критерии выборки передаются через `{Entity}QueryOptions`, а не отдельными параметрами метода; в сервисе options создаются отдельной локальной переменной, а не inline в аргументе вызова.
20. Трекинг: списки читаются с `AsNoTracking()`; `GetSingleOrDefaultAsync` возвращает отслеживаемую сущность, чтобы её можно было изменить и сохранить.
21. `GetNew`: без параметров возвращает сущность с пустым `Id` и значениями по умолчанию; с `{Entity}GetNewOptions` — сразу заполненную.
22. Unit of Work: `UnitOfWork{Area}` создаёт репозитории area и единственный владеет `SaveChangesAsync` и транзакциями (`BeginTransactionAsync` / `CommitTransactionAsync` / `RollbackTransactionAsync`); репозитории в DI не регистрируются.
23. Исход операции: метод сервиса возвращает `ResponseInfo<TResponse>` из `Services.Shared`; `ResponseInfo` создаётся только фабричными методами (`Success`, `Error`, `Invalid`, `BadRequest`, `NotFound`, `Unauthorized`, `Forbidden`) с подходящим `MessageType`. `ResponseInfo`, `MessageInfo` и `MessageType` повторяют эталон, кроме добавленных `Unauthorized` / `Forbidden` и их констант.
24. `MessageType`: статический класс `int`-констант `NONE`, `LOADED`, `SAVED`, `ERROR`, `INVALID`, `BAD_REQUEST`, `NOT_FOUND`, `WARNING`, `UNAUTHORIZED`, `FORBIDDEN` с методами оформления (`GetDescription`, `GetAlertClass`, `GetTextColorClass`, `GetBorderClass`, `GetBackgroundClass`, `GetIconClass`, `GetTempDataKey`); значения констант не меняются — на них опирается фронтенд.
25. Сервис: метод `Service{Entity}` — сценарий операции: валидация → чтение → изменение → `SaveChangesAsync` → ответ. Вспомогательная логика выносится в `Validation/`, `Mapping/`, `Rules/`, `Normalizers/`; `private`-методы в `Api/` создаются только после согласования.
26. Зависимости сервиса: сервис получает `IUnitOfWork{Area}`, `ILogger<T>` и вспомогательные абстракции с суффиксами по роли (`*Provider`, `*Context`, `*Hasher`, …); другие `IService*` в сервис не внедряются.
27. Валидация: порядок проверок — primary (запрос заполнен корректно, `ResponseInfo.BadRequest` с `BAD_REQUEST`) → текущий пользователь, если операция его требует (выбран, найден и не удалён, `ResponseInfo.Unauthorized` с `UNAUTHORIZED`) → accessibility (сущность найдена, `ResponseInfo.NotFound` с `NOT_FOUND`) → права (пользователю разрешено действие над найденной сущностью, `ResponseInfo.Forbidden` с `FORBIDDEN`) → domain (бизнес-правила, `ResponseInfo.Invalid` с `INVALID`); фабричный метод и `MessageType` всегда соответствуют друг другу; проверки — методы `{Entity}CrudValidators`, возвращающие `IEnumerable<string>`.
28. Тексты: все сообщения для пользователя и логов — константы `{Entity}CrudTexts` с группами `Start`, `Success`, `Error`, `Canceled`, `Validation`; строковые литералы сообщений в сервисе и валидаторах не пишутся.
29. Обработка исключений в сервисе: `OperationCanceledException` логируется текстом `Canceled` и пробрасывается; прочие исключения логируются (`logger.LogError(exception, "{Message}", …)`) и превращаются в `ResponseInfo.Error`.
30. Контракт сервиса: request/response лежат в `Services.{Area}/Models/` и не совпадают с сущностями EF; сущность наружу сервиса не отдаётся; коллекции в контракте — `ICollection<T>`.
31. Маппинг: преобразование сущности в контракт и обратно — статические методы `{Entity}Mapper`; маппинг в контроллере и репозитории запрещён.
32. CancellationToken: каждый асинхронный метод репозитория, Unit of Work и сервиса принимает `CancellationToken cancellationToken = default` и передаёт его дальше.
33. `Program.cs`: содержит только вызовы extension-методов и базовый pipeline; регистрация DI, контекстов и CORS — в `WebApi/Middleware/ServiceRegistration.cs` (`Add*Ext`).
34. Регистрация в DI: контекст — `AddDbContextFactory<DbContext{Area}>` + scoped-контекст из factory; `IUnitOfWork{Area}` и `IService{Entity}` — scoped.
35. Контроллер: наследует `ApiControllerBase`, маршрут `api/[controller]`; действие принимает один параметр запроса (`[FromQuery]` для GET/DELETE, `[FromBody]` для POST/PUT) и `CancellationToken`, вызывает один метод сервиса и возвращает `FromResponse(...)`; без бизнес-логики.
36. Идентификатор в запросе: `Id` сущности передаётся в query или body, а не в сегменте URL.
37. CRUD-эндпоинты: набор действий контроллера сущности:

   | Метод    | Маршрут                    | Действие        | Метод сервиса            |
   |----------|----------------------------|-----------------|--------------------------|
   | `GET`    | `api/{entity}`             | `List`          | `DisplayListPageAsync`   |
   | `GET`    | `api/{entity}/info`        | `Info`          | `DisplayInfoPageAsync`   |
   | `GET`    | `api/{entity}/create`      | `Create`        | `DisplayCreatePageAsync` |
   | `POST`   | `api/{entity}`             | `Create`        | `CreateAsync`            |
   | `GET`    | `api/{entity}/update`      | `Update`        | `DisplayUpdatePageAsync` |
   | `PUT`    | `api/{entity}`             | `Update`        | `UpdateAsync`            |
   | `GET`    | `api/{entity}/delete`      | `DeleteConfirm` | `DisplayDeletePageAsync` |
   | `DELETE` | `api/{entity}`             | `Delete`        | `DeleteAsync`            |

38. HTTP-ответ: `ApiControllerBase.FromResponse` переводит `MessageType` в ответ — `LOADED` / `SAVED` / `WARNING` → `200` с `ApiSuccessResponse<T>`; `BAD_REQUEST` → `400`, `UNAUTHORIZED` → `401`, `FORBIDDEN` → `403`, `NOT_FOUND` → `404`, `INVALID` → `422`, `ERROR` → `500` — Problem Details через `ApiProblem`. `ApiControllerBase`, `ApiSuccessResponse` и `ApiProblem` повторяют эталон, кроме веток `401` / `403`, включая поля оформления (`messageTypeDescription`, `messageAlert`, `messageTextColor`, `messageBorderColor`, `messageBackgroundColor`, `messageIcon`).
39. Конфигурация: строка подключения — `ConnectionStrings:{Area}`; опции библиотек — отдельные секции appsettings с Options-классом; код ветвится только по окружениям `Development` / `Testing`, а не по имени стенда.
40. Секреты: пароли, ключи и токены в репозиторий не попадают; для окружений, кроме локального, они задаются переменными окружения или user-secrets.

**Исключения:**

1. Локальная разработка: строка подключения к локальной БД с паролем допускается в `WebApi/appsettings.Development.json` и как запасная строка в design-time factory (п. 8, 40) — согласовано в [task-0003](../tasks/task-0003.md).
2. Абстракции HTTP-окружения: интерфейс, который сервис получает от `WebApi` (например, `ICurrentUser`), лежит в `Services.{Area}/Api/` отдельно от реализации, а реализация — в `WebApi/Middleware/`, потому что `Services.{Area}` не ссылается на `WebApi` (п. 3, 5) — согласовано в [task-0010](../tasks/task-0010.md).
3. Участники чата: `ParticipantController` (`api/participant`) содержит только `List` (`GET`), `Create` — вступление текущего пользователя (`POST`), `Update` — смена роли (`PUT`), `DeleteConfirm` — страница выхода с причиной, если уйти нельзя (`GET delete`), и `Delete` — выход (`DELETE`); страниц создания и обновления и `Info` нет (п. 37) — согласовано в [task-0010](../tasks/task-0010.md).
4. Коды `401` / `403`: `MessageType.UNAUTHORIZED` и `FORBIDDEN`, фабрики `ResponseInfo.Unauthorized` / `Forbidden` и их ветки в `ApiControllerBase.FromResponse` добавлены сверх эталона (п. 23, 24, 38) — согласовано в [task-0010](../tasks/task-0010.md).

**Связанные документы:**

- Реестр правил: `docs/rules.md`
- Оформление задач: `docs/rules/rule-0001-tasks.md`
- Оформление правил: `docs/rules/rule-0002-rules.md`
- Эталон структуры: `autoclass-reports.ru/src/backend/ReportsApp/Reports`, правило эталона `autoclass-reports.ru/docs/solution-rules/rule-0006-backend.md`
