# Правила для агента

## Domain events

1. Общая шина (`IDomainEvent`, `IDomainEventHandler<T>`, `IDomainEventPublisher`, реализация publisher) — в `Services.Shared/Events/`.
2. Конкретное событие area — в `Services.{Area}/Events/`, один файл на событие (например `RoomCreatedEvent.cs`).
3. Все handlers этого события — в **том же файле**, ниже класса события (отдельная папка `EventHandlers` не создаётся).
4. Реакции на событие лежат рядом с событием: новый handler на то же событие добавляется в тот же файл.
5. Handler вызывает `IService*` (`{Action}StageAsync` и т.п.), не дублирует запись в UoW и не вызывает `SaveChanges`; детали — rule-0003 п. 44–45.

## Интерфейс и реализация в одном файле

1. Имя файла — **без** префикса `I` (например `DomainEventPublisher.cs`, не `IDomainEventPublisher.cs`).
2. В файле сверху — интерфейс с префиксом `I` (`IDomainEventPublisher`).
3. Реализация(и) — **ниже** в том же файле (`DomainEventPublisher : IDomainEventPublisher`).
4. Порты / HTTP-абстракции (rule-0003 п. 41, искл. 2): адаптер в другой сборке — в файле area только интерфейс, имя файла всё равно без `I` (например `UserPort.cs` с `IUserPort`).
5. Marker-only без единственной реализации (`IDomainEvent`, `IDomainEventHandler<T>`) — файл без `I`, реализация в том же файле не требуется.
