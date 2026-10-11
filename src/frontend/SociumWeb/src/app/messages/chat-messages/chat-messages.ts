import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { forkJoin, of, Subscription } from 'rxjs';
import { FileApi } from '../../files/file-api';
import {
  fileInputAccept,
  isAllowedAttachment,
  openAttachmentPicker,
  supportsShowOpenFilePicker,
} from '../../files/file-picker-categories';
import { ChatHub } from '../../realtime/chat-hub';
import { ChatMessageNotify } from '../../realtime/chat-hub.models';
import { problemDetail } from '../../shared/api/api-response';
import { Icon } from '../../shared/icon/icon';
import { CurrentUser } from '../../users/current-user';
import { MessageApi } from '../message-api';
import { MessageItem } from '../message-item/message-item';
import { MessageListModel } from '../message.models';
import { isSubmitKey } from '../submit-key';
import {
  formatTypingLabel,
  typingExpireMs,
  typingIdleMs,
  typingRefreshMs,
  TypingUser,
} from '../typing-label';

interface PendingAttachment {
  localId: string;
  file: File;
  previewUrl: string | null;
}

const MaxAttachments = 5;

@Component({
  selector: 'app-chat-messages',
  imports: [MessageItem, Icon],
  templateUrl: './chat-messages.html',
  styleUrl: './chat-messages.css',
})
export class ChatMessages {
  private readonly messageApi = inject(MessageApi);
  private readonly fileApi = inject(FileApi);
  private readonly chatHub = inject(ChatHub);
  private readonly injector = inject(Injector);

  readonly chatId = input.required<string>();
  readonly chatName = input.required<string>();

  private readonly currentUser = inject(CurrentUser);
  protected readonly currentUserId = this.currentUser.id;
  protected readonly messages = signal<MessageListModel[]>([]);
  protected readonly hasOlder = signal(false);
  protected readonly loading = signal(true);
  protected readonly loadingOlder = signal(false);
  protected readonly loadError = signal<string | null>(null);

  protected readonly draft = signal('');
  protected readonly pending = signal<PendingAttachment[]>([]);
  protected readonly sending = signal(false);
  protected readonly sendError = signal<string | null>(null);
  protected readonly fileAccept = fileInputAccept;
  protected readonly canSend = computed(
    () => (!!this.draft().trim() || this.pending().length > 0) && !this.sending(),
  );
  protected readonly canAttach = computed(() => this.pending().length < MaxAttachments && !this.sending());
  protected readonly typingUsers = signal<TypingUser[]>([]);
  protected readonly typingLabel = computed(() => formatTypingLabel(this.typingUsers()));

  private readonly feed = viewChild<ElementRef<HTMLElement>>('feed');
  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');
  private loadSubscription?: Subscription;
  private olderSubscription?: Subscription;
  private hubSubscriptions: Subscription[] = [];
  private openedChatId?: string;
  private localTyping = false;
  private typingIdleTimer: ReturnType<typeof setTimeout> | null = null;
  private typingRefreshTimer: ReturnType<typeof setInterval> | null = null;
  private readonly typingExpireTimers = new Map<string, ReturnType<typeof setTimeout>>();

  constructor() {
    effect(() => {
      const chatId = this.chatId();
      this.currentUser.name();
      untracked(() => (chatId === this.openedChatId ? undefined : this.open(chatId)));
    });
    inject(DestroyRef).onDestroy(() => {
      this.loadSubscription?.unsubscribe();
      this.olderSubscription?.unsubscribe();
      this.clearLocalTyping();
      this.clearRemoteTyping();
      this.unbindHub();
      const chatId = this.openedChatId;
      if (chatId) {
        void this.chatHub.leaveChat(chatId);
      }
      this.clearPending();
    });
  }

  protected onDraftInput(event: Event): void {
    const value = (event.target as HTMLTextAreaElement).value;
    this.draft.set(value);
    this.onComposerActivity(value);
  }

  protected onComposerKeydown(event: KeyboardEvent): void {
    if (isSubmitKey(event)) {
      this.send(event);
    }
  }

  protected onFeedScroll(): void {
    const feed = this.feed()?.nativeElement;
    if (!feed || this.loadingOlder() || !this.hasOlder() || this.loading()) {
      return;
    }

    if (feed.scrollTop < 80) {
      this.loadOlder();
    }
  }

  protected async openFilePicker(): Promise<void> {
    if (!this.canAttach()) {
      return;
    }

    if (supportsShowOpenFilePicker()) {
      try {
        const handles = await openAttachmentPicker();
        const files = await Promise.all(handles.map((handle) => handle.getFile()));
        this.addPendingFiles(files);
        return;
      } catch (error: unknown) {
        if (isAbortError(error)) {
          return;
        }
      }
    }

    this.fileInput()?.nativeElement.click();
  }

