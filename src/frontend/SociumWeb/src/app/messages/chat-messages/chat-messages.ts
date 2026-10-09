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
import { Subscription } from 'rxjs';
import { problemDetail } from '../../shared/api/api-response';
import { MessageApi } from '../message-api';
import { MessageItem } from '../message-item/message-item';
import { MessageListModel } from '../message.models';
import { isSubmitKey } from '../submit-key';

@Component({
  selector: 'app-chat-messages',
  imports: [MessageItem],
  templateUrl: './chat-messages.html',
  styleUrl: './chat-messages.css',
})
export class ChatMessages {
  private readonly messageApi = inject(MessageApi);
  private readonly injector = inject(Injector);

  readonly chatId = input.required<string>();
  readonly chatName = input.required<string>();

  protected readonly messages = signal<MessageListModel[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly draft = signal('');
  protected readonly sending = signal(false);
  protected readonly sendError = signal<string | null>(null);
  protected readonly canSend = computed(() => !!this.draft().trim() && !this.sending());

  private readonly feed = viewChild<ElementRef<HTMLElement>>('feed');
  private loadSubscription?: Subscription;

  constructor() {
    effect(() => {
      this.chatId();
      untracked(() => this.open());
    });
    inject(DestroyRef).onDestroy(() => this.loadSubscription?.unsubscribe());
  }

  protected onDraftInput(event: Event): void {
    this.draft.set((event.target as HTMLTextAreaElement).value);
  }

  protected onComposerKeydown(event: KeyboardEvent): void {
    if (isSubmitKey(event)) {
      this.send(event);
    }
  }

  protected send(event: Event): void {
    event.preventDefault();
    const text = this.draft().trim();
    if (!text || this.sending()) {
      return;
    }

    const chatId = this.chatId();
    this.sending.set(true);
    this.sendError.set(null);
    this.messageApi.create({ chatId, text }).subscribe({
      next: () => {
        this.sending.set(false);
        if (chatId === this.chatId()) {
          this.draft.set('');
          this.load(true);
        }
      },
      error: (error: unknown) => {
        this.sending.set(false);
        this.sendError.set(problemDetail(error, 'Не удалось отправить сообщение'));
      },
    });
  }

  protected onMessageChanged(): void {
    this.load(false);
  }

  protected onMessageDeleted(): void {
    this.load(false);
  }

  private open(): void {
    this.messages.set([]);
    this.draft.set('');
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
