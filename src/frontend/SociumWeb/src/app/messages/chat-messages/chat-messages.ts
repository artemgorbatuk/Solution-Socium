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
import { problemDetail } from '../../shared/api/api-response';
import { Icon } from '../../shared/icon/icon';
import { CurrentUser } from '../../users/current-user';
import { MessageApi } from '../message-api';
import { MessageItem } from '../message-item/message-item';
import { MessageListModel } from '../message.models';
import { isSubmitKey } from '../submit-key';

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
  private readonly injector = inject(Injector);

  readonly chatId = input.required<string>();
  readonly chatName = input.required<string>();

  private readonly currentUser = inject(CurrentUser);
  protected readonly currentUserId = this.currentUser.id;
  protected readonly messages = signal<MessageListModel[]>([]);
  protected readonly loading = signal(true);
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

  private readonly feed = viewChild<ElementRef<HTMLElement>>('feed');
  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');
  private loadSubscription?: Subscription;
  private openedChatId?: string;

  constructor() {
    effect(() => {
      const chatId = this.chatId();
      this.currentUser.name();
      untracked(() => (chatId === this.openedChatId ? this.load(false) : this.open(chatId)));
    });
    inject(DestroyRef).onDestroy(() => {
      this.loadSubscription?.unsubscribe();
      this.clearPending();
    });
  }

  protected onDraftInput(event: Event): void {
    this.draft.set((event.target as HTMLTextAreaElement).value);
  }

  protected onComposerKeydown(event: KeyboardEvent): void {
    if (isSubmitKey(event)) {
      this.send(event);
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
        // Нет разрешения / сбой API — запасной input.
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
              this.load(true);
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

  protected onMessageChanged(): void {
    this.load(false);
  }

  protected onMessageDeleted(): void {
    this.load(false);
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
    this.openedChatId = chatId;
    this.messages.set([]);
    this.draft.set('');
    this.clearPending();
    this.sendError.set(null);
    this.load(true);
  }

  private load(scrollToEnd: boolean): void {
    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.loadError.set(null);
    this.loadSubscription = this.messageApi.getList(this.chatId()).subscribe({
      next: (body) => {
        this.messages.set(body.response?.rows ?? []);
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

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}