  protected onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = [...(input.files ?? [])];
    input.value = '';
    this.addPendingFiles(files);
  }

  protected removePending(localId: string): void {
    this.pending.update((list) => {
      const removed = list.find((item) => item.localId === localId);
      if (removed?.previewUrl) {
        URL.revokeObjectURL(removed.previewUrl);
      }
      return list.filter((item) => item.localId !== localId);
    });
  }

  protected send(event: Event): void {
    event.preventDefault();
    const text = this.draft().trim();
    const pending = this.pending();
    if ((!text && !pending.length) || this.sending()) {
      return;
    }

    const chatId = this.chatId();
    this.sending.set(true);
    this.sendError.set(null);

    const uploads$ = pending.length
      ? forkJoin(pending.map((item) => this.fileApi.upload(item.file)))
      : of([]);

    uploads$.subscribe({
      next: (uploaded) => {
        const fileIds = uploaded.map((file) => file.id);
        this.messageApi.create({ chatId, text, fileIds: fileIds.length ? fileIds : undefined }).subscribe({
          next: () => {
            this.sending.set(false);
            if (chatId === this.chatId()) {
              this.draft.set('');
              this.clearPending();
              this.clearLocalTyping();
            }
          },
          error: (error: unknown) => {
            this.sending.set(false);
            this.sendError.set(problemDetail(error, 'Не удалось отправить сообщение'));
          },
        });
      },
      error: (error: unknown) => {
        this.sending.set(false);
        this.sendError.set(problemDetail(error, 'Не удалось загрузить файл'));
      },
    });
  }

  private addPendingFiles(files: File[]): void {
    if (!files.length) {
      return;
    }

    const allowed = files.filter(isAllowedAttachment);
    if (allowed.length < files.length) {
      this.sendError.set('Тип выбранного файла не поддерживается.');
    } else {
      this.sendError.set(null);
    }

    const room = MaxAttachments - this.pending().length;
    const accepted = allowed.slice(0, room).map((file) => ({
      localId: crypto.randomUUID(),
      file,
      previewUrl: file.type.startsWith('image/') ? URL.createObjectURL(file) : null,
    }));
    this.pending.update((list) => [...list, ...accepted]);
  }

  private open(chatId: string): void {
    const previous = this.openedChatId;
    this.clearLocalTyping();
    this.clearRemoteTyping();
    this.openedChatId = chatId;
    this.messages.set([]);
    this.hasOlder.set(false);
    this.draft.set('');
    this.clearPending();
    this.sendError.set(null);
    this.unbindHub();
    if (previous) {
      void this.chatHub.leaveChat(previous);
    }
    this.bindHub(chatId);
    void this.chatHub.joinChat(chatId).catch(() => {
      this.loadError.set('Не удалось подключить realtime чата');
    });
    this.loadLatest(true);
  }

  private bindHub(chatId: string): void {
    this.hubSubscriptions = [
      this.chatHub.messageCreated$.subscribe((payload) => {
        if (payload.chatId !== chatId) {
          return;
        }
        this.upsertMessage(toListModel(payload), true);
        this.removeRemoteTyper(payload.senderUserId);
        if (payload.senderUserId !== this.currentUserId()) {
          this.acknowledgeIncoming(chatId, payload.id);
        }
      }),
      this.chatHub.messageUpdated$.subscribe((payload) => {
        if (payload.chatId !== chatId) {
          return;
        }
        this.upsertMessage(toListModel(payload), false);
      }),
      this.chatHub.messageDeleted$.subscribe((payload) => {
        if (payload.chatId !== chatId) {
          return;
        }
        this.messages.update((list) => list.filter((message) => message.id !== payload.id));
      }),
      this.chatHub.typingStarted$.subscribe((payload) => {
        if (payload.chatId !== chatId || payload.userId === this.currentUserId()) {
          return;
        }
        this.upsertRemoteTyper({ userId: payload.userId, name: payload.name });
      }),
      this.chatHub.typingStopped$.subscribe((payload) => {
        if (payload.chatId !== chatId) {
          return;
        }
        this.removeRemoteTyper(payload.userId);
      }),
      this.chatHub.reconnected$.subscribe(() => {
        if (this.chatId() === chatId) {
          this.clearRemoteTyping();
          this.loadLatest(true);
        }
      }),
    ];
  }

  private onComposerActivity(value: string): void {
    const chatId = this.chatId();
    if (!value.trim()) {
      this.clearLocalTyping();
      return;
    }

    if (!this.localTyping) {
      this.localTyping = true;
      void this.chatHub.startTyping(chatId).catch(() => undefined);
      this.typingRefreshTimer = setInterval(() => {
        if (this.localTyping && this.draft().trim()) {
          void this.chatHub.startTyping(chatId).catch(() => undefined);
        }
      }, typingRefreshMs);
    }

    if (this.typingIdleTimer) {
      clearTimeout(this.typingIdleTimer);
    }
    this.typingIdleTimer = setTimeout(() => this.clearLocalTyping(), typingIdleMs);
  }

  private clearLocalTyping(): void {
    if (this.typingIdleTimer) {
      clearTimeout(this.typingIdleTimer);
      this.typingIdleTimer = null;
    }
    if (this.typingRefreshTimer) {
      clearInterval(this.typingRefreshTimer);
      this.typingRefreshTimer = null;
    }
    if (!this.localTyping) {
      return;
    }
    this.localTyping = false;
    const chatId = this.openedChatId ?? this.chatId();
    void this.chatHub.stopTyping(chatId).catch(() => undefined);
  }

  private upsertRemoteTyper(user: TypingUser): void {
    this.typingUsers.update((list) => {
      const without = list.filter((item) => item.userId !== user.userId);
      return [...without, user];
    });
    const previous = this.typingExpireTimers.get(user.userId);
    if (previous) {
      clearTimeout(previous);
    }
    this.typingExpireTimers.set(
      user.userId,
      setTimeout(() => this.removeRemoteTyper(user.userId), typingExpireMs),
    );
  }

  private removeRemoteTyper(userId: string): void {
    const timer = this.typingExpireTimers.get(userId);
    if (timer) {
      clearTimeout(timer);
      this.typingExpireTimers.delete(userId);
    }
    this.typingUsers.update((list) => list.filter((item) => item.userId !== userId));
  }

  private clearRemoteTyping(): void {
    for (const timer of this.typingExpireTimers.values()) {
      clearTimeout(timer);
    }
    this.typingExpireTimers.clear();
    this.typingUsers.set([]);
  }

  /** Доставка + прочтение входящего в открытой ленте (список чатов обновится через chatListUpdated). */
  private acknowledgeIncoming(chatId: string, messageId: string): void {
    this.messageApi.markDelivered({ chatId, messageIds: [messageId] }).subscribe({ error: () => undefined });
    this.messageApi.markRead({ chatId }).subscribe({ error: () => undefined });
  }

  private unbindHub(): void {
    for (const subscription of this.hubSubscriptions) {
      subscription.unsubscribe();
    }
    this.hubSubscriptions = [];
  }

  private loadLatest(scrollToEnd: boolean): void {
    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.loadError.set(null);
    this.loadSubscription = this.messageApi.getList(this.chatId()).subscribe({
      next: (body) => {
        this.messages.set(body.response?.rows ?? []);
        this.hasOlder.set(body.response?.hasOlder ?? false);
        this.loading.set(false);
        if (scrollToEnd) {
          this.scrollToEnd();
        }
      },
      error: (error: unknown) => {
        this.loadError.set(problemDetail(error, 'Не удалось загрузить сообщения'));
        this.loading.set(false);
      },
    });
  }

  private loadOlder(): void {
    const oldest = this.messages()[0];
    if (!oldest || this.loadingOlder()) {
      return;
    }

    const feed = this.feed()?.nativeElement;
    const previousHeight = feed?.scrollHeight ?? 0;
    const previousTop = feed?.scrollTop ?? 0;

    this.loadingOlder.set(true);
    this.olderSubscription?.unsubscribe();
    this.olderSubscription = this.messageApi.getList(this.chatId(), oldest.id).subscribe({
      next: (body) => {
        const older = body.response?.rows ?? [];
        this.hasOlder.set(body.response?.hasOlder ?? false);
        if (older.length) {
          this.messages.update((list) => {
            const known = new Set(list.map((message) => message.id));
            return [...older.filter((message) => !known.has(message.id)), ...list];
          });
          afterNextRender(
            () => {
              const element = this.feed()?.nativeElement;
              if (element) {
                element.scrollTop = previousTop + (element.scrollHeight - previousHeight);
              }
            },
            { injector: this.injector },
          );
        }
        this.loadingOlder.set(false);
      },
      error: () => {
        this.loadingOlder.set(false);
      },
    });
  }

  private upsertMessage(message: MessageListModel, scrollIfNew: boolean): void {
    let added = false;
    this.messages.update((list) => {
      const index = list.findIndex((item) => item.id === message.id);
      if (index >= 0) {
        const next = [...list];
        next[index] = message;
        return next;
      }
      added = true;
      return [...list, message];
    });
    if (added && scrollIfNew) {
      this.scrollToEnd();
    }
  }

  private clearPending(): void {
    for (const item of this.pending()) {
      if (item.previewUrl) {
        URL.revokeObjectURL(item.previewUrl);
      }
    }
    this.pending.set([]);
  }

  private scrollToEnd(): void {
    afterNextRender(
      () => {
        const feed = this.feed()?.nativeElement;
        if (feed) {
          feed.scrollTop = feed.scrollHeight;
        }
      },
      { injector: this.injector },
    );
  }
}

function toListModel(payload: ChatMessageNotify): MessageListModel {
  return {
    id: payload.id,
    text: payload.text,
    createdAt: payload.createdAt,
    senderUserId: payload.senderUserId,
    senderName: payload.senderName,
    attachments: payload.attachments ?? [],
  };
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}
