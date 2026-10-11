# Realtime (SignalR)

## Хаб

| Параметр | Значение |
|----------|----------|
| Маршрут | `/hubs/chat` |
| Методы клиента → сервер | `JoinChat(chatId)`, `LeaveChat(chatId)` |
| Методы сервер → клиент | `messageCreated`, `messageUpdated`, `messageDeleted`, `participantJoined`, `participantLeft`, `participantRoleChanged`, `chatListUpdated` |
| Группы | `chat:{chatId}` — открытый чат; `user:{userId}` — список чатов (при `OnConnected`) |

Аутентификация: тот же JWT, что у REST. Браузерный клиент передаёт токен в query `access_token` (стандарт SignalR); WebApi читает его в `JwtBearerEvents.OnMessageReceived` для путей `/hubs`.

Join разрешён только участнику чата; иначе `HubException`.

## Клиент (Angular)

- Пакет `@microsoft/signalr`, сервис `app/realtime/chat-hub.ts`.
- Dev-proxy: кроме `/api` проксируется `/hubs` с `ws: true` на backend.
- После reconnect: снова Join + `GET` последних 50 сообщений и полный список участников (локальная история сбрасывается).
- Список чатов: `ensureConnected()` поднимает соединение (user-группа); событие `chatListUpdated` патчит превью / unread и пересортировывает строки.

## Notify после сохранения

`ServiceMessage` / `ServiceParticipant` вызывают `IChatNotifyPort` после успешного `SaveChanges`. Сбой отправки в хаб логируется и **не** откатывает уже сохранённые данные.

После create/update/delete последнего сообщения и после mark-read — `NotifyChatListUpdatedAsync` в группу `user:{id}` участников.

## Пагинация сообщений

`GET /api/message?chatId=…` — последние 50; при открытии хвоста ленты проставляются `MessageDelivered` / `MessageRead` у получателя.  
`GET /api/message?chatId=…&beforeId=…` — до 50 сообщений старше указанного.

## Статусы доставки (Recipient)

| API | Назначение |
|-----|------------|
| `POST /api/message/delivered` | `{ chatId, messageIds }` — `MessageDelivered` |
| `POST /api/message/read` | `{ chatId }` — `MessageRead` по чату; в ответе `unreadCount` |

Unread в списке чатов: число recipient’ов текущего пользователя с `MessageRead IS NULL`.
