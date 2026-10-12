import { Component, DestroyRef, ElementRef, effect, inject, input, model, signal, untracked, viewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { ChatHub } from '../../realtime/chat-hub';
import { ChatChangedNotify, ChatListNotify, UserChangedNotify } from '../../realtime/chat-hub.models';
import { problemDetail } from '../../shared/api/api-response';
import { CurrentUser } from '../../users/current-user';
import { ChatApi } from '../chat-api';
import { ChatChanges } from '../chat-changes';
import { ChatListItem } from '../chat-list-item/chat-list-item';
import { ChatListModel, chatNameMaxLength, sortChatList } from '../chat.models';

@Component({
  selector: 'app-room-chats',
  imports: [ChatListItem],
  templateUrl: './room-chats.html',
  styleUrl: './room-chats.css',
})
export class RoomChats {
  private readonly chatApi = inject(ChatApi);
  private readonly chatChanges = inject(ChatChanges);
  private readonly chatHub = inject(ChatHub);
  private readonly currentUser = inject(CurrentUser);

  readonly roomId = input.required<string>();
  readonly roomName = input.required<string>();
  /** Открыта форма нового чата; владелец открывает её из меню комнаты. */
  readonly creating = model(false);

  protected readonly nameMaxLength = chatNameMaxLength;

  protected readonly chats = signal<ChatListModel[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly newChatName = signal('');
  protected readonly saving = signal(false);
  protected readonly createError = signal<string | null>(null);

  private readonly nameInput = viewChild<ElementRef<HTMLInputElement>>('nameInput');
  private loadSubscription?: Subscription;
  private hubSubscription?: Subscription;

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.loadSubscription?.unsubscribe();
      this.hubSubscription?.unsubscribe();
    });
    effect(() => this.nameInput()?.nativeElement.focus());
    effect(() => {
      this.currentUser.id();
      this.chatChanges.version();
      untracked(() => this.loadChats());
    });
    effect(() => {
      this.roomId();
      this.currentUser.id();
      untracked(() => this.bindHub());
    });
  }

  protected onNewChatNameInput(event: Event): void {
    this.newChatName.set((event.target as HTMLInputElement).value);
  }

  protected cancelCreate(): void {
    this.creating.set(false);
    this.newChatName.set('');
    this.createError.set(null);
  }

  protected createChat(event: Event): void {
    event.preventDefault();
    const name = this.newChatName().trim();
    if (!name || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.createError.set(null);
    this.chatApi.create({ roomId: this.roomId(), name }).subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelCreate();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.createError.set(problemDetail(error, 'Не удалось создать чат'));
      },
    });
  }

  protected loadChats(): void {
    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.loadError.set(null);
    this.loadSubscription = this.chatApi.getList(this.roomId()).subscribe({
      next: (body) => {
        this.chats.set(sortChatList(body.response?.rows ?? []));
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loadError.set(problemDetail(error, 'Не удалось загрузить чаты'));
        this.loading.set(false);
      },
    });
  }

  private bindHub(): void {
    this.hubSubscription?.unsubscribe();
    const roomId = this.roomId();
    void this.chatHub.ensureConnected().catch(() => {
      // Список остаётся на REST; realtime — best effort.
    });
    const subscription = new Subscription();
    subscription.add(this.chatHub.chatListUpdated$.subscribe((update) => this.applyListUpdate(roomId, update)));
    subscription.add(this.chatHub.chatChanged$.subscribe((change) => this.applyChatChanged(roomId, change)));
    subscription.add(this.chatHub.userChanged$.subscribe((change) => this.applyUserChanged(change)));
    this.hubSubscription = subscription;
  }

  private applyUserChanged(change: UserChangedNotify): void {
    const name = change.name?.trim();
    if (!name) {
      return;
    }

    this.chats.update((rows) => {
      let changed = false;
      const next = rows.map((row) => {
        if (row.lastMessageSenderUserId !== change.userId) {
          return row;
        }
        changed = true;
        return { ...row, lastMessageSenderName: name };
      });
      return changed ? next : rows;
    });
  }

  private applyChatChanged(roomId: string, change: ChatChangedNotify): void {
    if (change.roomId !== roomId) {
      return;
    }

    const chatId = change.chatId;
    if (change.kind === 'deleted') {
      this.chats.update((rows) => rows.filter((row) => row.id !== chatId));
      return;
    }

    const name = change.name?.trim();
    if (!name) {
      return;
    }

    if (change.kind === 'updated') {
      this.chats.update((rows) =>
        sortChatList(rows.map((row) => (row.id === chatId ? { ...row, name } : row))),
      );
      return;
    }

    if (change.kind === 'created') {
      this.chats.update((rows) => {
        if (rows.some((row) => row.id === chatId)) {
          return rows;
        }
        return sortChatList([
          ...rows,
          {
            id: chatId,
            name,
            canUpdate: true,
            canDelete: true,
            unreadCount: 0,
          },
        ]);
      });
    }
  }

  private applyListUpdate(roomId: string, update: ChatListNotify): void {
    if (update.roomId !== roomId) {
      return;
    }

    this.chats.update((rows) => {
      const index = rows.findIndex((row) => row.id === update.chatId);
      if (index < 0) {
        return rows;
      }

      const current = rows[index];
      const next: ChatListModel = {
        ...current,
        unreadCount: update.unreadCount,
      };

      if (update.lastMessageCleared) {
        next.lastMessageText = null;
        next.lastMessageAt = null;
        next.lastMessageSenderName = null;
        next.lastMessageSenderUserId = null;
        next.lastMessageHasAttachments = false;
      } else if (
        update.lastMessageAt != null ||
        update.lastMessageText != null ||
        update.lastMessageHasAttachments ||
        update.lastMessageSenderName != null ||
        update.lastMessageSenderUserId != null
      ) {
        next.lastMessageText = update.lastMessageText ?? null;
        next.lastMessageAt = update.lastMessageAt ?? null;
        next.lastMessageSenderName = update.lastMessageSenderName ?? null;
        next.lastMessageSenderUserId = update.lastMessageSenderUserId ?? null;
        next.lastMessageHasAttachments = update.lastMessageHasAttachments ?? false;
      }

      const copy = [...rows];
      copy[index] = next;
      return sortChatList(copy);
    });
  }
}
