import { Injectable, inject } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { Auth } from '../account/auth';
import {
  ChatListNotify,
  ChatMessageDeletedNotify,
  ChatMessageNotify,
  ChatParticipantLeftNotify,
  ChatParticipantNotify,
  ChatTypingStartedNotify,
  ChatTypingStoppedNotify,
  RoomChangedNotify,
} from './chat-hub.models';

/**
 * Клиент ChatHub: одно соединение на вкладку, Join/Leave по открытому чату.
 * Группа `user:{userId}` подключается автоматически на OnConnected (список чатов).
 */
@Injectable({ providedIn: 'root' })
export class ChatHub {
  private readonly auth = inject(Auth);
  private connection: HubConnection | null = null;
  private joinedChatId: string | null = null;
  private startPromise: Promise<void> | null = null;

  readonly messageCreated$ = new Subject<ChatMessageNotify>();
  readonly messageUpdated$ = new Subject<ChatMessageNotify>();
  readonly messageDeleted$ = new Subject<ChatMessageDeletedNotify>();
  readonly participantJoined$ = new Subject<ChatParticipantNotify>();
  readonly participantLeft$ = new Subject<ChatParticipantLeftNotify>();
  readonly participantRoleChanged$ = new Subject<ChatParticipantNotify>();
  readonly chatListUpdated$ = new Subject<ChatListNotify>();
  readonly roomChanged$ = new Subject<RoomChangedNotify>();
  readonly typingStarted$ = new Subject<ChatTypingStartedNotify>();
  readonly typingStopped$ = new Subject<ChatTypingStoppedNotify>();
  /** После успешного reconnect — сбросить ленту к свежим 50. */
  readonly reconnected$ = new Subject<void>();

  /** Поднять соединение (user-группа на сервере) без JoinChat — для списка чатов. */
  async ensureConnected(): Promise<void> {
    await this.ensureStarted();
  }

  async joinChat(chatId: string): Promise<void> {
    await this.ensureStarted();
    if (!this.connection) {
      return;
    }

    if (this.joinedChatId && this.joinedChatId !== chatId) {
      await this.connection.invoke('LeaveChat', this.joinedChatId);
      this.joinedChatId = null;
    }

    if (this.joinedChatId === chatId) {
      return;
    }

    await this.connection.invoke('JoinChat', chatId);
    this.joinedChatId = chatId;
  }

  async leaveChat(chatId: string): Promise<void> {
    if (!this.connection || this.joinedChatId !== chatId) {
      return;
    }

    try {
      if (this.connection.state === HubConnectionState.Connected) {
        await this.stopTyping(chatId);
        await this.connection.invoke('LeaveChat', chatId);
      }
    } finally {
      this.joinedChatId = null;
    }
  }

  async startTyping(chatId: string): Promise<void> {
    await this.ensureStarted();
    if (!this.connection || this.joinedChatId !== chatId) {
      return;
    }
    if (this.connection.state !== HubConnectionState.Connected) {
      return;
    }
    await this.connection.invoke('StartTyping', chatId);
  }

  async stopTyping(chatId: string): Promise<void> {
    if (!this.connection || this.joinedChatId !== chatId) {
      return;
    }
    if (this.connection.state !== HubConnectionState.Connected) {
      return;
    }
    try {
      await this.connection.invoke('StopTyping', chatId);
    } catch {
      // Best effort при leave / disconnect.
    }
  }

  private async ensureStarted(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    if (!this.connection) {
      this.connection = new HubConnectionBuilder()
        .withUrl('/hubs/chat', {
          accessTokenFactory: () => this.auth.accessToken() ?? '',
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

      this.connection.on('messageCreated', (payload: ChatMessageNotify) => this.messageCreated$.next(payload));
      this.connection.on('messageUpdated', (payload: ChatMessageNotify) => this.messageUpdated$.next(payload));
      this.connection.on('messageDeleted', (payload: ChatMessageDeletedNotify) => this.messageDeleted$.next(payload));
      this.connection.on('participantJoined', (payload: ChatParticipantNotify) => this.participantJoined$.next(payload));
      this.connection.on('participantLeft', (payload: ChatParticipantLeftNotify) => this.participantLeft$.next(payload));
      this.connection.on('participantRoleChanged', (payload: ChatParticipantNotify) =>
        this.participantRoleChanged$.next(payload),
      );
      this.connection.on('chatListUpdated', (payload: ChatListNotify) => this.chatListUpdated$.next(payload));
      this.connection.on('roomChanged', (payload: RoomChangedNotify) => this.roomChanged$.next(payload));
      this.connection.on('typingStarted', (payload: ChatTypingStartedNotify) => this.typingStarted$.next(payload));
      this.connection.on('typingStopped', (payload: ChatTypingStoppedNotify) => this.typingStopped$.next(payload));

      this.connection.onreconnected(async () => {
        const chatId = this.joinedChatId;
        this.joinedChatId = null;
        if (chatId) {
          try {
            await this.connection?.invoke('JoinChat', chatId);
            this.joinedChatId = chatId;
          } catch {
            // Потребитель всё равно сбросит ленту через reconnected$.
          }
        }
        this.reconnected$.next();
      });
    }

    if (this.connection.state === HubConnectionState.Disconnected) {
      this.startPromise ??= this.connection
        .start()
        .catch((error) => {
          this.startPromise = null;
          throw error;
        })
        .then(() => {
          this.startPromise = null;
        });
      await this.startPromise;
    } else if (this.startPromise) {
      await this.startPromise;
    }
  }
}
