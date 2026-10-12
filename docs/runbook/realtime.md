# Realtime (SignalR)

## Хаб

| Параметр | Значение |
|----------|----------|
| Маршрут | `/hubs/chat` |
| Методы клиента → сервер | `JoinChat(chatId)`, `LeaveChat(chatId)`, `StartTyping(chatId)`, `StopTyping(chatId)` |
| Методы сервер → клиент | `messageCreated`, `messageUpdated`, `messageDeleted`, `participantJoined`, `participantLeft`, `participantRoleChanged`, `chatListUpdated`, `roomChanged`, `chatChanged`, `typingStarted`, `typingStopped` |
| Группы | `chat:{chatId}` — открытый чат; `user:{userId}` — список чатов (при `OnConnected`); `roomChanged` / `chatChanged` — broadcast всем подключениям |

Аутентификация: тот же JWT, что у REST. Браузерный клиент передаёт токен в query `access_token` (стандарт SignalR); WebApi читает его в `JwtBearerEvents.OnMessageReceived` для путей `/hubs`.

Join / typing разрешены только участнику чата; иначе `HubException`. Typing уходит в `OthersInGroup` (сам себе индикатор не получает).

## Клиент (Angular)

- Пакет `@microsoft/signalr`, сервис `app/realtime/chat-hub.ts`.
- Dev-proxy: кроме `/api` проксируется `/hubs` с `ws: true` на backend.
- После reconnect: снова Join + `GET` последних 50 сообщений и полный список участников (локальная история сбрасывается).
- Список чатов: `ensureConnected()` поднимает соединение (user-группа); событие `chatListUpdated` патчит превью / unread и пересортировывает строки.
- Список комнат: то же соединение; событие `roomChanged` (`created` / `updated` / `deleted`) патчит сайдбар без GET `/api/room`.
- Список чатов комнаты: событие `chatChanged` (`created` / `updated` / `deleted`) патчит `RoomChats` без GET `/api/chat?roomId=`; `chatListUpdated` по-прежнему только превью / unread.
- Typing: при наборе в composer — `StartTyping` (refresh ~2 с), idle ~3 с / отправка / leave — `StopTyping`; индикатор над composer.

## Notify после сохранения

`ServiceMessage` / `ServiceParticipant` вызывают `IChatNotifyPort` после успешного `SaveChanges`. Сбой отправки в хаб логируется и **не** откатывает уже сохранённые данные.

После create/update/delete последнего сообщения и после mark-read — `NotifyChatListUpdatedAsync` в группу `user:{id}` участников.

`ServiceRoom` после `SaveChanges` публикует `RoomChangedEvent` → handler → `NotifyRoomChangedAsync` (broadcast `Clients.All`).

`ServiceChat` после `SaveChanges` (ручной create / update / delete) публикует `ChatChangedEvent` → handler → `NotifyChatChangedAsync` (broadcast). Авточат при создании комнаты `ChatChanged` не шлёт.

## Пагинация сообщений

`GET /api/message?chatId=…` — последние 50; при открытии хвоста ленты проставляются `MessageDelivered` / `MessageRead` у получателя.  
`GET /api/message?chatId=…&beforeId=…` — до 50 сообщений старше указанного.

## Статусы доставки (Recipient)

| API | Назначение |
|-----|------------|
| `POST /api/message/delivered` | `{ chatId, messageIds }` — `MessageDelivered` |
| `POST /api/message/read` | `{ chatId }` — `MessageRead` по чату; в ответе `unreadCount` |

Unread в списке чатов: число recipient’ов текущего пользователя с `MessageRead IS NULL`.
