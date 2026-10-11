# Realtime (SignalR)

## Хаб

| Параметр | Значение |
|----------|----------|
| Маршрут | `/hubs/chat` |
| Методы клиента → сервер | `JoinChat(chatId)`, `LeaveChat(chatId)` |
| Методы сервер → клиент | `messageCreated`, `messageUpdated`, `messageDeleted`, `participantJoined`, `participantLeft`, `participantRoleChanged` |
| Группа | `chat:{chatId}` |

Аутентификация: тот же JWT, что у REST. Браузерный клиент передаёт токен в query `access_token` (стандарт SignalR); WebApi читает его в `JwtBearerEvents.OnMessageReceived` для путей `/hubs`.

Join разрешён только участнику чата; иначе `HubException`.

## Клиент (Angular)

- Пакет `@microsoft/signalr`, сервис `app/realtime/chat-hub.ts`.
- Dev-proxy: кроме `/api` проксируется `/hubs` с `ws: true` на backend.
- После reconnect: снова Join + `GET` последних 50 сообщений и полный список участников (локальная история сбрасывается).

## Notify после сохранения

`ServiceMessage` / `ServiceParticipant` вызывают `IChatNotifyPort` после успешного `SaveChanges`. Сбой отправки в хаб логируется и **не** откатывает уже сохранённые данные.

## Пагинация сообщений

`GET /api/message?chatId=…` — последние 50.  
`GET /api/message?chatId=…&beforeId=…` — до 50 сообщений старше указанного.
