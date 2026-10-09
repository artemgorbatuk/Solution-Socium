import { Component, ElementRef, OnInit, effect, inject, input, model, signal, viewChild } from '@angular/core';
import { problemDetail } from '../../shared/api/api-response';
import { ChatApi } from '../chat-api';
import { ChatChanges } from '../chat-changes';
import { ChatListItem } from '../chat-list-item/chat-list-item';
import { ChatListModel, chatNameMaxLength } from '../chat.models';

@Component({
  selector: 'app-room-chats',
  imports: [ChatListItem],
  templateUrl: './room-chats.html',
  styleUrl: './room-chats.css',
})
export class RoomChats implements OnInit {
  private readonly chatApi = inject(ChatApi);
  private readonly chatChanges = inject(ChatChanges);

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

  constructor() {
    effect(() => this.nameInput()?.nativeElement.focus());
  }

  ngOnInit(): void {
    this.loadChats();
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
        this.loadChats();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.createError.set(problemDetail(error, 'Не удалось создать чат'));
      },
    });
  }

  protected onChatRenamed(): void {
    this.chatChanges.notify();
    this.loadChats();
  }

  protected onChatDeleted(): void {
    this.chatChanges.notify();
    this.loadChats();
  }

  protected loadChats(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.chatApi.getList(this.roomId()).subscribe({
      next: (body) => {
        this.chats.set(body.response?.rows ?? []);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loadError.set(problemDetail(error, 'Не удалось загрузить чаты'));
        this.loading.set(false);
      },
    });
  }
}
